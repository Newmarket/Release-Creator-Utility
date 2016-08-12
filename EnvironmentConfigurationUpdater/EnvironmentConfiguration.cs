using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentConfigurationUpdater
{
    class EnvironmentConfiguration
    {
        public List<Environment> Environments { get; set; }
       
        public class Environment
        {
            public string Name { get; set; }

            public Dictionary<string, Common.Value> Variables { get; set; }
            
        }
    }
}
