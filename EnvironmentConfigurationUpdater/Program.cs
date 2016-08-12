using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using Newtonsoft.Json;
using System.Configuration;
using System.IO;

namespace EnvironmentConfigurationUpdater
{
    class Program
    {

        /*
            This Utility can be executed to take a pre-existing release definition and start the deployment of it. 
            We run this as an exe as a task at the end of our build definitions (which are in TFS) to start our 
            Release (in VSTS). We have our release pipeline (Dev->QA->Staging->Prod) and we have a json file which contains 
            all of our environment specific configuration entries. This tool takes that file and makes sure all of the 'Dev'
            environments defined in the Release definition get populated with the 'Dev' specific conifguration values. This 
            is accomplished by keeping the naming conventions in sync between the Release definition and the environment config, 
            which is passed in as a file path.
            
            Release Environment     |   Environment Config
            -----------------------------------------------------------
            Dev-ServerName          |   Dev - [ {key:value},{key:value},.. ] 
            Dev-ServerName2         |
            QA-ServerName           |   QA - [ {key:value},{key:value},... ] 
            QA-ServerName2          |
            ....
            
            The models used to create the contracts were derived from monitoring the api calls creating/starting a new Release in the UI
            they are subject to change and all fields may not be defined. 

            Workflow
            1.
                a. New release draft
                b. update the config
                c. post back the release draft
                d. start release

          */
        public static int? ReleaseId { get; set; }
        public static bool IsTest { get; set; }
        public static string BuildVersion { get; set; }
        public static string VSTSCollection { get; set; }
        public static string VSTSTeamProject { get; set; }
        public static string VSTSDomain { get; set; }
        public static string ApiVersion { get; set; }
        public static string EnvironmentConfigFilePath { get; set; }
        public static string OutputFile { get; set; }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.ToFile($"Unhandled Exception while running : {e.ExceptionObject}");
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            //get environment configuration
            EnvironmentConfiguration environmentConfigDef;
            try
            {
                environmentConfigDef = PopulateArguments(args);
            }
            catch (MissingFieldException mfe)
            {
                Log.ToFile($"A failure occured during the read of execution parameters: {mfe.Message}");
                return;
            }

            //builds the VSTS Url based on the default values or passed in parameters
            string _uri = string.Format("https://{0}/{1}/{2}/_apis/release/", VSTSDomain, VSTSCollection, VSTSTeamProject);
            Log.ToFile(string.Format("Derived URL: {0}", _uri));
            ReleaseManagementApi RM_api = new ReleaseManagementApi(ApiVersion, _uri, IsTest);


            //Gets the Release Definition from VSTS
            ReleaseManagementModels.ReleaseDraft responseGet = RM_api.GetReleaseDefinition(ReleaseId.Value);
            
            int? highestRank = null;
            int? targetEnvironment = null;
            //Determine the first environment you will need to deploy to       
            foreach (ReleaseManagementModels.ReleaseEnvironment selectedEnv in responseGet.environments)
            {
                if (!highestRank.HasValue || highestRank.Value < selectedEnv.id)
                {
                    highestRank = selectedEnv.rank;
                    targetEnvironment = selectedEnv.id;
                }
            }

            Log.ToFile(JsonConvert.SerializeObject(responseGet));
            
            Log.ToFile("\n\nCreating new Release Draft");
            //construct the draft release model
            ReleaseManagementModels.CreateDraftRelease draft = new ReleaseManagementModels.CreateDraftRelease()
            {
                definitionId = ReleaseId.Value,
                isDraft = true,
                targetEnvironmentId = targetEnvironment.Value
            };
            
            //Post the Draft release to the RM API.
            ReleaseManagementModels.ReleaseDraft responsePost = RM_api.CreateDraftRelease(draft);


