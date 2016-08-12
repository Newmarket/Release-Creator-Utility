using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentConfigurationUpdater
{
    public class Artifact
    {

        public string alias { get; set; }
        public ArtifactDefinition definitionReference { get; set; }
        public int id { get; set; }
        public bool isPrimary { get; set; }
        public string type { get; set; }


        //public ArtifactSource source { get; set; }
        //public List<ArtifactDefinition> definitions { get; set; }
    }

    public class BuildArtifactSourceData
    {
        public string displayValue { get; set; }
        public string value { get; set; }
        public Common.BranchLocation data { get; set; }
    }

    public class ArtifactVersionList
    {
        public List<ArtifactVersion> artifactVersions { get; set; }
    }

    public class ArtifactVersion
    {
        public int artifactSourceId { get; set; }
        public List<Common.Version> versions { get; set; }
    }

    public class ArtifactDefinition
    {
        public Common.ValueIdentifier artifacts { get; set; }
        public Common.ValueIdentifier project { get; set; }
        public Common.ValueIdentifier sourceId { get; set; }
        public Common.ValueIdentifier version { get; set; }
        public Common.ValueIdentifier branch { get; set; }

        public Common.ValueIdentifier definition { get; set; }
    }



}
