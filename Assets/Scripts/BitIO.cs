using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

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
        public static int GetData<T>(ref byte[] buffer, ref T data, int offset = 0)
        {
            var type = typeof(T);
            
            // if T is enumerator
            if (typeof(T).IsEnum)
                type = Enum.GetUnderlyingType(type);
            
            if (type == typeof(sbyte))
            {
                data = ((sbyte)buffer[offset]).As<T>();
                return offset + sizeof(sbyte);
            }

            if (type == typeof(byte))
            {
                data = buffer[offset].As<T>();
                return offset + sizeof(byte);
            }

            if (type == typeof(short))
            {
                data = BitConverter.ToInt16(buffer, offset).As<T>();
                return offset + sizeof(short);
            }

            if (type == typeof(ushort))
            {
                data = BitConverter.ToUInt16(buffer, offset).As<T>();
                return offset + sizeof(ushort);
            }

            if (type == typeof(int))
            {
                data = BitConverter.ToInt32(buffer, offset).As<T>();
                return offset + sizeof(int);
            }

            if (type == typeof(uint))
            {
                data = BitConverter.ToUInt32(buffer, offset).As<T>();
                return offset + sizeof(uint);
            }

            if (type == typeof(long))
            {
                data = BitConverter.ToInt64(buffer, offset).As<T>();
                return offset + sizeof(long);
            }

            if (type == typeof(ulong))
            {
                data = BitConverter.ToUInt64(buffer, offset).As<T>();
                return offset + sizeof(ulong);
            }
            
            if (type == typeof(float))
            {
                data = BitConverter.ToSingle(buffer, offset).As<T>();
                return offset + sizeof(float);
            }
            
            if (type == typeof(double))
            {
                data = BitConverter.ToDouble(buffer, offset).As<T>();
                return offset + sizeof(double);
            }
            
            // string 
            if (type == typeof(string))
            {
                ulong size = 0;
                offset = GetData<ulong>(ref buffer, ref size, offset);
                data = Encoding.UTF8.GetString(buffer, offset, (int)size).As<T>();
                return offset + (int)size;
            }
            
            throw new NotImplementedException();
        }
        
        public static T As<T>(this object o)
        {
            return (T)o;
        }
    }
}