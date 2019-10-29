/*
 * Author: Dongho Kang (kangd@ethz.ch)
 *
 * Inspired by http://code4k.blogspot.com/2010/08/import-and-export-3d-collada-files-with.html
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Collada141
{
    struct VertexSources
    {
        public string positionId;
        public string normalId;
    }
    
    public class ColladaLoader
    {
        public GameObject Load(string inputFile)
        {
            COLLADA model = COLLADA.Load(inputFile);
            GameObject unityObj = new GameObject("mesh");

            // Iterate on libraries
            foreach (var item in model.Items)
            {
                if (item is library_geometries)
                {
                    // geometry libraries
                    var geometries = item as library_geometries;
                    if (geometries== null)
                        continue;

                    // Dictionary stores source arrays
                    Dictionary<string, double[]> sourceDict = new Dictionary<string, double[]>();
                    Dictionary<string, VertexSources> vertexSourceDict = new Dictionary<string, VertexSources>();
                    
                    // Iterate on geomerty in library_geometries 
                    foreach (var geom in geometries.geometry)
                    {
                        // converted as vector3 
                        List<Vector3> vertexList = new List<Vector3>();
                        List<Vector3> normalList = new List<Vector3>();
                        List<int> idxList = new List<int>();
                        
                        var mesh = geom.Item as mesh;
                        if (mesh == null)
                            continue;
                        
                        foreach (var source in mesh.source)
                        {
                            var float_array = source.Item as float_array;
                            if (float_array == null)
                                continue;
         
                            sourceDict.Add(source.id, float_array.Values);
                        }
                        
                        // Add vertex of mesh to list
                        foreach(var input in mesh.vertices.input)
                        {
                            if (input.semantic == "POSITION")
                            {
                                VertexSources vs = new VertexSources();
                                vs.positionId = input.source.Substring(1);
                                vertexSourceDict.Add(mesh.vertices.id, vs);
                            }
                            else if (input.semantic == "NORMAL")
                            {
                                VertexSources vs = new VertexSources();
                                vs.normalId = input.source.Substring(1);
                                vertexSourceDict.Add(mesh.vertices.id, vs);
                            }
                        }

                        if (mesh.Items == null)
                            continue;
                        
                        // triangle or polylist 
                        foreach (var meshItem in mesh.Items)
                        {
                            // indices
                            int indexStride = 1;
                            int posOffset = 0;
                            int normalOffset = 0;
                            int numIndices = 0;
                            
                            // source name
                            string positionSourceName = "";
                            string normalSourceName = "";
                            
                            // triangles
                            if (meshItem is triangles)
                            {
                                var triangles = meshItem as triangles;
                                var inputs = triangles.input;
                                
                                int count = (int)triangles.count;

                                foreach (var input in inputs)
                                {
                                    // offset
                                    int offset = (int)input.offset;
                                    if (offset + 1 > indexStride)
                                        indexStride = offset + 1;
                                    
                                    // source 
                                    string sourceName = input.source.Substring(1);
                                    
                                    if (input.semantic == "VERTEX")
                                    {
                                        VertexSources vs = vertexSourceDict[sourceName];
                                        if (vs.positionId != null && vs.positionId.Length > 0)
                                        {
                                            positionSourceName = vs.positionId;
                                            posOffset = offset;
                                        }
                                        else if (vs.normalId != null && vs.normalId.Length > 0)
                                        {
                                            normalSourceName = vs.normalId;
                                            normalOffset = offset;
                                        }
                                    }
                                    else if (input.semantic == "NORMAL")
                                    {
                                        normalSourceName = sourceName;
                                        normalOffset = offset;
                                    }
                                }
                                
                                numIndices = count * 3;
                                
                                // parse index from p
                                idxList = triangles.p.Split(' ').Select(Int32.Parse).ToList();
                            }

                            // vertex
                            List<double> positionFloatArray = new List<double>();
                            if (sourceDict.ContainsKey(positionSourceName))
                            {
                                positionFloatArray = sourceDict[positionSourceName].ToList();
                            }
                            
                            // normal
                            List<double> normalFloatArray = new List<double>();
                            if (sourceDict.ContainsKey(normalSourceName))
                            {
                                normalFloatArray = sourceDict[normalSourceName].ToList();
                            }
                            
                            // add to list
                            int indexOffset = vertexList.Count;

                            for (int i = 0; i < (int)numIndices; i++)
                            {
                                int posIndex = idxList[i * indexStride + posOffset];
                                int normalIndex = idxList[i * indexStride + normalOffset];
                                
                                vertexList.Add(new Vector3(
                                    (float)positionFloatArray[posIndex*3],
                                    (float)positionFloatArray[posIndex*3+1],
                                    (float)positionFloatArray[posIndex*3+2]
                                ));

                                if (normalFloatArray.Count > 0 && (normalFloatArray.Count > normalIndex))
                                {
                                  normalList.Add(new Vector3(
                                      (float)normalFloatArray[normalIndex*3],
                                      (float)normalFloatArray[normalIndex*3+1],
                                      (float)normalFloatArray[normalIndex*3+2]
                                      ));  
                                }
                                else
                                {
                                    // Add dummy normal for debugging
                                    normalList.Add(new Vector3(
                                        0,
                                        0,
                                        0
                                    ));
                                }
                            }

                            for (int i = 0; i < numIndices; i++)
                            {
                                idxList.Add(i + indexOffset);
                            }
                        }
                        
                        // Create sub-gameobject
                        var unitySubObj = new GameObject(geom.id);
                        unitySubObj.transform.SetParent(unityObj.transform, true);
                        
                        // Add mesh to sub-gameobject
                        Mesh unityMesh = new Mesh();
                        unityMesh.vertices = vertexList.ToArray();
                        unityMesh.normals = normalList.ToArray();
                        unityMesh.triangles = idxList.ToArray();
                        unitySubObj.AddComponent<MeshFilter>();
                        unitySubObj.AddComponent<MeshRenderer>();
                        unitySubObj.AddComponent<MeshCollider>();

                        unitySubObj.GetComponent<MeshFilter>().mesh = unityMesh;
//                        unityObj.GetComponent<MeshRenderer>().material =  temp.GetComponent<MeshRenderer>().sharedMaterial;
                    }
                }
                else if (item is library_materials)
                {
                    // material libraries
                    
                }
            }
            
            return unityObj;
        }

    }
}