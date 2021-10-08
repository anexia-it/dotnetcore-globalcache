// ------------------------------------------------------------------------------------------
// <copyright file="BaseCache.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ACIM.Sales.Core.Helper.Cache.BaseInterface;
using Anexia.Caching.GlobalCache.Caches.CacheTypes;
using Anexia.Caching.GlobalCache.Config;
using Anexia.Caching.GlobalCache.Config.Model;
using Anexia.Caching.GlobalCache.Interface.BaseInterface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Anexia.Caching.GlobalCache.Abstraction.BaseCache
{
    /// <summary>
    /// Base cache instance with standard functions
    /// </summary>
    /// <typeparam name="T">Type of the returned value</typeparam>
    public class BaseCache<T> : IBaseCache<T>
    {
        private readonly IBaseCache<T> _cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCache{T}"/> class.
        ///     If there is configuration for Redis then redis
        ///     Otherwise memory cache
        /// </summary>
        /// <param name="serializer">Serializer for key and values</param>
        /// <param name="typeKey">Type for redis to use as key</param>
        public BaseCache(
            IBaseSerializer serializer = default,
            string typeKey = null)
        {
            var config = RedisConfig.ReadRedisFromConfig();
            if (config != null)
            {
                _cache = new RedisCacheLayer<T>(config, serializer, typeKey);
            }
            else
            {
                _cache = new MemoryCacheLayer<T>(serializer: serializer);
            }
        }

        /// <summary>
        ///     Just Redis Cache
        /// </summary>
        /// <param name="appConfig">Configuration from app settings</param>
        /// <param name="serializer">Serializer for key and values</param>
        /// <param name="typeKey">Type for redis to use as key</param>
        public BaseCache(
            IConfiguration appConfig,
            IBaseSerializer serializer = default,
            string typeKey = null)
        {
            _cache = new RedisCacheLayer<T>(appConfig, serializer, typeKey);
        }

        /// <summary>
        ///     Just Redis Cache, fastest way
        /// </summary>
        /// <param name="config">Options parameter from startup</param>
        /// <param name="serializer">Serializer for key and values</param>
        /// <param name="typeKey">Type for redis to use as key</param>
        public BaseCache(
            IOptions<RedisConfigModel> config,
            IBaseSerializer serializer = default,
            string typeKey = null)
        {
            _cache = new RedisCacheLayer<T>(config, serializer, typeKey);
        }

        /// <summary>
        ///     Just Redis Cache, fastest way
        /// </summary>
        /// <param name="config">Options parameter from startup</param>
        /// <param name="serializer">Serializer for key and values</param>
        /// <param name="typeKey">Type for redis to use as key</param>
        public BaseCache(
            RedisConfigModel config,
            IBaseSerializer serializer = default,
            string typeKey = null)
        {
            _cache = new RedisCacheLayer<T>(config, serializer, typeKey);
        }

        /// <summary>
        ///     Just memory bases cache
        /// </summary>
        /// <param name="sizeLimit">Cache size</param>
        /// <param name="serializer">Serializer for key and values</param>
        public BaseCache(
            long? sizeLimit = null,
            IBaseSerializer serializer = default)
        {
            _cache = new MemoryCacheLayer<T>(sizeLimit, serializer);
        }

        /// <inheritdoc/>
        public void Dispose() => _cache?.Dispose();

        /// <inheritdoc/>
        public T Get(object key) => _cache.Get(key);

        /// <inheritdoc/>
        public ICollection GetAllKeys() => _cache.GetAllKeys();

        /// <inheritdoc/>
        public List<T> GetAllValues() => _cache.GetAllValues();

        /// <inheritdoc/>
        public Task<T> GetAsync(object key) => _cache.GetAsync(key);

        /// <inheritdoc/>
        public bool HasKey(object key) => _cache.HasKey(key);

        /// <inheritdoc/>
        public bool HasKey(object key, out T result) => _cache.HasKey(key, out result);

        /// <inheritdoc/>
        public Task<bool> HasKeyAsync(object key) => Task.Run(() => HasKey(key));

        /// <inheritdoc/>
        public void Insert(
            object key,
            T element,
            DateTime? duration = null,
            TimeSpan? slidingExpiration = null) =>
            _cache.Insert(key, element, duration, slidingExpiration);

        /// <inheritdoc/>
        public void Insert(
            object key,
            T element,
            int duration,
            TimeSpan? slidingExpiration = null) => 
            _cache.Insert(key, element, duration, slidingExpiration);

        /// <inheritdoc/>
        public Task InsertAsync(
            object key,
            T element,
            DateTime? duration = null,
            TimeSpan? slidingExpiration = null) =>
            _cache.InsertAsync(key, element, duration, slidingExpiration);

        /// <inheritdoc/>
        public Task InsertAsync(
            object key,
            T element,
            int duration,
            TimeSpan? slidingExpiration = null) =>
            _cache.InsertAsync(key, element, duration, slidingExpiration);

        /// <inheritdoc/>
        public bool Remove(object key) => _cache.Remove(key);

        /// <inheritdoc/>
        public Task<bool> RemoveAsync(object key) => _cache.RemoveAsync(key);
    }
}