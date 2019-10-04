/*
 * Author: Dongho Kang (kangd@ethz.ch)
 */

using System;
using UnityEngine;
using YamlDotNet.Core.Tokens;

public class CameraController : MonoBehaviour
{
    private float speed = 0.5f;
    private float sensitivity = 0.5f;
 
    private Camera cam;
    private Vector3 anchorPoint;
    private Quaternion anchorRot;

    private RenderTexture rt;        // for video recording
    
    private GameObject _selected;    // selected object by clicking

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void FixedUpdate()
    {
        // move by keyboard
        if (_selected == null)
        {
            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
                move += Vector3.forward * speed;
            if (Input.GetKey(KeyCode.S))
                move -= Vector3.forward * speed;
            if (Input.GetKey(KeyCode.D))
                move += Vector3.right * speed;
            if (Input.GetKey(KeyCode.A))
                move -= Vector3.right * speed;
            if (Input.GetKey(KeyCode.E))
                move += Vector3.up * speed;
            if (Input.GetKey(KeyCode.Q))
                move -= Vector3.up * speed;
            transform.Translate(move);
        }
        
        // select object by left click
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (_selected != null)
                {
                    // former selected object
                    foreach (var ren in _selected.GetComponentsInChildren<Renderer>())
                    {
                        ren.material.shader = Shader.Find("Diffuse");
                    }
                }
                
                _selected = hit.transform.parent.gameObject;
                
                foreach (var ren in _selected.GetComponentsInChildren<Renderer>())
                {
                    ren.material.shader = Shader.Find("Self-Illumin/Outlined Diffuse");
                }
            }
        }

        // change camera orientation by right drag 
        if (Input.GetMouseButtonDown(1))
        {
            anchorPoint = new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
            anchorRot = transform.rotation;

            // deselect object by right click
            if (_selected != null)
            {
                foreach (var ren in _selected.GetComponentsInChildren<Renderer>())
                {
                    ren.material.shader = Shader.Find("Diffuse");
                }
            }
            
            _selected = null;
        }
        if (Input.GetMouseButton(1))
        {
            Quaternion rot = anchorRot;
            Vector3 dif = anchorPoint - new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
            rot.eulerAngles += dif * sensitivity;
            transform.rotation = rot;
        }
    }
    
    void Update() {
        
        // follow selected object 
        if (_selected != null)
        {
            gameObject.transform.rotation =
                Quaternion.LookRotation(_selected.transform.position - gameObject.transform.position);
        }
    }
}