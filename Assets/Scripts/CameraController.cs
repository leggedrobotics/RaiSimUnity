/*
 * Author: Dongho Kang (kangd@ethz.ch)
 *
 * Inspired by https://gist.github.com/DashW/74d726293c0d3aeb53f4
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private Vector3 anchorPoint;
    private Quaternion anchorRot;

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
    private int _saverExitCode = 0;
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
    private int screenWidth;
    private int screenHeight;
    private bool terminateThreadWhenDone;
    private bool threadIsProcessing;
    
    // Video name
    private string _outputName = "out";
    private int _outputIdx = 1;
    
    public bool ThreadIsProcessing
    {
        get => threadIsProcessing;
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        
        // Check if FFMPEG available
        int ffmpegExitCode = FFMPEGTest();
        if (ffmpegExitCode == 0)
            videoAvailable = true;
    }

    void Start () 
    {
        // Set target frame rate (optional)
        Application.targetFrameRate = frameRate;

        // Prepare textures and initial values
        screenWidth = cam.pixelWidth;
        screenHeight = cam.pixelHeight;
		
        _tempRenderTexture = new RenderTexture(screenWidth, screenHeight, 0);
        _tempTexture2D = new Texture2D(screenWidth, screenHeight, TextureFormat.RGB24, false);
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
        
        if (!EventSystem.current.IsPointerOverGameObject ()) {
            // only do this if mouse pointer is not on the GUI
            
            // select object by left click
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (_selected != null)
                    {
                        // former selected object
                        foreach (var ren in _selected.GetComponentsInChildren<Renderer>())
                        {
                            ren.material.shader = Shader.Find("Standard");
                        }
                    }
                
                    _selected = hit.transform.parent.gameObject;

                    foreach (var ren in _selected.GetComponentsInChildren<Renderer>())
                    {
                        ren.material.shader = Shader.Find("Outlined/UltimateOutline");
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
                        ren.material.shader = Shader.Find("Standard");
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
    }
    
    void Update() {
        
        // follow selected object 
        if (_selected != null)
        {
            gameObject.transform.rotation =
                Quaternion.LookRotation(_selected.transform.position - gameObject.transform.position);
        }
    }
    
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Check if render target size has changed, if so, terminate
        if(source.width != screenWidth || source.height != screenHeight)
        {
            threadIsProcessing = false;
            this.enabled = false;
            throw new UnityException("ScreenRecorder render target size has changed!");
        }

        // Calculate number of video frames to produce from this game frame
        // Generate 'padding' frames if desired framerate is higher than actual framerate
        float thisFrameTime = Time.time;
        int framesToCapture = ((int)(thisFrameTime / captureFrameTime)) - ((int)(lastFrameTime / captureFrameTime));

        if (_isRecording)
        {
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

    public void StartRecording()
    {
        // Kill thread if it's still alive
        if (_saverThread != null && (threadIsProcessing || _saverThread.IsAlive)) {
            threadIsProcessing = false;
            _saverThread.Join();
        }
        
        if (threadIsProcessing)
        {
            // TODO exception... something wrong...
            print("oops...");
        }
        else
        {
            _isRecording = true;
            frameNumber = 0;
        
            // Start a new encoder thread
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

        return -1;
    }

    private void SaveVideo()
    {
        print ("SCREENRECORDER IO THREAD STARTED");

        // Generate file path
        string path = _outputName + _outputIdx++.ToString() + ".mp4";

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
                "ffmpeg -r " + frameRate.ToString() + " -f rawvideo -pix_fmt rgb24 -s " + screenWidth.ToString() + "x" +
                screenHeight.ToString() +
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