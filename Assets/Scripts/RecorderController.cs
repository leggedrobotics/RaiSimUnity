using System;
using System.Diagnostics;
using System.IO;
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

        private int _width = 1920;
        private int _height = 1080;

        private Process _ffmpegProc = null;

        private String _outFile = "/tmp/test.mp4";
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
            _rt = new RenderTexture(1920, 1080, 16, RenderTextureFormat.ARGB32);
            _rt.Create();
            gameObject.GetComponent<Camera>().targetTexture = _rt;
            
            // ffmpeg 
            // TODO what about Mac and Windows?
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "/bin/sh";
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.Arguments = 
                "-c \"" +
                "ffmpeg -r " + _frameRate.ToString() + " -f rawvideo -pix_fmt rgb24 -s " + _width.ToString() + "x" + _height.ToString() +
                " -i - -threads 0 -preset fast -y -pix_fmt yuv420p -crf 21 " + _outFile + "\"";
            _ffmpegProc = Process.Start(psi);
            
            if (_ffmpegProc != null)
            {
                // TODO exception
//                print(_ffmpegProc.StandardError.ReadToEnd());
                
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
                _ffmpegProc.StandardInput.BaseStream.Close();
                _ffmpegProc.Close();
            }
            
            _isRecording = false;
        }

        private void WriteFrame()
        {
            // render recorder screen
            gameObject.GetComponent<Camera>().Render();

            // read pixels
            RenderTexture.active = _rt;
            var texture = new Texture2D(_width, _height, TextureFormat.ARGB32, false);
            texture.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;

            // write to ffmpeg
            _ffmpegProc.StandardInput.BaseStream.Write(texture.GetRawTextureData(), 0, 0);
            _ffmpegProc.StandardInput.BaseStream.Flush();
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