/*
 * Author: Dongho Kang (kangd@ethz.ch)
 */

using System.Collections.Generic;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace raisimUnity
{
    public class ObjectController
    {
        public static GameObject CreateSphere(GameObject root, string name, float radius, string tag)
        {
            var objFrame = new GameObject(name);
            objFrame.transform.SetParent(root.transform, false);
            objFrame.tag = tag;
            var viz = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            viz.transform.SetParent(objFrame.transform, true);
            viz.transform.localScale = new Vector3(radius*2.0f, radius*2.0f, radius*2.0f);
//            viz.GetComponent<Collider>().enabled = false;
            return objFrame;
        }

        public static GameObject CreateBox(GameObject root, string name, float sx, float sy, float sz, string tag)
        {
            var objFrame = new GameObject(name);
            objFrame.transform.SetParent(root.transform, false);
            objFrame.tag = tag;
            var viz = GameObject.CreatePrimitive(PrimitiveType.Cube);
            viz.transform.SetParent(objFrame.transform, true);
            viz.transform.localScale = new Vector3(sx, sy, sz);
//            viz.GetComponent<Collider>().enabled = false;
            return objFrame;
        }

        public static GameObject CreateCylinder(GameObject root, string name, float radius, float height, string tag)
        {
            var objFrame = new GameObject(name);
            objFrame.transform.SetParent(root.transform, false);
            objFrame.tag = tag;
            var viz = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            viz.transform.SetParent(objFrame.transform, true);
            viz.transform.localScale = new Vector3(radius*2f, height*0.5f, radius*2f);
//            viz.GetComponent<Collider>().enabled = false;
            return objFrame;
        }

        public static GameObject CreateCapsule(GameObject root, string name, float radius, float height, string tag)
        {
            
            var objFrame = new GameObject(name);
            objFrame.transform.SetParent(root.transform, false);
            objFrame.tag = tag;
                        
            // Note.
            // raisim geometry of capsule: http://ode.org/wiki/index.php?title=Manual#Capsule_Class
            // unity geometry of capsule: https://docs.unity3d.com/Manual/class-CapsuleCollider.html
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(objFrame.transform, true);
            capsule.transform.localScale = new Vector3(radius*2f, height*0.5f+radius, radius*2f);
//            capsule.GetComponent<Collider>().enabled = false;
            return capsule;
        }

        public static GameObject CreateHalfSpace(GameObject root, string name, float height, string tag)
        {
            var objFrame = new GameObject(name);
            objFrame.transform.SetParent(root.transform, false);
            objFrame.tag = tag;
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.SetParent(objFrame.transform, true);
            plane.transform.localPosition = new Vector3(0, height, 0);
//            plane.GetComponent<Collider>().enabled = false;
            return plane;
        }

        public static GameObject CreateTerrain(GameObject root, string name, 
            ulong numSampleX, float sizeX, float centerX, ulong numSampleY, float sizeY, float centerY, 
            float[,] heights, string tag)
        {
            // Note that we create terrain with mesh since unity support only square size height map
            
            var objFrame = new GameObject(name);
            objFrame.transform.SetParent(root.transform, false);
            objFrame.tag = tag;

            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();
            List<Vector2> uv = new List<Vector2>();
            List<Vector3> normal = new List<Vector3>();

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
                    vertices.Add(new Vector3(
                        gridStartX + j * gridSizeX,
                        heights[i, j],
                        gridStartY + i * gridSizeY  
                        ));
                    
                    // (x, y) = (j+1, i)
                    vertices.Add(new Vector3(
                        gridStartX + (j + 1) * gridSizeX,
                        heights[i, j + 1],
                        gridStartY + i * gridSizeY
                        ));
                    
                    // (x, y) = (j+1, i+1)
                    vertices.Add(new Vector3(
                        gridStartX + (j + 1) * gridSizeX,
                        heights[i + 1, j + 1],
                        gridStartY + (i + 1) * gridSizeY
                    ));

                    // (x, y) = (j, i)
                    vertices.Add(new Vector3(
                        gridStartX + j * gridSizeX,
                        heights[i, j],
                        gridStartY + i * gridSizeY
                    ));
                    
                    // (x, y) = (j+1, i+1)
                    vertices.Add(new Vector3(
                        gridStartX + (j + 1) * gridSizeX,
                        heights[i + 1, j + 1],
                        gridStartY + (i + 1) * gridSizeY
                    ));
                    
                    // (x, y) = (j, i+1)
                    vertices.Add(new Vector3(
                        gridStartX + j * gridSizeX,
                        heights[i + 1, j],
                        gridStartY + (i + 1) * gridSizeY
                    ));
                }
            }
            
            // triangles
            for (int i = 0; i < vertices.Count; i++) {
                indices.Add(i);
            }
            
            // normals
            for (int i = 0; i < vertices.Count; i += 3) {
                Vector3 point1 = vertices[i];
                Vector3 point2 = vertices[i+1];
                Vector3 point3 = vertices[i+2];
                
                Vector3 diff1 = point2 - point1;
                Vector3 diff2 = point3 - point2;
                Vector3 norm = Vector3.Cross(diff1, diff2);
                norm = Vector3.Normalize(norm); 

                normal.Add(norm);
                normal.Add(norm);
                normal.Add(norm);
            }
            
            // uv
            for (ulong i = 0; i < numSampleY-1; i++)
            {
                for (ulong j = 0; j < numSampleX-1; j++)
                {
                    uv.Add(new Vector2(
                        j / (float)numSampleX,
                        i / (float)numSampleY
                    ));
                    
                    uv.Add(new Vector2(
                        (j + 1) / (float)numSampleX,
                        i / (float)numSampleY
                    ));
                    
                    uv.Add(new Vector2(
                        (j + 1) / (float)numSampleX,
                        (i + 1) / (float)numSampleY
                    ));
                    
                    uv.Add(new Vector2(
                        j / (float)numSampleX,
                        i / (float)numSampleY
                    ));
                    
                    uv.Add(new Vector2(
                        (j + 1) / (float)numSampleX,
                        (i + 1) / (float)numSampleY
                    ));
                    
                    uv.Add(new Vector2(
                        j / (float)numSampleX,
                        (i + 1) / (float)numSampleY
                    ));
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = indices.ToArray();
            mesh.uv = uv.ToArray();
            mesh.normals = normal.ToArray();

            var terrain = new GameObject("terrain");
            terrain.transform.SetParent(objFrame.transform, true);
            terrain.AddComponent<MeshFilter>();
            terrain.AddComponent<MeshRenderer>();
            terrain.GetComponent<MeshFilter>().mesh = mesh;
            
            return objFrame;
        }

        public static GameObject CreateMesh(GameObject root, string name, string meshFile, float sx, float sy, float sz, string tag)
        {
            // meshFile is file name without file extension related to Resources directory
            // sx, sy, sz is scale 
            
            var objFrame = new GameObject(name);
            objFrame.transform.SetParent(root.transform, false);
            objFrame.tag = tag;
            var meshRes = Resources.Load(meshFile) as GameObject;
            if (meshRes == null)
            {
                // TODO error
            }
            var mesh = GameObject.Instantiate(meshRes);
            mesh.transform.SetParent(objFrame.transform, true);
            mesh.transform.localScale = new Vector3((float)sx, (float)sy, (float)sz);
            
            // add collider to children
            foreach (Transform children in mesh.transform)
            {
                children.gameObject.AddComponent<MeshCollider>();
            }
            
            return objFrame;
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
    }
}