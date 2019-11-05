using Parabox.Stl;
using UnityEngine;

namespace raisimUnity.STLImport
{
    public class StlLoader
    {
        public GameObject Load(string inputFile)
        {
            Mesh[] results = Importer.Import(inputFile, CoordinateSpace.Left, UpAxis.Z);
            
            GameObject unityObj = new GameObject("mesh");
            
            foreach (var mesh in results)
            {
                GameObject unitySubObj = new GameObject(mesh.name);
                unitySubObj.AddComponent<MeshFilter>();
                unitySubObj.AddComponent<MeshRenderer>();
                unitySubObj.AddComponent<MeshCollider>();
                unitySubObj.GetComponent<MeshFilter>().mesh = mesh;
                unitySubObj.transform.SetParent(unityObj.transform, true);
            }
            
            return unityObj;
        }
    }
}