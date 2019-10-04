using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace raisimUnity
{
    public class RecorderController : MonoBehaviour
    {
        private RenderTexture _rt;

        private int _frameRate = 60;
        private int _frameCount = 0;
        private float _startTime;

        private int _width = 960;
        private int _height = 540;

        private Process _ffmpegProc = null;

        private String _outFile = "test.mp4";
        private bool _isRecording = false;
        public bool IsRecording
        {
            get => _isRecording;
        }

        float FrameTime {
            get { return _startTime + (_frameCount - 0.5f) / _frameRate; }
        }

        public void StartRecording()
        {
            // render texture depth check
            _rt = new RenderTexture(_width, _height, 16, RenderTextureFormat.ARGB32);
            _rt.Create();
            gameObject.GetComponent<Camera>().targetTexture = _rt;
            
            // ffmpeg 
            // TODO what about Mac and Windows?
            _ffmpegProc = new Process();
            _ffmpegProc.StartInfo.FileName = "/bin/sh";
            _ffmpegProc.StartInfo.UseShellExecute = false;
            _ffmpegProc.StartInfo.CreateNoWindow = true;
            _ffmpegProc.StartInfo.RedirectStandardInput = true;
            _ffmpegProc.StartInfo.RedirectStandardOutput = false;
            _ffmpegProc.StartInfo.RedirectStandardError = true;
            _ffmpegProc.StartInfo.Arguments = 
                "-c \"" +
                "ffmpeg -r " + _frameRate.ToString() + " -f rawvideo -pix_fmt argb -s " + _width.ToString() + "x" + _height.ToString() +
                " -i - -threads 0 -preset fast -y -pix_fmt yuv420p -crf 21 " + _outFile + "\"";

            // this is for debugging
            _ffmpegProc.OutputDataReceived += new DataReceivedEventHandler((s, e) => 
            { 
                print(e.Data); 
            });
            _ffmpegProc.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                print(e.Data);
            });

            _ffmpegProc.Start();
            _ffmpegProc.BeginErrorReadLine();
//            _ffmpegProc.BeginOutputReadLine();
            
            if (_ffmpegProc != null)
            {
                // TODO exception
                _isRecording = true;
                _startTime = Time.time;
                _frameCount = 0;
            }
            else
            {
                // TODO exception
            }
        }

        public void FinishRecording()
        {
            if (_isRecording)
            {
                _ffmpegProc.StandardInput.Flush();
                _ffmpegProc.StandardInput.BaseStream.Close();
//                _ffmpegProc.Close();
            }
            
            _isRecording = false;
        }

        private void WriteFrame()
        {
            // render recorder screen
//            gameObject.GetComponent<Camera>().Render();

            // read pixels
            RenderTexture.active = _rt;
            var texture = new Texture2D(_width, _height, TextureFormat.ARGB32, false);
            texture.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            texture.Apply();
//            RenderTexture.active = null;

            byte[] bytes = texture.EncodeToPNG();
            Destroy(texture);

//            // For testing purposes, also write to a file in the project folder
            File.WriteAllBytes("test.png", bytes);

            // write to ffmpeg
//            StreamWriter sw = _ffmpegProc.StandardInput;
//            var ffmpegIn = _ffmpegProc.StandardInput.BaseStream;
//            sw.Write(texture.GetRawTextureData());
//            sw.Flush();
        }

        void FixedUpdate()
        {
            // update camera pose 
            gameObject.transform.position = Camera.main.transform.position;
            gameObject.transform.rotation = Camera.main.transform.rotation;

            if (_isRecording)
            {
                var gap = Time.time - FrameTime;
                var delta = 1.0f / _frameRate;

                if (gap < 0)
                {
                    // no update
                }
                else if (gap < delta)
                {
                    // single frame
                    WriteFrame();
                    _frameCount++;
                }
                else if (gap < delta * 2)
                {
                    // two frames
                    WriteFrame();
                    WriteFrame();
                    _frameCount += 2;

                }
                else
                {
                    // warning!
                }
            }
        }
        
        void OnApplicationQuit()
        {
            // close tcp client
            FinishRecording();
        }
    }
}