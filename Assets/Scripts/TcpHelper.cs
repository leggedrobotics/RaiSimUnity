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
 */

using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ServerStatus GetDataServerStatus()
        {
            return (ServerStatus)(GetDataInt());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ServerMessageType GetDataServerMessageType()
        {
            return (ServerMessageType)(GetDataInt());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RsObejctType GetDataRsObejctType()
        {
            return (RsObejctType)(GetDataInt());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RsShapeType GetDataRsShapeType()
        {
            return (RsShapeType)(GetDataInt());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetDataDouble()
        {
            var data = BitConverter.ToDouble(_buffer, _bufferOffset).As<double>();
            _bufferOffset = _bufferOffset + sizeof(double);
            return data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetDataFloat()
        {
            var data = BitConverter.ToSingle(_buffer, _bufferOffset).As<float>();
            _bufferOffset = _bufferOffset + sizeof(float);
            return data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetDataUlong()
        {
            var data = BitConverter.ToUInt64(_buffer, _bufferOffset).As<ulong>();
            _bufferOffset = _bufferOffset + sizeof(ulong);
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetDataLong()
        {
            var data = BitConverter.ToInt64(_buffer, _bufferOffset).As<long>();
            _bufferOffset = _bufferOffset + sizeof(long);
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetDataUint()
        {
            var data = BitConverter.ToUInt32(_buffer, _bufferOffset).As<uint>();
            _bufferOffset = _bufferOffset + sizeof(uint);
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetDataInt()
        {
            var data = BitConverter.ToInt32(_buffer, _bufferOffset).As<int>();
            _bufferOffset = _bufferOffset + sizeof(int);
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetDataUshort()
        {
            var data = BitConverter.ToUInt16(_buffer, _bufferOffset).As<ushort>();
            _bufferOffset = _bufferOffset + sizeof(ushort);
            return data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetDataShort()
        {
            var data = BitConverter.ToInt16(_buffer, _bufferOffset).As<short>();
            _bufferOffset = _bufferOffset + sizeof(short);
            return data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetDataByte()
        {
            var data = _buffer[_bufferOffset].As<byte>();
            _bufferOffset = _bufferOffset + sizeof(byte);
            return data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte GetDataSbyte()
        {
            var data = ((sbyte)_buffer[_bufferOffset]).As<sbyte>();
            _bufferOffset = _bufferOffset + sizeof(sbyte);
            return data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetDataString()
        {
            ulong size = GetDataUlong();
            var data = Encoding.UTF8.GetString(_buffer, _bufferOffset, (int)size).As<string>();
            _bufferOffset = _bufferOffset + (int)size;
            return data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetDataBool()
        {
            var data = BitConverter.ToBoolean(_buffer, _bufferOffset).As<bool>();
            _bufferOffset = _bufferOffset + sizeof(bool);
            return data;
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