using System.Collections.Generic;
using System.IO;

namespace raisimUnity
{
    public class ResourceLoader
    {
        private List<string> resourceDirs;

        public ResourceLoader()
        {
            resourceDirs = new List<string>();
            
            // TODO just for test 
            resourceDirs.Add("/home/donghok/Workspace/unity/raisimUnity/Examples/rsc");
        }

        public void RetrieveMesh(string parentDir, string nameWithExtension)
        {
            foreach (var dir in resourceDirs)
            {
                var meshPath = Path.Combine(dir, parentDir, nameWithExtension);
                if (File.Exists(meshPath))
                {
                    
                }
            }
        }

    }
}