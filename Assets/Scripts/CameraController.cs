/*
 * Author: Dongho Kang (kangd@ethz.ch)
 */

using UnityEditor;
using UnityEngine;
 
public class CameraController : MonoBehaviour
{
    [SerializeField] float speed = 0.25f;
    [SerializeField] float sensitivity = 0.25f;
 
    private Camera cam;
    private Vector3 anchorPoint;
    private Quaternion anchorRot;
    
    private GameObject _selected;    // selected object by clicking
 
    private void Awake()
    {
        cam = GetComponent<Camera>();
    }
   
    void FixedUpdate()
    {
        Vector3 move = Vector3.zero;
        if(Input.GetKey(KeyCode.W))
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
 
        if (Input.GetMouseButtonDown(1))
        {
            anchorPoint = new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
            anchorRot = transform.rotation;
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
        
        // deselect object by right click
        else if (Input.GetMouseButtonDown(1))
        {
            if (_selected != null)
            {
                foreach (var ren in _selected.GetComponentsInChildren<Renderer>())
                {
                    ren.material.shader = Shader.Find("Diffuse");
                }
            }
            
            _selected = null;
        }
        
        // follow selected object 
        if (_selected != null)
        {
            gameObject.transform.rotation =
                Quaternion.LookRotation(_selected.transform.position - gameObject.transform.position);

        }
    }
}