/*
 * Author: Dongho Kang (kangd@ethz.ch)
 */

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Toggle = UnityEngine.UI.Toggle;

namespace raisimUnity
{
    public class UIController : MonoBehaviour
    {
        private TcpRemote _remote = null;
        private CameraController _camera = null;
        
        static GUIStyle _style = null;
        
        // UI element names
        // Buttons
        private const string _ButtonConnectName = "_ButtonConnect";
        private const string _ButtonScreenshotName = "_ButtonScreenshot";
        private const string _ButtonRecordName = "_ButtonRecord";
        private const string _ButtonAddResourceName = "_ButtonAddResource";
        private const string _ButtonDeleteResourceName = "_ButtonDeleteResource";
        
        // Dropdown 
        private const string _DropdownBackgroundName = "DropdownBackground";
        
        // Scroll View
        private const string _ScrollViewResourceDirs = "ScrollResources";
        
        // Input field
        private const string _InputFieldTcpName = "_InputFieldTcp"; 
        private const string _InputFieldPortName = "_InputFieldPort"; 
        
        // Toggle
        private const string _ToggleVisualBodiesName = "_ToggleVisualBodies";
        private const string _ToggleCollisionBodiesName = "_ToggleCollisionBodies";
        private const string _ToggleContactPointsName = "_ToggleContactPoints";
        private const string _ToggleContactForcesName = "_ToggleContactForces";
        
        // Modal view
        private const string _ErrorModalViewName = "_ModalViewError";
        private const string _ErrorModalViewMessageName = "_TextErrorMessage";

        // Backgrounds
        private Material _daySky;
        private Material _sunriseSky;
        private Material _sunsetSky;
        private Material _nightSky;
        private Material _milkywaySky;
        
