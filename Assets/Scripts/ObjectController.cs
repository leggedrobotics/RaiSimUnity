/*
 * Author: Dongho Kang (kangd@ethz.ch)
 */

using System.Collections.Generic;
using Dummiesman;
using UnityEngine;
using UnityEngine.Rendering;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace raisimUnity
{
    public class ObjectController
    {
        private OBJLoader _objLoader;
        
        public ObjectController()
        {
            _objLoader = new OBJLoader(); 
        }

        public GameObject CreateSphere(GameObject root, float radius)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(root.transform, true);
            sphere.transform.localScale = new Vector3(radius*2.0f, radius*2.0f, radius*2.0f);
            return sphere;
        }

        public GameObject CreateBox(GameObject root, float sx, float sy, float sz)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(root.transform, true);
            box.transform.localScale = new Vector3(sx, sy, sz);
            return box;
        }

        public GameObject CreateCylinder(GameObject root, float radius, float height)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.transform.SetParent(root.transform, true);
            cylinder.transform.localScale = new Vector3(radius*2f, height*0.5f, radius*2f);
            return cylinder;
        }

        public GameObject CreateCapsule(GameObject root, float radius, float height)
        {
            // Note.
            // raisim geometry of capsule: http://ode.org/wiki/index.php?title=Manual#Capsule_Class
            // unity geometry of capsule: https://docs.unity3d.com/Manual/class-CapsuleCollider.html
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(root.transform, true);
            capsule.transform.localScale = new Vector3(radius*2f, height*0.5f+radius, radius*2f);
            return capsule;
        }

        public GameObject CreateHalfSpace(GameObject root, float height)
        {
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.SetParent(root.transform, true);
            plane.transform.localPosition = new Vector3(0, height, 0);
            plane.transform.localScale = new Vector3(5, 1, 5);
            return plane;
        }
        
        public GameObject CreateTerrain(GameObject root, 
            ulong numSampleX, float sizeX, float centerX, ulong numSampleY, float sizeY, float centerY, 
            float[,] heights, bool recomputeNormal = true)
        {
            // Note that we create terrain with mesh since unity support only square size height map

            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();

            float gridSizeX = sizeX / (numSampleX - 1);
            float gridSizeY = sizeY / (numSampleY - 1);
            float gridStartX = centerX - sizeX * 0.5f;
            float gridStartY = centerY - sizeY * 0.5f;
            
            // vertices
            for (ulong i = 0; i < numSampleY-1; i++)
            {
                for (ulong j = 0; j < numSampleX-1; j++)
                {
                    // (x, y) = (j, i)
                    vertices.Add(ConvertRs2Unity(
                        gridStartX + j * gridSizeX,
                        gridStartY + i * gridSizeY,
                        heights[i, j]
                    ));
                    
                    // (x, y) = (j+1, i+1)
                    vertices.Add(ConvertRs2Unity(
                        gridStartX + (j + 1) * gridSizeX,
                        gridStartY + (i + 1) * gridSizeY,
                        heights[i + 1, j + 1]
                    ));
                    
                    // (x, y) = (j+1, i)
                    vertices.Add(ConvertRs2Unity(
                        gridStartX + (j + 1) * gridSizeX,
                        gridStartY + i * gridSizeY,
                        heights[i, j + 1]
                    ));

                    // (x, y) = (j, i)
                    vertices.Add(ConvertRs2Unity(
                        gridStartX + j * gridSizeX,
                        gridStartY + i * gridSizeY,
                        heights[i, j]
                    ));
                    
                    // (x, y) = (j, i+1)
                    vertices.Add(ConvertRs2Unity(
                        gridStartX + j * gridSizeX,
                        gridStartY + (i + 1) * gridSizeY,
                        heights[i + 1, j]
                    ));
                    
                    // (x, y) = (j+1, i+1)
                    vertices.Add(ConvertRs2Unity(
                        gridStartX + (j + 1) * gridSizeX,
                        gridStartY + (i + 1) * gridSizeY,
                        heights[i + 1, j + 1]
                    ));
                }
            }
            
            // triangles
            for (int i = 0; i < vertices.Count; i++) {
                indices.Add(i);
            }

            if (!recomputeNormal)
            {
                // normals
                for (int i = 0; i < vertices.Count; i += 3)
                {
                    Vector3 point1 = ConvertUnity2RS(vertices[i].x, vertices[i].y, vertices[i].z);
                    Vector3 point2 = ConvertUnity2RS(vertices[i + 1].x, vertices[i + 1].y, vertices[i + 1].z);
                    Vector3 point3 = ConvertUnity2RS(vertices[i + 2].x, vertices[i + 2].y, vertices[i + 2].z);

                    Vector3 diff1 = point2 - point1;
                    Vector3 diff2 = point3 - point2;
                    Vector3 norm = Vector3.Cross(diff1, diff2);
                    norm = Vector3.Normalize(norm);
                    norm = ConvertRs2Unity(norm.x, norm.y, norm.z);

                    normals.Add(norm);
                    normals.Add(norm);
                    normals.Add(norm);
                }
            }

            // uvs
            for (ulong i = 0; i < numSampleY-1; i++)
            {
                for (ulong j = 0; j < numSampleX-1; j++)
                {
                    // (x, y) = (j, i)
                    uvs.Add(new Vector2(
                        (j / (float)numSampleX),
                        (i / (float)numSampleY)
                    ));

                    // (x, y) = (j+1, i+1)
                    uvs.Add(new Vector2(
                        ((j + 1) / (float)numSampleX),
                        ((i + 1) / (float)numSampleY)
                    ));
                    
                    // (x, y) = (j+1, i)
                    uvs.Add(new Vector2(
                        ((j + 1) / (float)numSampleX),
                        (i / (float)numSampleY)
                    ));

                    // (x, y) = (j, i)
                    uvs.Add(new Vector2(
                        (j / (float)numSampleX),
                        (i / (float)numSampleY)
                    ));
                                      
                    // (x, y) = (j, i+1)
                    uvs.Add(new Vector2(
                        (j / (float)numSampleX),
                        ((i + 1) / (float)numSampleY)
                    ));
                    
                    // (x, y) = (j+1, i+1)
                    uvs.Add(new Vector2(
                        ((j + 1) / (float)numSampleX),
                        ((i + 1) / (float)numSampleY)
                    ));
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = indices.ToArray();
            mesh.uv = uvs.ToArray();
            if (!recomputeNormal) mesh.normals = normals.ToArray();
            else mesh.RecalculateNormals();

            // this is just temporal object (will be deleted immediately!)
            var temp = GameObject.CreatePrimitive(PrimitiveType.Plane);
            temp.SetActive(false);

            var terrain = new GameObject("terrain");
            terrain.transform.SetParent(root.transform, true);
            terrain.AddComponent<MeshFilter>();
            terrain.AddComponent<MeshRenderer>();

            terrain.GetComponent<MeshFilter>().mesh = mesh;
            terrain.GetComponent<MeshRenderer>().material =  temp.GetComponent<MeshRenderer>().sharedMaterial;
            terrain.AddComponent<MeshCollider>();

            // destroy temp 
            GameObject.DestroyImmediate(temp);

            return terrain;
        }

        public GameObject CreateMesh(GameObject root, string meshFile, float sx, float sy, float sz, bool flipYz=false)
        {
            // meshFile is file name without file extension related to Resources directory
            // sx, sy, sz is scale 

            var mesh = _objLoader.Load(meshFile);
            if (mesh == null)
            {
                // TODO error
            }
//            var mesh = GameObject.Instantiate(meshRes);
            mesh.transform.SetParent(root.transform, true);
            mesh.transform.localScale = new Vector3((float)sx, (float)sy, (float)sz);
            if(flipYz)
                mesh.transform.localRotation = new Quaternion(-0.7071f, 0, 0, 0.7071f);
            
            // add collider to children
            foreach (Transform children in mesh.transform)
            {
                children.gameObject.AddComponent<MeshCollider>();
            }
            
            return mesh;
        }

        public GameObject CreateContactMarker(GameObject root, int index, Vector3 rsPos)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.SetParent(root.transform, true);
            marker.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
            marker.GetComponent<Collider>().enabled = false;

            marker.tag = "contact";
            marker.name = "contact" + index.ToString();
            marker.transform.localPosition = new Vector3(-rsPos.x, rsPos.z, -rsPos.y);
            marker.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            marker.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
            return marker;
        }
        
        public GameObject CreateContactForceMarker(GameObject root, int index, Vector3 rsPos, Vector3 force)
        {
            var meshRes = Resources.Load("others/arrow") as GameObject;
            var marker = GameObject.Instantiate(meshRes);
            marker.GetComponentInChildren<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            marker.transform.SetParent(root.transform, true);
            marker.tag = "contact";
            marker.name = "contactForce" + index.ToString();

            Vector3 view = new Vector3(-force.x, force.z, -force.y);
            
            Quaternion q = new Quaternion(0, 0, 0, 1);
            q.SetLookRotation(view);
            
            marker.transform.localPosition = new Vector3(-rsPos.x, rsPos.z, -rsPos.y);
            marker.transform.localRotation = q;
            marker.transform.localScale = new Vector3(0.3f, 0.3f, 1.0f);
            marker.GetComponentInChildren<Renderer>().material.SetColor("_Color", Color.blue);
            return marker;
        }
        
        public static void SetTransform(GameObject obj, Vector3 rsPos, Quaternion rsQuat)
        {
            // rsPos is position in RaiSim
            // rsQuat is quaternion in RaiSim
            
            var yaxis = rsQuat * new Vector3(0, 1.0f, 0);
            var zaxis = rsQuat * new Vector3(0, 0, 1.0f);

            Quaternion q = new Quaternion(0, 0, 0, 1);
            q.SetLookRotation(
                new Vector3(yaxis[0], -yaxis[2], yaxis[1]),
                new Vector3(-zaxis[0], zaxis[2], -zaxis[1])
            );

            obj.transform.localPosition = new Vector3(-rsPos.x, rsPos.z, -rsPos.y);
            obj.transform.localRotation = q;
        }
        
        private static Vector3 ConvertRs2Unity(float rx, float ry, float rz)
        {
            return new Vector3(-rx, rz, -ry);
        }

        private static Vector3 ConvertUnity2RS(float ux, float uy, float uz)
        {
            return new Vector3(-ux, -uz, uy);
        }
    }
}