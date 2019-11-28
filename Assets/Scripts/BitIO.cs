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
using System.Text;

namespace raisimUnity
{
    public static class BitIO
    {
        /// <summary>
        /// Get data from buffer. 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="data"></param>
        /// <param name="offset">buffer offset before data</param>
        /// <typeparam name="T">type of data</typeparam>
        /// <returns>buffer offset after data</returns>
        /// <exception cref="NotImplementedException"></exception>
        public static T GetData<T>(ref byte[] buffer, ref int offset)
        {
            var type = typeof(T);
            
            // if T is enumerator
            if (typeof(T).IsEnum)
                type = Enum.GetUnderlyingType(type);
            
            if (type == typeof(sbyte))
            {
                var data = ((sbyte)buffer[offset]).As<T>();
                offset = offset + sizeof(sbyte);
                return data;
            }

            if (type == typeof(byte))
            {
                var data = buffer[offset].As<T>();
                offset = offset + sizeof(byte);
                return data;
            }

            if (type == typeof(short))
            {
                var data = BitConverter.ToInt16(buffer, offset).As<T>();
                offset = offset + sizeof(short);
                return data;
            }

            if (type == typeof(ushort))
            {
                var data = BitConverter.ToUInt16(buffer, offset).As<T>();
                offset = offset + sizeof(ushort);
                return data;
            }

            if (type == typeof(int))
            {
                var data = BitConverter.ToInt32(buffer, offset).As<T>();
                offset = offset + sizeof(int);
                return data;
            }

            if (type == typeof(uint))
            {
                var data = BitConverter.ToUInt32(buffer, offset).As<T>();
                offset = offset + sizeof(uint);
                return data;
            }

            if (type == typeof(long))
            {
                var data = BitConverter.ToInt64(buffer, offset).As<T>();
                offset = offset + sizeof(long);
                return data;
            }

            if (type == typeof(ulong))
            {
                var data = BitConverter.ToUInt64(buffer, offset).As<T>();
                offset = offset + sizeof(ulong);
                return data;
            }
            
            if (type == typeof(float))
            {
                var data = BitConverter.ToSingle(buffer, offset).As<T>();
                offset = offset + sizeof(float);
                return data;
            }
            
            if (type == typeof(double))
            {
                var data = BitConverter.ToDouble(buffer, offset).As<T>();
                offset = offset + sizeof(double);
                return data;
            }
            
            // string 
            if (type == typeof(string))
            {
                ulong size = GetData<ulong>(ref buffer, ref offset);
                var data = Encoding.UTF8.GetString(buffer, offset, (int)size).As<T>();
                offset = offset + (int)size;
                return data;
            }

            if (type == typeof(bool))
            {
                var data = BitConverter.ToBoolean(buffer, offset).As<T>();
                offset = offset + sizeof(bool);
                return data;
            }
            
            throw new NotImplementedException();
        }
        
        public static T As<T>(this object o)
        {
            return (T)o;
        }
    }
}