        private void Awake()
        {
            _remote = GameObject.Find("RaiSimUnity").GetComponent<TcpRemote>();
            _camera = GameObject.Find("Main Camera").GetComponent<CameraController>();

            if (_remote == null)
            {
                // TODO exception
            }

            if (_camera == null)
            {
                // TODO exception
            }

            // modal view
            {
                var modal = GameObject.Find(_ErrorModalViewName).GetComponent<Canvas>();
                modal.enabled = false;
                var okButton = modal.GetComponentInChildren<Button>();
                okButton.onClick.AddListener(() => { modal.enabled = false;});
            }
            
            // visualize section
            {
                var toggleVisual = GameObject.Find(_ToggleVisualBodiesName).GetComponent<Toggle>();
                toggleVisual.onValueChanged.AddListener((isSelected) =>
                {
                    _remote.ShowVisualBody = isSelected;
                    _remote.ShowOrHideObjects();
                });
                var toggleCollision = GameObject.Find(_ToggleCollisionBodiesName).GetComponent<Toggle>();
                toggleCollision.onValueChanged.AddListener((isSelected) =>
                {
                    _remote.ShowCollisionBody = isSelected;
                    _remote.ShowOrHideObjects();
                });
                var toggleContactPoints = GameObject.Find(_ToggleContactPointsName).GetComponent<Toggle>();
                toggleContactPoints.onValueChanged.AddListener((isSelected) =>
                {
                    _remote.ShowContactPoints = isSelected;
                    _remote.ShowOrHideObjects();
                });
                var toggleContactForces = GameObject.Find(_ToggleContactForcesName).GetComponent<Toggle>();
                toggleContactForces.onValueChanged.AddListener((isSelected) =>
                {
                    _remote.ShowContactForces = isSelected;
                    _remote.ShowOrHideObjects();
                });
            }
            
            // connection section
            {
                var ipInputField = GameObject.Find(_InputFieldTcpName).GetComponent<InputField>();
                ipInputField.text = _remote.TcpAddress;
                var portInputField = GameObject.Find(_InputFieldPortName).GetComponent<InputField>();
                portInputField.text = _remote.TcpPort.ToString();
                var connectButton = GameObject.Find(_ButtonConnectName).GetComponent<Button>();
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
                        catch (Exception e)
                        {
                            var modal = GameObject.Find(_ErrorModalViewName).GetComponent<Canvas>();
                            modal.enabled = true;
                            var message = GameObject.Find(_ErrorModalViewMessageName).GetComponentInChildren<Text>();
                            message.text = e.Message;
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
                var screenshotButton = GameObject.Find(_ButtonScreenshotName).GetComponent<Button>();
                screenshotButton.onClick.AddListener(() =>
                {
                    if (!File.Exists("Screenshot"))
                        Directory.CreateDirectory("Screenshot");
                    var filename = "Screenshot/Screenshot " + DateTime.Now.ToString("yyyy-MM-d hh-mm-ss") + ".png";
                    ScreenCapture.CaptureScreenshot(filename);
                });
                
                var recordButton = GameObject.Find(_ButtonRecordName).GetComponent<Button>();
                recordButton.onClick.AddListener(() =>
                {
                    if (_camera.IsRecording)
                    {
                        _camera.FinishRecording();
                    }
                    else
                    {
                        _camera.StartRecording();
                    }
                });
            }
            
            // background section 
            {
                _daySky = Resources.Load<Material>("backgrounds/Wispy Sky/Materials/WispySkyboxMat2");
                _sunriseSky = Resources.Load<Material>("backgrounds/Wispy Sky/Materials/WispySkyboxMat");
                _sunsetSky = Resources.Load<Material>("backgrounds/Skybox/Materials/Skybox_Sunset");
                _nightSky = Resources.Load<Material>("backgrounds/FreeNightSky/Materials/nightsky1");
                _milkywaySky = Resources.Load<Material>("backgrounds/MilkyWay/Material/MilkyWay");

                var backgroundDropdown = GameObject.Find(_DropdownBackgroundName).GetComponent<Dropdown>();
                backgroundDropdown.onValueChanged.AddListener(delegate {
                    ChangeBackground(backgroundDropdown);
                    DynamicGI.UpdateEnvironment();
                });
            }
            
            // resource section 
            {
                _remote.ResourceLoader.LoadFromPref();
                RefereshScrollResources();

                var addButton = GameObject.Find(_ButtonAddResourceName).GetComponent<Button>();
                addButton.onClick.AddListener(() =>
                {
                    SimpleFileBrowser.FileBrowser.ShowLoadDialog((path) =>
                    {
                        _remote.ResourceLoader.AddResourceDirectory(path);
                        RefereshScrollResources();
                    }, null, true);
                });
                
                var removeButton = GameObject.Find(_ButtonDeleteResourceName).GetComponent<Button>();
                removeButton.onClick.AddListener(() =>
                {
                    _remote.ResourceLoader.RemoveResourceDirectory();
                    RefereshScrollResources();
                });
            }
        }

        private void RefereshScrollResources()
        {
            var scrollRect = GameObject.Find(_ScrollViewResourceDirs).GetComponent<ScrollRect>();
            
            // remove every text
            var uiContent = FindContent(scrollRect);
            foreach (Transform child in uiContent)
            {
                Destroy(child.gameObject);
            }

            // referesh text
            foreach (var dir in _remote.ResourceLoader.ResourceDirs)
            {
                DefaultControls.Resources tempResource = new DefaultControls.Resources();
                GameObject newText = DefaultControls.CreateText(tempResource);
                newText.AddComponent<LayoutElement>();
                newText.transform.SetParent(uiContent);
                newText.GetComponent<Text>().text = dir;
                newText.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            }
        }

        private RectTransform FindContent (ScrollRect ScrollViewObject) {
            RectTransform RetVal = null;
            Transform[] Temp = ScrollViewObject.GetComponentsInChildren<Transform>();
            foreach (Transform Child in Temp) {
                if (Child.name == "Content") { RetVal = Child.gameObject.GetComponent<RectTransform>(); }
            }
            return RetVal;
        }
        
        private void ChangeBackground(Dropdown dropdown)
        {
            switch (dropdown.value)
            {
            case 0:
                // day
                RenderSettings.skybox=_daySky;
                break;
            case 1:
                // sunrise
                RenderSettings.skybox=_sunriseSky;
                break;
            case 2:
                // sunset
                RenderSettings.skybox=_sunsetSky;
                break;
            case 3:
                // night
                RenderSettings.skybox=_nightSky;
                break;
            case 4:
                // milkyway
                RenderSettings.skybox=_milkywaySky;
                break;
            default:
                // TODO error
                break;
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
            var connectButton = GameObject.Find(_ButtonConnectName).GetComponent<Button>();

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
            
            // show recording status
            var recordButton = GameObject.Find(_ButtonRecordName);
            
            if (_camera.IsRecording)
            {
                recordButton.GetComponentInChildren<Text>().text = "Stop Recording";
            }
            else
            {
                recordButton.GetComponentInChildren<Text>().text = "Record Video";
            }

            if (!_camera.IsRecording && _camera.ThreadIsProcessing)
            {
                recordButton.GetComponent<Button>().interactable = false;
                recordButton.GetComponentInChildren<Text>().text = "Saving Video...";
            }
            else
            {
                recordButton.GetComponent<Button>().interactable = true;
            }
        }
    }
}