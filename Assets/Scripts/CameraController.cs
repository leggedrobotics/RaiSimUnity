/*
 * MIT License
 * 
 * Copyright (c) 2019, Dongho Kang, Robotics Systems Lab, ETH Zurich
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *
 * Note. This code is inspired by https://gist.github.com/DashW/74d726293c0d3aeb53f4
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using raisimUnity;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    // camera pose control
    private float speed = 0.1f;
    private float sensitivity = 0.5f;
 
    private Camera cam;
    private Vector3 _anchorPoint;
    private Quaternion _anchorRot;
    private Vector3 _relativePositionB;

    // object selection
    private GameObject _selected; 

    // video recording
    private bool _isRecording = false;
    public bool IsRecording
    {
        get => _isRecording;
    }

    // Public Properties
    public int maxFrames; // maximum number of frames you want to record in one video
    public int frameRate = 30; // number of frames to capture per second
    public bool videoAvailable = false;

    // The Encoder Thread
    private Thread _saverThread;

    // Texture Readback Objects
    private RenderTexture _tempRenderTexture;
    private Texture2D _tempTexture2D;

    // Timing Data
    private float captureFrameTime;
    private float lastFrameTime;
    private int frameNumber;

    // Encoder Thread Shared Resources
    private Queue<byte[]> _frameQueue;
    private int _screenWidth;
    private int _screenHeight;
    private bool terminateThreadWhenDone;
    private bool threadIsProcessing;
    
    // Screenshot related
    private string _dirPath = "";
    private string _videoName = "Recording.mp4";    // updated to Recording-<TIME>.mp4
    
    // Error modal view
    private const string _ErrorModalViewName = "_CanvasModalViewError";
    private ErrorViewController _errorModalView;
    
    public bool ThreadIsProcessing
    {
        get => threadIsProcessing;
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        
        // Error modal view
        _errorModalView = GameObject.Find("_CanvasModalViewError").GetComponent<ErrorViewController>();
        
        // Check if FFMPEG available
        int ffmpegExitCode = FFMPEGTest();
        if (ffmpegExitCode == 0)
            videoAvailable = true;
        
        // Check if video directory is created
        _dirPath = Path.Combine(Application.dataPath, "../Screenshot");
        if (!File.Exists(_dirPath))
            Directory.CreateDirectory(_dirPath);
    }

    void Start () 
    {
        // Set target frame rate (optional)
        Application.targetFrameRate = frameRate;

        // Prepare textures and initial values
        _screenWidth = cam.pixelWidth;
        _screenHeight = cam.pixelHeight;
		
        _tempRenderTexture = new RenderTexture(_screenWidth, _screenHeight, 0);
        _tempTexture2D = new Texture2D(_screenWidth, _screenHeight, TextureFormat.RGB24, false);
        _frameQueue = new Queue<byte[]> ();

        frameNumber = 0;

        captureFrameTime = 1.0f / (float)frameRate;
        lastFrameTime = Time.time;
        
        // Kill the encoder thread if running from a previous execution
        if (_saverThread != null && (threadIsProcessing || _saverThread.IsAlive)) {
            threadIsProcessing = false;
            _saverThread.Join();
        }
    }

    void Update() {
        
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
        
        if (!EventSystem.current.IsPointerOverGameObject ()) 
        {
            // Only do this if mouse pointer is not on the GUI
            
            // Select object by left click
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (_selected != null)
                    {
                        // Change shader back for previously selected object
                        foreach (var ren in _selected.GetComponentsInChildren<Renderer>())
                        {
                            ren.material.shader = Shader.Find("Standard");
                        }
                    }
                
                    // Set selected object
                    _selected = hit.transform.parent.gameObject;
                    
                    // Focus camera on selected object + save relative position of object w.r.t camera
                    transform.rotation = Quaternion.LookRotation(_selected.transform.position - transform.position);
                    _relativePositionB = Quaternion.Inverse(transform.rotation) * (_selected.transform.position - transform.position);

                    // Change shader for selected object
                    foreach (var ren in _selected.GetComponentsInChildren<Renderer>())
                    {
                        ren.material.shader = Shader.Find("Outlined/UltimateOutline");
                    }
                }
            }

            // Change camera orientation by right drag 
            if (Input.GetMouseButtonDown(1))
            {
                _anchorPoint = new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
                _anchorRot = transform.rotation;

                // deselect object by right click
                if (_selected != null)
                {
                    foreach (var ren in _selected.GetComponentsInChildren<Renderer>())
                    {
                        ren.material.shader = Shader.Find("Standard");
                    }
                }
            
                _selected = null;
            }
            
            if (Input.GetMouseButton(1))
            {
                Quaternion rot = _anchorRot;
                Vector3 dif = _anchorPoint - new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
                rot.eulerAngles += dif * sensitivity;
                transform.rotation = rot;
            }   
            
            // Set anchor for orbiting around selected object  
            if (Input.GetMouseButtonDown(0) && _selected != null)
            {
                _anchorPoint = new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
                _anchorRot = transform.rotation;
            }
            
            if (Input.GetMouseButton(0) && _selected != null)
            {
                Quaternion rot = _anchorRot;
                Vector3 dif = _anchorPoint - new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
                rot.eulerAngles += dif * sensitivity;
                transform.rotation = rot;
            }
        }
        
        // Follow and orbiting around selected object  
        if (_selected != null)
        {
            transform.position = _selected.transform.position - transform.rotation * _relativePositionB;
        }
    }
    
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_isRecording)
        {
            // Check if render target size has changed, if so, terminate
            if(source.width != _screenWidth || source.height != _screenHeight)
            {
                FinishRecording();
                
                // Show error modal view
                _errorModalView.Show(true);
                _errorModalView.SetMessage("You cannot change screen size during a recording. Terminated recording. (video is saved)");
            }

            // Calculate number of video frames to produce from this game frame
            // Generate 'padding' frames if desired framerate is higher than actual framerate
            float thisFrameTime = Time.time;
            int framesToCapture = ((int)(thisFrameTime / captureFrameTime)) - ((int)(lastFrameTime / captureFrameTime));

            // Capture the frame
            if (framesToCapture > 0)
            {
                Graphics.Blit(source, _tempRenderTexture);

                RenderTexture.active = _tempRenderTexture;
                _tempTexture2D.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                FlipTextureVertically(_tempTexture2D);
                RenderTexture.active = null;
            }

            // Add the required number of copies to the queue
            for (int i = 0; i < framesToCapture; ++i)
            {
                var data = _tempTexture2D.GetRawTextureData();
                if(data == null) continue;
                
                _frameQueue.Enqueue(data);
                frameNumber++;
            }

            lastFrameTime = thisFrameTime;
        }

        // Passthrough
        Graphics.Blit (source, destination);
    }
    
    private static void FlipTextureVertically(Texture2D original)
    {
        // on some platforms texture is flipped by unity
        
        var originalPixels = original.GetPixels();

        Color[] newPixels = new Color[originalPixels.Length];

        int width = original.width;
        int rows = original.height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                newPixels[x + y * width] = originalPixels[x + (rows - y -1) * width];
            }
        }

        original.SetPixels(newPixels);
        original.Apply();
    }

    public void TakeScreenShot()
    {
        if (!File.Exists(_dirPath))
            Directory.CreateDirectory(_dirPath);
        var filename = Path.Combine(
            _dirPath,
            "Screenshot-" + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss") + ".png");
        ScreenCapture.CaptureScreenshot(filename);
    }

    public void StartRecording()
    {
        // Kill thread if it's still alive
        if (_saverThread != null && (threadIsProcessing || _saverThread.IsAlive)) {
            threadIsProcessing = false;
            _saverThread.Join();
        }
        
        // Set recording screend width and height
        _screenWidth = cam.pixelWidth;
        _screenHeight = cam.pixelHeight;
		
        _tempRenderTexture = new RenderTexture(_screenWidth, _screenHeight, 0);
        _tempTexture2D = new Texture2D(_screenWidth, _screenHeight, TextureFormat.RGB24, false);
        
        // Start recording
        if (threadIsProcessing)
        {
            // TODO error... something wrong...
            print("oops...");
        }
        else
        {
            _isRecording = true;
            frameNumber = 0;
        
            // Start a new encoder thread
            _videoName = "Recording-" + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss") + ".mp4";
            
            threadIsProcessing = true;
            _saverThread = new Thread(SaveVideo);
            _saverThread.Start();
        }
    }

    public void FinishRecording()
    {
        // Done queueing
        _isRecording = false;
        
        // Terminate thread after it saves
        terminateThreadWhenDone = true;
    }

    private int FFMPEGTest()
    {
        // to check ffmpeg works 
        using (var ffmpegProc = new Process())
        {
            if (Application.platform == RuntimePlatform.LinuxEditor ||
                Application.platform == RuntimePlatform.LinuxPlayer)
            {
                // Linux
                ffmpegProc.StartInfo.FileName = "/bin/sh";
            }
            else if (Application.platform == RuntimePlatform.OSXEditor ||
                     Application.platform == RuntimePlatform.OSXPlayer)
            {
                // Mac
                throw new NotImplementedException();
            }
            else
            {
                // Else...
                throw new NotImplementedException();
            }

            ffmpegProc.StartInfo.UseShellExecute = false;
            ffmpegProc.StartInfo.CreateNoWindow = true;
            ffmpegProc.StartInfo.RedirectStandardInput = true;
            ffmpegProc.StartInfo.RedirectStandardOutput = true;
            ffmpegProc.StartInfo.RedirectStandardError = true;
            ffmpegProc.StartInfo.Arguments =
                "-c \"" +
                "ffmpeg -version\"";

            ffmpegProc.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                // print(e.Data);    // this is for debugging 
            });
            ffmpegProc.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                // print(e.Data);     // this is for debugging
            });

            // Start ffmpeg
            ffmpegProc.Start();
            ffmpegProc.BeginErrorReadLine();
            ffmpegProc.BeginOutputReadLine();

            while (!ffmpegProc.HasExited) {}
            
            // check exit code
            return ffmpegProc.ExitCode;
        }

    }

    private void SaveVideo()
    {
        print ("SCREENRECORDER IO THREAD STARTED");

        // Generate file path
        string path = Path.Combine(_dirPath, _videoName);

        using (var ffmpegProc = new Process())
        {
            if (Application.platform == RuntimePlatform.LinuxEditor ||
                Application.platform == RuntimePlatform.LinuxPlayer)
            {
                // Linux
                ffmpegProc.StartInfo.FileName = "/bin/sh";
            }
            else if (Application.platform == RuntimePlatform.OSXEditor || 
                     Application.platform == RuntimePlatform.OSXPlayer)
            {
                // Mac
                throw new NotImplementedException();
            }
            else
            {
                // Else...
                throw new NotImplementedException();
            }
            
            ffmpegProc.StartInfo.UseShellExecute = false;
            ffmpegProc.StartInfo.CreateNoWindow = true;
            ffmpegProc.StartInfo.RedirectStandardInput = true;
            ffmpegProc.StartInfo.RedirectStandardOutput = true;
            ffmpegProc.StartInfo.RedirectStandardError = true;
            ffmpegProc.StartInfo.Arguments =
                "-c \"" +
                "ffmpeg -r " + frameRate.ToString() + " -f rawvideo -pix_fmt rgb24 -s " + _screenWidth.ToString() + "x" +
                _screenHeight.ToString() +
                " -i - -threads 0 -preset fast -y " +
                "-crf 21 " + path + "\"";
        
            ffmpegProc.OutputDataReceived += new DataReceivedEventHandler((s, e) => 
            { 
                // print(e.Data);    // this is for debugging 
            });
            ffmpegProc.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                // print(e.Data);     // this is for debugging
            });

            // Start ffmpeg
            ffmpegProc.Start();
            ffmpegProc.BeginErrorReadLine();
            ffmpegProc.BeginOutputReadLine();

            if (ffmpegProc.HasExited)
            {
                // check exit code
                int result = ffmpegProc.ExitCode;
                if (result == 127)
                {
                    // TODO error ffmpeg is not exist
                }
            }

            while (threadIsProcessing) 
            {
                // Dequeue the frame, encode it as a bitmap, and write it to the file
                if(_frameQueue.Count > 0)
                {
                    var ffmpegStream = ffmpegProc.StandardInput.BaseStream;
                
                    byte[] data = _frameQueue.Dequeue(); 
                    ffmpegStream.Write(data, 0, data.Length);
                    ffmpegStream.Flush();
                }
                else
                {
                    if(terminateThreadWhenDone)
                    {
                        break;
                    }

                    Thread.Sleep(1);
                }
            }
        
            // Close ffmpeg
            ffmpegProc.StandardInput.BaseStream.Flush();
            ffmpegProc.StandardInput.BaseStream.Close();
            ffmpegProc.WaitForExit();
            
            if (ffmpegProc.HasExited)
            {
                // check exit code
                int result = ffmpegProc.ExitCode;
                if (result != 0)
                {
                    // TODO error
                }
            }
        }
        
        terminateThreadWhenDone = false;
        threadIsProcessing = false;
        
        print ("SCREENRECORDER IO THREAD FINISHED");
    }
}