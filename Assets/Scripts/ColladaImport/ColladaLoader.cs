/*
 * Author: Dongho Kang (kangd@ethz.ch)
 *
 * Inspired by http://code4k.blogspot.com/2010/08/import-and-export-3d-collada-files-with.html
 */

using System;
using System.Collections.Generic;
using System.Linq;
using raisimUnity;
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

            // (effect id, material)
            Dictionary<string, Material> effectDict = new Dictionary<string, Material>();
            // (material id, effect id)
            Dictionary<string, string> materialDict = new Dictionary<string, string>();
            // (geom id, mesh)
            Dictionary<string, Mesh> geomDict = new Dictionary<string, Mesh>();
            
            // root game object for mesh file
            GameObject unityObj = new GameObject("mesh");
            
            // Iterate on libraries
            foreach (var item in model.Items)
            {
                if (item is library_effects)
                {
                    // effect libraries -> effectDict
                    var lib_effect = item as library_effects;
                    if(lib_effect == null)
                        continue;
                    
                    foreach (var eff in lib_effect.effect)
                    {
                        var name = eff.id;
                        if (eff.Items == null) continue; 

                        foreach (var it in eff.Items)
                        {
                            var profile = it as effectFx_profile_abstractProfile_COMMON;
                            var phong = profile.technique.Item as effectFx_profile_abstractProfile_COMMONTechniquePhong;
                            
                            var diffuse = phong.diffuse.Item as common_color_or_texture_typeColor;

                            if (diffuse != null)
                            {
                                Material material = new Material(Shader.Find("Diffuse"));
                                Color color = new Color(
                                    (float)diffuse.Values[0],
                                    (float)diffuse.Values[1],
                                    (float)diffuse.Values[2],
                                    (float)diffuse.Values[3]
                                );
                                material.color = color;
                            
                                effectDict.Add(name, material);
                            }
                        }
                    }
                }
                else if (item is library_materials)
                {
                    // material libraries -> materialDict
                    var material = item as library_materials;
                    if(material == null)
                        continue;

                    foreach (var mat in material.material)
                    {
                        materialDict.Add(mat.id, mat.instance_effect.url.Substring(1));
                    }
                }
                else if (item is library_geometries)
                {
                    // geometry libraries -> geomDict
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
                        int[] idxList = new int[0];
                        
                        // material id
                        string materialId = "";
                        
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
                            int indexStride = 1;
                            int posOffset = 0;
                            int normalOffset = 0;
                            int numIndices = 0;
                            
                            // source name
                            string positionSourceName = "";
                            string normalSourceName = "";
                            
                            // current indices
                            List<int> currIdxList = new List<int>();
                            
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
                                        if (!string.IsNullOrEmpty(vs.positionId))
                                        {
                                            positionSourceName = vs.positionId;
                                            posOffset = offset;
                                        }
                                        else if (string.IsNullOrEmpty(vs.normalId))
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
                                currIdxList = triangles.p.Split(' ').Select(Int32.Parse).ToList();
                                
                                // material
                                string materialName = triangles.material;
                                if (!string.IsNullOrEmpty(materialName) && materialDict.ContainsKey(materialName))
                                {
                                    materialId = materialName;
                                }
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
                                int posIndex = currIdxList[i * indexStride + posOffset];
                                int normalIndex = currIdxList[i * indexStride + normalOffset];

                                if (model.asset.up_axis == UpAxisType.Y_UP)
                                {
                                    throw new NotImplementedException();
                                }
                                else if (model.asset.up_axis == UpAxisType.Z_UP)
                                {
                                    vertexList.Add(new Vector3(
                                        -(float)positionFloatArray[posIndex*3],
                                        (float)positionFloatArray[posIndex*3+2],
                                        -(float)positionFloatArray[posIndex*3+1]
                                    ));
                                    
                                    if (normalFloatArray.Count > 0 && (normalFloatArray.Count > normalIndex))
                                    {
                                        normalList.Add(new Vector3(
                                            -(float)normalFloatArray[normalIndex*3],
                                            (float)normalFloatArray[normalIndex*3+2],
                                            -(float)normalFloatArray[normalIndex*3+1]
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
                            }

                            // indices
                            int curNumIndices = idxList.Length;
                            Array.Resize(ref idxList, curNumIndices +  numIndices);
                            for (int i = 0; i < numIndices; i+=3)
                            {
                                idxList[curNumIndices + i+0] = i+2 + indexOffset;
                                idxList[curNumIndices + i+1] = i+1 + indexOffset;
                                idxList[curNumIndices + i+2] = i+0 + indexOffset;
                            }
                        }

                        // Add mesh to sub-gameobject
                        Mesh unityMesh = new Mesh();
                        unityMesh.vertices = vertexList.ToArray();
                        unityMesh.normals = normalList.ToArray();
                        unityMesh.triangles = idxList;
                        
                        geomDict.Add(geom.id, unityMesh);
                    }
                }
                else if (item is library_visual_scenes)
                {
                    var visual_scenes = item as library_visual_scenes;
                    if (visual_scenes == null)
                        continue;

                    foreach (var vis in visual_scenes.visual_scene)
                    {
                        foreach (var node in vis.node)
                        {
                            Quaternion quat = Quaternion.identity;
                            Vector3 pos = Vector3.zero;

                            foreach (var item2 in node.Items)
                            {
                                var matrix = item2 as matrix;
                                if(matrix == null) continue;
                                
                                Matrix4x4 unityMatrix = new Matrix4x4();
                                unityMatrix.SetColumn(0, new Vector4(
                                    (float)matrix.Values[0], 
                                    (float)matrix.Values[4],
                                    (float)matrix.Values[8],
                                    (float)matrix.Values[12]
                                ));
                                unityMatrix.SetColumn(1, new Vector4(
                                    (float)matrix.Values[1], 
                                    (float)matrix.Values[5],
                                    (float)matrix.Values[9],
                                    (float)matrix.Values[13]
                                ));
                                unityMatrix.SetColumn(2, new Vector4(
                                    (float)matrix.Values[2], 
                                    (float)matrix.Values[6],
                                    (float)matrix.Values[10],
                                    (float)matrix.Values[14]
                                ));
                                unityMatrix.SetColumn(3, new Vector4(
                                    (float)matrix.Values[3], 
                                    (float)matrix.Values[7],
                                    (float)matrix.Values[11],
                                    (float)matrix.Values[15]
                                ));

                                quat = Quaternion.LookRotation(unityMatrix.GetColumn(2), unityMatrix.GetColumn(1));
                                pos = unityMatrix.GetColumn(3);
                            }
                            
                            foreach (var geom in node.instance_geometry)
                            {
                                var url = geom.url.Substring(1);
                                
                                if (geomDict.ContainsKey(url))
                                {
                                    var unityMesh = geomDict[url];
                                    
                                    // Create sub-gameobject
                                    var unitySubObj = new GameObject(geom.name);
                                    unitySubObj.AddComponent<MeshFilter>();
                                    unitySubObj.AddComponent<MeshRenderer>();
                                    unitySubObj.AddComponent<MeshCollider>();
                                    unitySubObj.GetComponent<MeshFilter>().mesh = unityMesh;

//                                    if(!string.IsNullOrEmpty(materialId) && effectDict.ContainsKey(materialDict[materialId]))
//                                        unitySubObj.GetComponent<Renderer>().material = effectDict[materialDict[materialId]];
                                    
                                    unitySubObj.transform.SetParent(unityObj.transform, true);
                                    ObjectController.SetTransform(unitySubObj, pos, quat);
                                }
                            }
                        }
                    }
                }
            }

            return unityObj;
        }
    }
}