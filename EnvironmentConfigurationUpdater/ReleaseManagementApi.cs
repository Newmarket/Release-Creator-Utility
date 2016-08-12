using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using System.Configuration;
using System.IO;

namespace EnvironmentConfigurationUpdater
{
    class ReleaseManagementApi
    {
        
        private static string ReleaseManagementUri { get; set; }
        private static string ApiVersion { get; set; }
        private static bool IsTest { get; set; }
        public ReleaseManagementApi(string api, string uri, bool _isTest)
        {
            ApiVersion = api;
            ReleaseManagementUri = uri;
            IsTest = _isTest;

        }

        /// <summary>
        /// Creates a newe DraftRelease and returns the resulting response in a dynamic value
        /// </summary>
        /// <param name="draft"></param>
        /// <returns></returns>
        public ReleaseManagementModels.ReleaseDraft CreateDraftRelease(ReleaseManagementModels.CreateDraftRelease draft)
        {

            string _url = ReleaseManagementUri + "releases";
            string _method = "POST";
            dynamic _payload = draft;

            return JsonConvert.DeserializeObject<ReleaseManagementModels.ReleaseDraft>(SendRestRequest(_url, _method, _payload));
            
        }

        /// <summary>
        /// Sends in an updated Configuration value
        /// </summary>
        /// <param name="updatedConfig"></param>
        /// <param name="releaseId"></param>
        /// <returns></returns>
        public dynamic PostRelease(dynamic updatedConfig, int releaseId)
        {
            string _url = string.Format("{0}releases/{1}",ReleaseManagementUri,releaseId.ToString());
            string _method = "POST";
            dynamic _payload = updatedConfig;

            return JsonConvert.DeserializeObject(SendRestRequest(_url, _method, _payload));

        }

        /// <summary>
        /// Does a get on the ReleaseDefinition and returns the payload as a dynamic value
        /// </summary>
        /// <param name="releaseId"></param>
        /// <returns></returns>
        public ReleaseManagementModels.ReleaseDraft GetReleaseDefinition(int releaseId)
        {


            string _url = ReleaseManagementUri + "definitions/" + releaseId.ToString();
            string _method = "GET";
            dynamic _payload = null;

            return JsonConvert.DeserializeObject<ReleaseManagementModels.ReleaseDraft>(SendRestRequest(_url, _method, _payload));

        }

        public ArtifactVersionList GetArtifactVersions(dynamic _artifacts)
        {

            string _url = ReleaseManagementUri + "artifacts/versions";
            string _method = "POST";
            dynamic _payload = _artifacts;

            return JsonConvert.DeserializeObject<ArtifactVersionList>(SendRestRequest(_url, _method, _payload));
        }

        public dynamic StartRelease(dynamic kickoff, int releaseId)
        {
            string _url = string.Format("{0}releases/{1}", ReleaseManagementUri, releaseId.ToString());
            string _method = "PATCH";
            dynamic _payload = kickoff;

            return JsonConvert.DeserializeObject(SendRestRequest(_url, _method, _payload));

        }

        public ReleaseManagementModels.ReleaseDraft GetRelease(int releaseId)
        {
            string _url = string.Format("{0}releases/{1}", ReleaseManagementUri, releaseId.ToString());
            string _method = "GET";
            dynamic _payload = null;

            return JsonConvert.DeserializeObject<ReleaseManagementModels.ReleaseDraft>(SendRestRequest(_url, _method, _payload));

        }


        /// <summary>
        /// Basic Http Request sender
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        private static string SendRestRequest(string url, string method, dynamic payload)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            
            request.Method = method;
            request.Accept = string.Format("application/json;api-version={0}", ApiVersion);
            request.ContentType = "application/json";
            request.ContentLength = 0;
            request.Headers.Add("Authorization", "Basic " + GetBasicCredentials());
            string payloadString = "";
            if(payload != null)
            {
                payloadString = JsonConvert.SerializeObject(payload, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }  );
                request.ContentLength = payloadString.Length;
                using (var stream = new StreamWriter(request.GetRequestStream()))
                {
                    stream.Write(payloadString);
                    stream.Close();
                }
            }


            string result;

            if (IsTest)
                return "Testing....";

            Console.WriteLine(string.Format("Sending Http Request:\nURL: {0}\nPayload: {1}\n Method: {2}\n", url, payload, method));

            HttpWebResponse webResponse = request.GetResponse() as HttpWebResponse;

            using (var stream = new StreamReader(webResponse.GetResponseStream()))
            {
                result = stream.ReadToEnd();
            }

            if (webResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException(string.Format("Request was not successful! \nStatus: {0}\nMessage: {1}", webResponse.StatusCode.ToString(), result));
            }

            return result;
        }

        private static string GetBasicCredentials()
        {
            string username = ConfigurationManager.AppSettings["BuildUserName"];
            string password = ConfigurationManager.AppSettings["BuildToken"];
            
            return Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));

        }




    }
}