            //update the different release environments with the proper environment variables         
            foreach (ReleaseManagementModels.ReleaseEnvironment selectedEnv in responsePost.environments)
            {

                Log.ToFile("Updating Environment Variables: " + selectedEnv.name);
                try
                {
                    
                    ReleaseManagementModels.ReleaseEnvironment tempEnv = responseGet.environments.Where(x => x.id == selectedEnv.definitionEnvironmentId).FirstOrDefault();
                    selectedEnv.preApprovalsSnapshot = tempEnv?.preDeployApprovals;
                    selectedEnv.postApprovalsSnapshot = tempEnv?.postDeployApprovals;
                    // The environment is a JSON Dictionary of environment specific variables for when deploying web/app configs. 
                    // QA and DEV will have different connection strings and cerdentials that are stored in the Release Environment Variables 
                    dynamic resultingVariables = GetUpdatedEnvironmentVariables(environmentConfigDef, selectedEnv.name);
                    selectedEnv.variables = resultingVariables;
                   
                }
                catch(MissingMemberException mme)
                {
                    Log.ToFile($"No environment defined in config:{mme.Message}");
                }


            }

            //set release level variables
            Dictionary<string, Common.Value> releaseVariables = new Dictionary<string, Common.Value>();
            releaseVariables.Add("eRestVersion", new Common.Value() { value = BuildVersion });
            responsePost.variables = releaseVariables;


            //Update the draft with all the new Environment and Release variables
            Log.ToFile("Updating ReleaseDraft variables (Environment and Release)");
            RM_api.PostRelease(responsePost, responsePost.id);
            Log.ToFile("Updated ReleaseDraft variables");

            Log.ToFile($"Starting Release {responsePost.name} ....");
            //start Release
            StartRelease(responsePost, draft.isDraft, targetEnvironment.Value, RM_api);
            Log.ToFile($"Started Release {responsePost.name}");
            
        }

        public static dynamic StartRelease(ReleaseManagementModels.ReleaseDraft draft, bool _isDraft, int targetEnv, ReleaseManagementApi api)
        {
            //Get the latest draft
            Log.ToFile($"Getting latest release with id: {draft.id}");
            ReleaseManagementModels.ReleaseDraft currentRelease = api.GetRelease(draft.id);


            //first get list of artifacts...
            Log.ToFile($"Getting list of artifact versions");
            ArtifactVersionList versions = api.GetArtifactVersions(currentRelease.artifacts);
            Log.ToFile($"Got artifact versions \n\n" + JsonConvert.SerializeObject(versions));
            
            //determine the highest version of all the artifacts and use that. 
            Dictionary<int, BuildArtifactSourceData> artifactSources = new Dictionary<int, BuildArtifactSourceData>(); 
            foreach(ArtifactVersion av in versions.artifactVersions)
            {
                var highestVersion = av.versions.First();

                  BuildArtifactSourceData sd = new BuildArtifactSourceData()
                {
                    value = highestVersion.id,
                    displayValue = highestVersion.name,
                    data = new Common.BranchLocation()
                    {
                        branch = highestVersion.sourceBranch
                    }
                };

                artifactSources.Add(av.artifactSourceId, sd);
            }
            
            //update the current release to put it in a building state
            currentRelease.status = "1";
            currentRelease.reason = "1";

            //set environment statuses and approvals
            foreach(ReleaseManagementModels.ReleaseEnvironment env in currentRelease.environments)
            {
                env.status = "0";
                env.originalPostDeployApprovals = null;
                env.originalPreDeployApprovals = null; 
            }

            //set artifact versions
            foreach(Artifact art in currentRelease.artifacts)
            {

                BuildArtifactSourceData versionData = artifactSources[art.id];
                Log.ToFile($"Latest vesrion of artifact {art.id}, \nlatest version: {versionData.value} - {versionData.displayValue} \nBranch: {versionData.data.branch}");
                art.definitionReference.version = new Common.ValueIdentifier() { id = versionData.value, name = versionData.displayValue };
                art.definitionReference.branch = new Common.ValueIdentifier() { id = versionData.data.branch, name = versionData.data.branch };

            }


            Log.ToFile("Queueing Release");
            dynamic response = api.PostRelease(currentRelease, currentRelease.id);

            //sets the release status to be in progress
            Log.ToFile("Release to be started...");
            dynamic patchResponse = new ExpandoObject();
            patchResponse = new
            {
                status = 3, //I beleive this maps to inProgress
            };

            response = api.StartRelease(patchResponse, currentRelease.id);

            return response;            
        }

