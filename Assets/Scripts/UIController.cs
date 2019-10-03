/*
 * Author: Dongho Kang (kangd@ethz.ch)
 */

using System;
using UnityEngine;
using UnityEngine.UI;

namespace raisimUnity
{
    public class UIController : MonoBehaviour
    {
        private TcpRemote _remote = null;
        static GUIStyle _style = null;
        
        private void Awake()
        {
            _remote = GameObject.Find("RaiSimUnity").GetComponent<TcpRemote>();
            
            // modal view
            {
                var modal = GameObject.Find("ErrorModalView").GetComponent<Canvas>();
                modal.enabled = false;
                var okButton = modal.GetComponentInChildren<Button>();
                okButton.onClick.AddListener(() => { modal.enabled = false;});
            }
            
            // visualize section
            {
                var toggleVisual = GameObject.Find("ToggleVisualBodies").GetComponent<Toggle>();
                toggleVisual.onValueChanged.AddListener((isSelected) =>
                {
                    _remote.ShowVisualBody = isSelected;
                });
                var toggleCollision = GameObject.Find("ToggleCollisionBodies").GetComponent<Toggle>();
                toggleCollision.onValueChanged.AddListener((isSelected) =>
                {
                    _remote.ShowCollisionBody = isSelected;
                });
                var toggleContactPoints = GameObject.Find("ToggleContactPoints").GetComponent<Toggle>();
                toggleContactPoints.onValueChanged.AddListener((isSelected) =>
                {
                    _remote.ShowContactPoints = isSelected;
                });
                var toggleContactForces = GameObject.Find("ToggleContactForces").GetComponent<Toggle>();
                toggleContactForces.onValueChanged.AddListener((isSelected) =>
                {
                    _remote.ShowContactForces = isSelected;
                });
            }
            
            // connection section
            {
                var ipInputField = GameObject.Find("TCP IP Inputfield").GetComponent<InputField>();
                ipInputField.text = _remote.TcpAddress;
                var portInputField = GameObject.Find("TCP Port Inputfield").GetComponent<InputField>();
                portInputField.text = _remote.TcpPort.ToString();
                var connectButton = GameObject.Find("Connect Button").GetComponent<Button>();
                connectButton.onClick.AddListener(() =>
                {
                    _remote.TcpAddress = ipInputField.text;
                    _remote.TcpPort = Int32.Parse(portInputField.text);
                
                    // connect / disconnect
                    if (!_remote.TcpConnected)
                    {
                        try
                        {
                            _remote.EstablishConnection();
                        }
                        catch (Exception)
                        {
                            var modal = GameObject.Find("ErrorModalView").GetComponent<Canvas>();
                            modal.enabled = true;
                        }
                    }
                    else
                    {
                        try
                        {
                            _remote.CloseConnection();
                        }
                        catch (Exception)
                        {
                        
                        }
                    }
                });
            }
            
            // recording section 
            {
                var connectButton = GameObject.Find("Screenshot Button").GetComponent<Button>();
                connectButton.onClick.AddListener(() =>
                {
                    var filename = "Screenshot " + DateTime.Now.ToString("yyyy-MM-d hh-mm-ss") + ".png";
                    ScreenCapture.CaptureScreenshot(filename);
                });
            }
        }
        
        // GUI
        void OnGUI()
        {
            // set style once
            if( _style==null )
            {
                _style = GUI.skin.textField;
                _style.normal.textColor = Color.white;
        
                // scale font size with DPI
                if( Screen.dpi<100 )
                    _style.fontSize = 14;
                else if( Screen.dpi>300 )
                    _style.fontSize = 34;
                else
                    _style.fontSize = Mathf.RoundToInt(14 + (Screen.dpi-100.0f)*0.1f);
            }
        
            // show connected status
            var connectButton = GameObject.Find("Connect Button").GetComponent<Button>();

            if (_remote.TcpConnected)
            {
                GUILayout.Label("Connected", _style);
                connectButton.GetComponentInChildren<Text>().text = "Disconnect";
            }
            else
            {
                GUILayout.Label("Waiting", _style);
                connectButton.GetComponentInChildren<Text>().text = "Connect";
            }
        }
    }
}