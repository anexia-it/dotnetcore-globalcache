// ------------------------------------------------------------------------------------------
// <copyright file="BaseTextJsonSerializer.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using Anexia.Caching.GlobalCache.Interface.BaseInterface;
using MSJson = System.Text.Json;

namespace Anexia.Caching.GlobalCache.Abstraction.BaseAbstraction
{
    /// <summary>
    ///     I created delegates because they do not allocate functions as much as normal functions
    ///     In other words it's faster
    /// </summary>
    public class BaseTextJsonSerializer : IBaseSerializer
    {
        /// <summary>
        ///     Serialization of object to string as an object
        /// </summary>
        Func<object, object> IBaseSerializer.SerializeObject { get; } = obj =>
        {
            if (obj is string objStr) // i assume keys are mostly strings so this should be faster
            {
                return objStr;
            }

            return MSJson.JsonSerializer.Serialize(obj);
        };

        /// <summary>
        ///     Serialization of object to string as a string
        /// </summary>
        Func<object, string> IBaseSerializer.SerializeToString { get; } = obj =>
        {
            if (obj is string objStr) // i assume keys are mostly strings so this should be faster
            {
                return objStr;
            }

            return MSJson.JsonSerializer.Serialize(obj);
        };

        /// <summary>
        ///     Serialization of object to bytearray
        /// </summary>
        Func<object, byte[]> IBaseSerializer.SerializeToByte { get; } = obj =>
        {
            if (obj.GetType() == typeof(byte[])) // i assume nobody sends a key as an array of bytes so i use this approach
            {
                return obj as byte[];
            }

            return MSJson.JsonSerializer.SerializeToUtf8Bytes(obj);
        };

        /// <summary>
        ///     Serialization of object to bytearray in asynchronous call
        ///     String as Object
        /// </summary>
        Func<object, Task<object>> IBaseSerializer.SerializeObjectAsync { get; } = async obj =>
        {
            if (obj is string objStr) // i assume keys are mostly strings so this should be faster
            {
                return objStr;
            }

            await using var stream = new MemoryStream();
            await MSJson.JsonSerializer.SerializeAsync(stream, obj);
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        };

        /// <summary>
        ///     Serialization of object to bytearray in asynchronous call
        ///     String as String
        /// </summary>
        Func<object, Task<string>> IBaseSerializer.SerializeToStringAsync { get; } = async obj =>
        {
            if (obj is string objStr) // i assume keys are mostly strings so this should be faster
            {
                return objStr;
            }

            await using var stream = new MemoryStream();
            await MSJson.JsonSerializer.SerializeAsync(stream, obj);
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        };

        /// <summary>
        ///     Serialization of object to bytearray in asynchronous call
        ///     As byte array
        /// </summary>
        Func<object, Task<byte[]>> IBaseSerializer.SerializeToByteAsync { get; } = async obj =>
        {
            if (obj.GetType() == typeof(byte[])) // i assume nobody sends a key as an array of bytes so i use this approach
            {
                return obj as byte[];
            }

            await using var stream = new MemoryStream();
            await MSJson.JsonSerializer.SerializeAsync(stream, obj);
            stream.Position = 0;
            return stream.ToArray();
        };

        /// <summary>
        ///     Deserializes objects which can be strings or byte arrays
        /// </summary>
        /// <typeparam name="T">Type which is preferred</typeparam>
        /// <param name="obj">Object to deserialize</param>
        /// <returns>Object type with correct type identification or default(T)</returns>
        public T DeserializeObject<T>(object obj)
        {
            if (obj is byte[] byteArrObj)
            {
                return MSJson.JsonSerializer.Deserialize<T>(byteArrObj);
            }

            if (obj is string stringObj)
            {
                return MSJson.JsonSerializer.Deserialize<T>(stringObj);
            }

            return default;
        }

        /// <summary>
        ///     Deserializes objects which can be strings or byte arrays asynchronously
        /// </summary>
        /// <typeparam name="T">Type which is preferred</typeparam>
        /// <param name="obj">Object to deserialize</param>
        /// <returns>Object type with correct type identification or default(T)</returns>
        public async Task<T> DeserializeObjectAsync<T>(object obj)
        {
            if (obj is byte[] byteArrObj)
            {
                using var stream = new MemoryStream(byteArrObj);
                return await MSJson.JsonSerializer.DeserializeAsync<T>(stream);
            }

            if (obj is string stringObj)
            {
                return MSJson.JsonSerializer.Deserialize<T>(stringObj);
            }

            return default;
        }
    }
}