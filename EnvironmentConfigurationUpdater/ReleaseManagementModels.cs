using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Dynamic;

namespace EnvironmentConfigurationUpdater
{
    public class ReleaseManagementModels
    {

        public class CreateDraftRelease
        {
            public int definitionId { get; set; }

            public string releaseName { get; set; }

            public int targetEnvironmentId { get; set; }

            public string description { get; set; }

            public Dictionary<string, Common.NameIdentifier> artifactSourceData { get; set; }

            public bool isDraft { get; set; }

        }      
        public class ReleaseDraft
        {
            public int id { get; set; }
            public int revision { get; set; }
            public string name { get; set; }
            public string status { get; set; }
            public bool isDeactivated { get; set; }
            public Common.User createdBy { get; set; }
            public DateTime createdOn { get; set; }
            public Common.User modifiedBy { get; set; }
            public DateTime modifiedOn { get; set; }
            public List<ReleaseEnvironment> environments { get; set; }
            public dynamic variables { get; set; }
            public List<Artifact> artifacts { get; set; }
            public ReleaseDefinition releaseDefinition { get; set; }
            public string description { get; set; }
            public string reason { get; set; }
            public int targetEnvironmentId { get; set; }
            public string releaseNameFormat { get; set; }
            //The following items are found in the Get of a release
            //and not returned when you post a CreateDraftRelease
            public List<Artifact> linkedArtifacts { get; set; }
            public List<ReleaseTrigger> triggers { get; set; }


        }
        public class ReleaseDefinition
        {
            public int id { get; set; }
            public string name { get; set; }
            public string url { get; set; }
        }
        public class ReleaseEnvironment
        {
            //standard dataTypes
            public int id { get; set; }
            public string name { get; set; }
            public int releaseId { get; set; }
            public string status { get; set; }
            public int rank { get; set; }
            public int queueId { get; set; }
            public int definitionEnvironmentId { get; set; }
            //custom data types
            public dynamic variables { get; set; }
            public dynamic preDeployApprovals { get; set; }
            public dynamic postDeployApprovals { get; set; }
            public dynamic preApprovalsSnapshot { get; set; }
            public dynamic postApprovalsSnapshot { get; set; }
            public dynamic originalPreDeployApprovals { get; set; }
            public dynamic originalPostDeployApprovals { get; set; }
            public List<Common.Task> tasks { get; set; }
            public RunOptions runOptions { get; set; }
            
            public dynamic demands { get; set; }
            public List<Common.Task> workflowTasks { get; set; }
            public Common.User owner { get; set; }
            
            //The following items are found in the Get of a release
            //and not returned when you post a CreateDraftRelease
            public dynamic preDeploySteps { get; set; }
            public ReleaseDeployStep deployStep { get; set; }
            public dynamic postDeployStep { get; set; }

        }
        public class ReleaseTrigger
        {
            public string triggerType { get; set; }
            public int triggerEntityId { get; set; }
            public string targetEnvironmentName { get; set; }
        }
        public class ReleaseDeployStep
        {
            public int id { get; set; }
            public List<Common.Task> tasks { get; set; }
        }
        public class RunOptions
        {
            public string EnvironmentOwnerEmailNotificationType { get; set; }
            public string skipArtifactsDownload { get; set; }
            public string TimeoutInMinutes { get; set; }
        }
        

    }
}