        /// <summary>
        /// This populates some static variables passed in via the command line and attempts to read the 
        /// Environment Configuration file in. This file represents the Variables that will be associated 
        /// to each environment defined the release definition. 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static EnvironmentConfiguration PopulateArguments(string[] args)
        {
            IsTest = false;
            DateTime d = DateTime.Now;
            BuildVersion = null;// "testversion";
            VSTSCollection = "DefaultCollection";
            VSTSDomain = "newmarketrm.vsrm.visualstudio.com";
            VSTSTeamProject = "NWS_eRest";
            ApiVersion = ConfigurationManager.AppSettings["SelectedApiVersion"];
            EnvironmentConfigFilePath = null;// "EnvironmentTest.json";
            OutputFile = d.Day.ToString() + d.Month.ToString() + d.Year.ToString() + "_" + d.Hour.ToString() + d.Minute.ToString() + d.Second.ToString() + "_log.txt";
            ReleaseId = null;//5;
        
            if (args.Length != 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i].ToLower();
                    if (arg == "istest")
                        IsTest = Convert.ToBoolean(args[i + 1]);
                    if (arg == "buildversion")
                        BuildVersion = args[i + 1];
                    if (arg == "vstscollection")
                        VSTSCollection = args[i + 1];
                    if (arg == "vstsdomain")
                        VSTSDomain = args[i + 1];
                    if (arg == "vststeamproject")
                        VSTSTeamProject = args[i + 1];
                    if (arg == "apiversion")
                        ApiVersion = args[i + 1];
                    if (arg == "releaseid")
                        ReleaseId = Convert.ToInt32(args[i + 1]);
                    if (arg == "config")
                        EnvironmentConfigFilePath = args[i + 1];
                    if (arg == "output")
                    {
                        OutputFile = args[i + 1];
                    }
                        
                }
            }

            Log.fileName = OutputFile;
            Log.ToFile($"IsTest = {IsTest}\nVSTSCollection = {VSTSCollection}\nVSTSDomain = {VSTSDomain}\nVSTSTeamProject = {VSTSTeamProject}\nApiVersion = {ApiVersion}\nEnvironmentConfigFilePath = {EnvironmentConfigFilePath}\nOutputFile = {OutputFile}\nReleaseId = {ReleaseId}\nBuildVersion = {BuildVersion}");
            
            if (!ReleaseId.HasValue)
                throw new MissingFieldException("ReleaseId was not provided");
            if (string.IsNullOrEmpty(EnvironmentConfigFilePath))
                throw new MissingFieldException("EnvironmentConfiguration not provided");
            if (string.IsNullOrEmpty(BuildVersion))
                throw new MissingFieldException("BuildVersion not provided");
            string config;

            EnvironmentConfiguration environmentConfig;
            try
            {
                config = File.ReadAllText(@EnvironmentConfigFilePath);
                environmentConfig = JsonConvert.DeserializeObject<EnvironmentConfiguration>(config);
            }
            catch(JsonException je)
            {
                throw new MissingFieldException("Could not interpret provided config file as a valid environment configuration");
            }
            catch (IOException)
            {
                throw new MissingFieldException($"Could not read Environment Config from the provided config filepath: {EnvironmentConfigFilePath}");
            }
           
            return environmentConfig;


        }


        

        public static dynamic GetUpdatedEnvironmentVariables(EnvironmentConfiguration masterConfig, string environmentId)
        {

            string truncatedEnvName = environmentId;
            if(environmentId.Contains('-'))
                truncatedEnvName = environmentId.Substring(0, environmentId.IndexOf('-'));
            var selectedEnv = (from env in masterConfig.Environments
                                where env.Name == truncatedEnvName
                                select env); 

            if(selectedEnv == null || selectedEnv.Count() == 0)
            {
                throw new MissingMemberException($"No environment with Name: {environmentId} found in the configuration file. Searched for environment key of {truncatedEnvName}");
            }
            
            return selectedEnv.First().Variables;

        }

    }
}
