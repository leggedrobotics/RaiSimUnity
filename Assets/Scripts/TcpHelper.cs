using System;
using System.Net.Sockets;
using UnityEngine;

namespace raisimUnity
{
    public class TcpHelper
    {
        private const int MaxBufferSize = 33554432;
        private const int MaxPacketSize = 4096;
        private const int FooterSize = sizeof(Byte);

        // Tcp Address
        private string _tcpAddress = "127.0.0.1";
        private int _tcpPort = 8080;

        // Tcp client and stream
        private TcpClient _client = null;
        private NetworkStream _stream = null;
        
        // Buffer
        private byte[] _buffer;
        private int _bufferOffset = 0;
        
        // Read data timer
        private float readDataTime = 0;

        public TcpHelper()
        {
            _tcpAddress = "127.0.0.1";
            _tcpPort = 8080;
            
            _buffer = new byte[MaxBufferSize];
        }


        public void EstablishConnection()
        {
            try
            {
                // create tcp client and stream
                if (_client == null || !_client.Connected)
                {
                    _client = new TcpClient(_tcpAddress, _tcpPort);
                    _stream = _client.GetStream();
                }
            }
            catch (Exception e)
            {
                // connection cannot be established
                throw new RsuTcpConnectionException(e.Message);
            }
        }
        
        public void CloseConnection()
        {
            try
            {
                // clear tcp stream and client
                if (_stream != null)
                {
                    _stream.Close();
                    _stream = null;
                }

                if (_client != null)
                {
                    _client.Close();
                    _client = null;
                }
            }
            catch (Exception e)
            {
            }
        }
        
        public bool CheckConnection()
        {
            try
            {
                if( _client!=null && _client.Client!=null && _client.Client.Connected )
                {
                    if( _client.Client.Poll(0, SelectMode.SelectRead) )
                    {
                        if( _client.Client.Receive(_buffer, SocketFlags.Peek)==0 )
                            return false;
                        else
                            return true;
                    }
                    else
                        return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }
        
        public int ReadData()
        {
            // Clear buffer first
            Array.Clear(_buffer, 0, MaxBufferSize);

            // Wait until stream data is available 
            readDataTime = Time.realtimeSinceStartup;
            while (!_stream.DataAvailable)
            {
                if (Time.realtimeSinceStartup - readDataTime > 5.0f)
                    // If data is not available until timeout, return error
                    return -1;
            }
            
            int numBytes = 0;
            Byte footer = Convert.ToByte('c');
            while (footer == Convert.ToByte('c'))
            {
                int valread = _stream.Read(_buffer, numBytes, MaxPacketSize);
                if (valread == 0) break;
                footer = _buffer[numBytes + MaxPacketSize - FooterSize];
                numBytes += valread - FooterSize;
            }

            _bufferOffset = 0;
            return numBytes;
        }

        public int WriteData(Byte[] data)
        {
            while (!_stream.CanWrite)
            {
                // wait until stream can write
                // TODO what to do when it is stucked here....
            }
            
            _stream.Write(data, 0, data.Length);
            return 0;
        }

        public T GetData<T>()
        {
            return BitIO.GetData<T>(ref _buffer, ref _bufferOffset);
        }


        //**************************************************************************************************************
        //  Getter and Setters 
        //**************************************************************************************************************
        
        public bool DataAvailable
        {
            get => _client != null && _client.Connected && _stream != null;
        }

        public bool Connected
        {
            get => _client != null && _client.Connected;
        }

        public string TcpAddress
        {
            get => _tcpAddress;
            set => _tcpAddress = value;
        }

        public int TcpPort
        {
            get => _tcpPort;
            set => _tcpPort = value;
        }
    }
}