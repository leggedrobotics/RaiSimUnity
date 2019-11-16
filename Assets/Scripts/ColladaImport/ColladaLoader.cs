/*
 * Author: Dongho Kang (kangd@ethz.ch)
 *
 * Inspired by http://code4k.blogspot.com/2010/08/import-and-export-3d-collada-files-with.html
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using raisimUnity;
using UnityEngine;
using UnityEngine.UI;

namespace Collada141
{
    struct VertexSources
    {
        public string positionId;
        public string normalId;
    }

    public class ColladaLoader
    {
        public Tuple<GameObject, MeshUpAxis> Load(string inputFile)
        {
            COLLADA model = COLLADA.Load(inputFile);
            
            // parent directory 
            string parentDir = Directory.GetParent(inputFile).FullName;

            // (image id, texture)
            Dictionary<string, Texture2D> textureDict = new Dictionary<string, Texture2D>();
            // (effect id, material)
            Dictionary<string, Material> effectDict = new Dictionary<string, Material>();
            // (material id, effect id)
            Dictionary<string, string> materialDict = new Dictionary<string, string>();
            
            // (geom id, mesh)
            Dictionary<string, Mesh> geomDict = new Dictionary<string, Mesh>();
            // (geom id, materilas)
            Dictionary<string, List<string>> geomMatDict = new Dictionary<string, List<string>>(); 
            
            // root game object for mesh file
            GameObject unityObj = new GameObject("mesh");
            
            // Iterate on libraries
            foreach (var item in model.Items)
            {
                if (item is library_images)
                {
                    // image libraries -> effectDict
                    var lib_image = item as library_images;
                    if(lib_image == null || lib_image.image == null)
                        continue;

                    foreach (var image in lib_image.image)
                    {
                        var imagePath = Path.Combine(parentDir, image.Item as string);
                        
                        // load image
                        byte[] byteArray = File.ReadAllBytes(imagePath);
                        Texture2D texture = new Texture2D(2,2);
                        bool isLoaded = texture.LoadImage(byteArray);
                        if (!isLoaded)
                        {
                            // TODO error
                        }
                        textureDict.Add(image.id, texture);
                    }
                }
                else if (item is library_effects)
                {
                    // effect libraries -> effectDict
                    var lib_effect = item as library_effects;
                    if(lib_effect == null || lib_effect.effect == null)
                        continue;
                    
                    foreach (var eff in lib_effect.effect)
                    {
                        var name = eff.id;
                        if (eff.Items == null) continue; 

                        foreach (var it in eff.Items)
                        {
                            var profile = it as effectFx_profile_abstractProfile_COMMON;

                            Dictionary<string, string> surfaceDict = new Dictionary<string, string>();
                            Dictionary<string, string> samplerDict = new Dictionary<string, string>();
                            
                            if (it.Items != null)
                            {
                                foreach(var it2 in it.Items)
                                {
                                    var newparam = it2 as common_newparam_type;
                                    if (newparam.Item is fx_surface_common)
                                    {
                                        var surface = newparam.Item as fx_surface_common;
                                        surfaceDict.Add(newparam.sid, surface.init_from[0].Value);
                                    }
                                    else if (newparam.Item is fx_sampler2D_common)
                                    {
                                        var sampler = newparam.Item as fx_sampler2D_common;
                                        samplerDict.Add(newparam.sid, sampler.source);
                                    }
                                }
                            }

                            var phong = profile.technique.Item as effectFx_profile_abstractProfile_COMMONTechniquePhong;
                            if (phong.diffuse.Item is common_color_or_texture_typeColor)
                            {
                                // color
                                var diffuse = phong.diffuse.Item as common_color_or_texture_typeColor;
    
                                if (diffuse != null)
                                {
                                    Material material = new Material(Shader.Find("Standard"));
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
                            else if (phong.diffuse.Item is common_color_or_texture_typeTexture)
                            {
                                // texture
                                var diffuse = phong.diffuse.Item as common_color_or_texture_typeTexture;
                                
                                if (diffuse != null)
                                {
                                    var texture = diffuse.texture;

                                    if (samplerDict.ContainsKey(texture))
                                    {
                                        var surface = samplerDict[texture];
                                        if (surfaceDict.ContainsKey(surface))
                                        {
                                            var textureName = surfaceDict[surface];
                                            if (textureDict.ContainsKey(textureName))
                                            {
                                                Material material = new Material(Shader.Find("Standard"));
                                                material.SetTexture("_MainTex", textureDict[textureName]);
                                                effectDict.Add(name, material);    
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (item is library_materials)
                {
                    // material libraries -> materialDict
                    var material = item as library_materials;
                    if(material == null || material.material == null)
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
                    if (geometries== null || geometries.geometry == null)
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
                        List<Vector2> uvList = new List<Vector2>();
                        List<int[]> idxList = new List<int[]>();

                        // submesh 
                        int numSubmesh = 0;
                        List<string> subMaterials = new List<string>();
                        
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
                            int uvOffset = 0;
                            int numIndices = 0;
                            int curNumIndices = 0;
                            
                            // source name
                            string positionSourceName = "";
                            string normalSourceName = "";
                            string uvSourceName = "";
                            
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
                                    else if (input.semantic == "TEXCOORD")
                                    {
                                        uvSourceName = sourceName;
                                        uvOffset = offset;
                                    }
                                }
                                
                                numIndices = count * 3;
                                
                                // parse index from p
                                currIdxList = triangles.p.Split(' ').Select(Int32.Parse).ToList();
                                
                                // material
                                string materialName = triangles.material;
                                if (!string.IsNullOrEmpty(materialName) && materialDict.ContainsKey(materialName))
                                {
                                    subMaterials.Add(materialName);
                                }
                                
                                // Increment submesh count 
                                numSubmesh += 1;
                            }
                            else if (meshItem is polylist)
                            {
                                var polylist = meshItem as polylist;
                                var inputs = polylist.input;
                                
                                int count = (int)polylist.count;

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
                                    else if (input.semantic == "TEXCOORD")
                                    {
                                        uvSourceName = sourceName;
                                        uvOffset = offset;
                                    }
                                }
                                
                                numIndices = count * 3;
                                
                                // parse index from p
                                currIdxList = polylist.p.Split(' ').Select(Int32.Parse).ToList();
                                
                                // material
                                string materialName = polylist.material;
                                if (!string.IsNullOrEmpty(materialName) && materialDict.ContainsKey(materialName))
                                {
                                    subMaterials.Add(materialName);
                                }
                                
                                // Increment submesh count 
                                numSubmesh += 1;
                            }
                            else
                            {
//                                throw new NotImplementedException();
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
                            
                            // uv
                            List<double> uvFloatArray = new List<double>();
                            if (sourceDict.ContainsKey(uvSourceName))
                            {
                                uvFloatArray = sourceDict[uvSourceName].ToList();
                            }
                            
                            // add to list
                            int indexOffset = vertexList.Count;

                            for (int i = 0; i < (int)numIndices; i++)
                            {
                                int posIndex = currIdxList[i * indexStride + posOffset];
                                int normalIndex = currIdxList[i * indexStride + normalOffset];
                                int uvIndex = currIdxList[i * indexStride + uvOffset];

                                if (model.asset.up_axis == UpAxisType.Y_UP)
                                {
                                    vertexList.Add(new Vector3(
                                        -(float)positionFloatArray[posIndex*3],
                                        (float)positionFloatArray[posIndex*3+1],
                                        (float)positionFloatArray[posIndex*3+2]
                                    ));
                                    
                                    if (normalFloatArray.Count > 0 && (normalFloatArray.Count > normalIndex))
                                    {
                                        normalList.Add(new Vector3(
                                            -(float)normalFloatArray[normalIndex*3],
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
                                
                                if (uvFloatArray.Count > 0)
                                {
                                    uvList.Add(new Vector2(
                                        (float)uvFloatArray[uvIndex*2],
                                        (float)uvFloatArray[uvIndex*2+1]
                                    ));
                                }
                            }

                            // indices
                            int[] currIndices = new int[numIndices];
                            for (int i = 0; i < numIndices; i+=3)
                            {
                                currIndices[i+0] = i+2 + indexOffset;
                                currIndices[i+1] = i+1 + indexOffset;
                                currIndices[i+2] = i+0 + indexOffset;
                            }
                            
                            if (numIndices != 0)
                                idxList.Add(currIndices);
                            curNumIndices += numIndices;
                        }

                        // Add mesh to sub-gameobject
                        Mesh unityMesh = new Mesh();
                        unityMesh.vertices = vertexList.ToArray();
                        unityMesh.normals = normalList.ToArray();
                        unityMesh.subMeshCount = numSubmesh;
                        for (int i = 0; i < idxList.Count; i++)
                        {
                            unityMesh.SetTriangles(idxList[i], i);
                        }

                        if (uvList.Count > 0)
                        {
                            unityMesh.uv = uvList.ToArray();
                        }

                        geomDict.Add(geom.id, unityMesh);
                        geomMatDict.Add(geom.id, subMaterials);
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
                            if (node.instance_geometry == null)
                                continue;
                            
                            Quaternion quat = Quaternion.identity;
                            Vector3 pos = Vector3.zero;

                            if (node.Items != null)
                            {
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
                                    
                                    // local transform
                                    unitySubObj.transform.SetParent(unityObj.transform, true);
                                    ObjectController.SetTransform(unitySubObj, pos, quat);
                                    
                                    // material
                                    var materials = geomMatDict[url];
                                    if (materials.Count > 0)
                                    {
                                        List<Material> unityMaterials = new List<Material>();
                                        foreach (var mat in materials)
                                        {
                                            if (!materialDict.ContainsKey(mat)) continue;
                                            
                                            var eff = materialDict[mat];
                                            if(effectDict.ContainsKey(eff))
                                                unityMaterials.Add(effectDict[eff]);
                                        }
                                        unitySubObj.GetComponent<Renderer>().materials = unityMaterials.ToArray();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            MeshUpAxis meshUpAxis = MeshUpAxis.ZUp;
            if (model.asset.up_axis != null)
            {
                switch (model.asset.up_axis)
                {
                    case UpAxisType.X_UP:
                        meshUpAxis = MeshUpAxis.XUp;
                        break;
                    case UpAxisType.Y_UP:
                        meshUpAxis = MeshUpAxis.YUp;
                        break;
                    case UpAxisType.Z_UP:
                        meshUpAxis = MeshUpAxis.ZUp;
                        break;
                    default:
                        meshUpAxis = MeshUpAxis.ZUp;
                        break;
                }
            }

            return new Tuple<GameObject, MeshUpAxis>(unityObj, meshUpAxis);
        }
    }
}