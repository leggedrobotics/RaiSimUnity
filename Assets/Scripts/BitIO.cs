/*
 * Author: Dongho Kang (kangd@ethz.ch)
 */

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
            
            throw new NotImplementedException();
        }
        
        public static T As<T>(this object o)
        {
            return (T)o;
        }
    }
}