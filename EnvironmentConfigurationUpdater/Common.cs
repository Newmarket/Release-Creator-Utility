using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentConfigurationUpdater
{
    public class Common
    {
        public class NameIdentifier
        {
            public string value { get; set; }
            public string displayName { get; set; }
        }

        public class ValueIdentifier
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public class BranchLocation
        {
            public string branch { get; set; }
        }

        public class Version
        {
            public string id { get; set; }
            public string name { get; set; }
            public string sourceBranch { get; set; }
        }

        public class Value
        {
            public string value { get; set; }
        }

        public class User
        {
            public string id { get; set; }
            public string displayName { get; set; }

        }

        public class Task
        {
            public string taskId { get; set; }
            public string version { get; set; }
            public string name { get; set; }
            public bool enabled { get; set; }
            public bool alwaysRun { get; set; }
            public bool continueOnError { get; set; }
            public string definitionType { get; set; }
            public dynamic inputs { get; set; }
        }

        public class DeployStep
        {
            public int rank { get; set; }
            public bool isAutomated { get; set; }
            public bool isNotificationOn { get; set; }
            public int id { get; set; }
            public User approver { get; set; }
        }
    }
}
