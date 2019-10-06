/*
 * Author: Dongho Kang (kangd@ethz.ch)
 *
 * Inspired by https://gist.github.com/DashW/74d726293c0d3aeb53f4
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    private float speed = 0.5f;
    private float sensitivity = 0.5f;
 
    private Camera cam;
    private Vector3 anchorPoint;
    private Quaternion anchorRot;

    private RenderTexture rt;        // for video recording
    
    private GameObject _selected;    // selected object by clicking

    private bool _isRecording;
    public bool IsRecording
    {
        get => _isRecording;
    }

    // Public Properties
    public int maxFrames; // maximum number of frames you want to record in one video
    public int frameRate = 30; // number of frames to capture per second

    // The Encoder Thread
    private Thread _saverThread;

    // Texture Readback Objects
    private RenderTexture tempRenderTexture;
    private Texture2D tempTexture2D;

    // Timing Data
    private float captureFrameTime;
    private float lastFrameTime;
    private int frameNumber;

    // Encoder Thread Shared Resources
    private Queue<byte[]> frameQueue;
    private string persistentDataPath;
    private int screenWidth;
    private int screenHeight;
    private bool threadIsProcessing;
    private bool terminateThreadWhenDone;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }
    
    void Start () 
    {
        // Set target frame rate (optional)
        Application.targetFrameRate = frameRate;

        // Prepare textures and initial values
        screenWidth = cam.pixelWidth;
        screenHeight = cam.pixelHeight;
		
        tempRenderTexture = new RenderTexture(screenWidth, screenHeight, 0);
        tempTexture2D = new Texture2D(screenWidth, screenHeight, TextureFormat.RGB24, false);
        frameQueue = new Queue<byte[]> ();

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
    
    void OnDisable() 
    {
        // Reset target frame rate
        Application.targetFrameRate = -1;

        // Inform thread to terminate when finished processing frames
        terminateThreadWhenDone = true;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_isRecording)
        {
            if (frameNumber <= maxFrames)
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

                // Capture the frame
                if(framesToCapture > 0)
                {
                    Graphics.Blit (source, tempRenderTexture);
				
                    RenderTexture.active = tempRenderTexture;
                    tempTexture2D.ReadPixels(new Rect(0, 0, Screen.width, Screen.height),0,0);
                    RenderTexture.active = null;
                }

                // Add the required number of copies to the queue
                for(int i = 0; i < framesToCapture && frameNumber <= maxFrames; ++i)
                {
                    frameQueue.Enqueue(tempTexture2D.GetRawTextureData());
                    frameNumber ++;
                }
			
                lastFrameTime = thisFrameTime;

            }
            else //keep making screenshots until it reaches the max frame amount
            {
                // Inform thread to terminate when finished processing frames
                terminateThreadWhenDone = true;
            }

            // Passthrough
            Graphics.Blit (source, destination);
        }
    }

    public void StartRecording()
    {
        _isRecording = true;
        
        // Start a new encoder thread
        threadIsProcessing = true;
        _saverThread = new Thread (SaveVideo);
        _saverThread.Start ();
    }

    public void FinishRecording()
    {
        _isRecording = false;
    }
	
    private void SaveVideo()
    {
        print ("SCREENRECORDER IO THREAD STARTED");

        // Generate file path
        string path = "output.mp4";
		
        var ffmpegProc = new Process();
        ffmpegProc.StartInfo.FileName = "/bin/sh";
        ffmpegProc.StartInfo.UseShellExecute = false;
        ffmpegProc.StartInfo.CreateNoWindow = true;
        ffmpegProc.StartInfo.RedirectStandardInput = true;
        ffmpegProc.StartInfo.RedirectStandardOutput = true;
        ffmpegProc.StartInfo.RedirectStandardError = true;
        ffmpegProc.StartInfo.Arguments = 
            "-c \"" +
            "ffmpeg -r " + frameRate.ToString() + " -f rawvideo -pix_fmt rgb24 -s " + screenWidth.ToString() + "x" + screenHeight.ToString() +
            " -i - -threads 0 -preset fast -y " + 
            // TODO this line makes problem
            //            "-pix_fmt yuv420p" + 
            "-crf 21 -loglevel debug " + path + "\"";
		
//        // this is for debugging
//        ffmpegProc.OutputDataReceived += new DataReceivedEventHandler((s, e) => 
//        { 
//            print(e.Data); 
//        });
//        ffmpegProc.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
//        {
//            print(e.Data);
//        });

        ffmpegProc.Start();
        
//        // this is for debugging
//        ffmpegProc.BeginErrorReadLine();
//        ffmpegProc.BeginOutputReadLine();

        while (threadIsProcessing) 
        {
            // Dequeue the frame, encode it as a bitmap, and write it to the file
            if(frameQueue.Count > 0)
            {
                var ffmpegStream = ffmpegProc.StandardInput.BaseStream;
                
                byte[] data = frameQueue.Dequeue(); 
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

        terminateThreadWhenDone = false;
        threadIsProcessing = false;
        
        ffmpegProc.StandardInput.BaseStream.Close();
        ffmpegProc.WaitForExit();

        print ("SCREENRECORDER IO THREAD FINISHED");
    }
}