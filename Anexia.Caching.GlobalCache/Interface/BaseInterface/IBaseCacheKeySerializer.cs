// ------------------------------------------------------------------------------------------
// <copyright file="IBaseCacheKeySerializer.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Anexia.Caching.GlobalCache.Interface.BaseInterface
{
    public interface IBaseSerializer
    {
        /// <summary>
        ///     Serialize obects
        /// </summary>
        public Func<object, object> SerializeObject { get; }

        public Func<object, string> SerializeToString { get; }

        public Func<object, byte[]> SerializeToByte { get; }

        public Func<object, Task<object>> SerializeObjectAsync { get; }

        public Func<object, Task<string>> SerializeToStringAsync { get; }

        public Func<object, Task<byte[]>> SerializeToByteAsync { get; }

        /// <summary>
        ///     Deserialize object
        /// </summary>
        public T DeserializeObject<T>(object obj);

        public Task<T> DeserializeObjectAsync<T>(object obj);
    }
}