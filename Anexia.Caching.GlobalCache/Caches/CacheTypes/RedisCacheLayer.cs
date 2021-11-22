// ------------------------------------------------------------------------------------------
// <copyright file="RedisCacheLayer.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Anexia.Caching.GlobalCache.Abstraction.BaseAbstraction;
using Anexia.Caching.GlobalCache.Abstraction.RedisCacheExtension;
using Anexia.Caching.GlobalCache.Config;
using Anexia.Caching.GlobalCache.Config.Model;
using Anexia.Caching.GlobalCache.Constants;
using Anexia.Caching.GlobalCache.Interface.BaseInterface;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Anexia.Caching.GlobalCache.Caches.CacheTypes
{
    /// <summary>
    /// Redis Cache layer
    /// </summary>
    /// <typeparam name="T">Type of return values</typeparam>
    internal class RedisCacheLayer<T> : IBaseCache<T>
    {
        private readonly IDistributedCache _cache;
        private readonly string _typeKey;

        private RedisCacheLayer(
            IBaseSerializer serializer = default,
            string keyType = null)
        {
            _serializer = serializer ?? new BaseTextJsonSerializer();
            _typeKey = keyType ?? typeof(T).Name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheLayer{T}"/> class.
        /// </summary>
        /// <param name="options">Redis Cache options</param>
        /// <param name="serializer">Serializer class the de-/serialize objects and keys</param>
        /// <param name="keyType">Prefix for keys (should always be added)</param>
        internal RedisCacheLayer(
            RedisCacheOptions options,
            IBaseSerializer serializer = default,
            string keyType = null)
            : this(serializer)
        {
            _cache = options != null
                ? new RedisExtendedCache(options)
                : new RedisExtendedCache(RedisConfig.ReadRedisFromConfig());
            _typeKey = keyType ?? typeof(T).Name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheLayer{T}"/> class.
        /// </summary>
        /// <param name="config">Startup config to get redis options from config file</param>
        /// <param name="serializer">Serializer class the de-/serialize objects and keys</param>
        /// <param name="keyType">Prefix for keys (should always be added)</param>
        internal RedisCacheLayer(
            IConfiguration config,
            IBaseSerializer serializer = default,
            string keyType = null)
            : this(serializer)
        {
            _cache = config != null
                ? new RedisExtendedCache(
                    RedisConfig.ReadRedisFromConfig(
                        JsonSerializer.Deserialize<RedisConfigModel>(config.GetSection("RedisConfiguration").Value)))
                : new RedisExtendedCache(RedisConfig.ReadRedisFromConfig());
            _typeKey = keyType ?? typeof(T).Name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheLayer{T}"/> class.
        /// </summary>
        /// <param name="config">Redis config option from config file fragment</param>
        /// <param name="serializer">Serializer class the de-/serialize objects and keys</param>
        /// <param name="keyType">Prefix for keys (should always be added)</param>
        internal RedisCacheLayer(
            IOptions<RedisConfigModel> config,
            IBaseSerializer serializer = default,
            string keyType = null)
            : this(serializer, keyType)
        {
            _cache = config != null
                ? new RedisCache(RedisConfig.ReadRedisFromConfig(config?.Value))
                : new RedisCache(RedisConfig.ReadRedisFromConfig());
            _typeKey = keyType ?? typeof(T).Name;
        }

        /// <summary>
        /// Serializer class for keys and values
        /// </summary>
        private protected IBaseSerializer _serializer { get; set; }

        /// <inheritdoc/>
        public void Dispose() => ((RedisExtendedCache)_cache)?.Dispose();

        /// <inheritdoc/>
        public Task InsertAsync(
            object key,
            T element,
            DateTime? duration = null,
            TimeSpan? slidingExpiration = null)
        {
            var absoluteExpirationDate = CalculateAbsoluteExpirationDate(duration);
            var tasks = new Task[2];
            tasks[0] = _serializer.SerializeToStringAsync(key).ContinueWith(task => CreateKey(task.Result));
            tasks[1] = _serializer.SerializeToByteAsync(element);
            return Task.WhenAll(tasks).ContinueWith(
                task =>
                {
                    _cache.SetAsync(
                        ((Task<string>)tasks[0]).Result,
                        ((Task<byte[]>)tasks[1]).Result,
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = absoluteExpirationDate,
                            SlidingExpiration = CalculateSlidingExpirationTime(absoluteExpirationDate, slidingExpiration),
                        });
                },
                TaskScheduler.Current);
        }

        /// <inheritdoc/>
        public Task InsertAsync(
            object key,
            T element,
            int duration,
            TimeSpan? slidingExpiration = null)
        {
            return InsertAsync(
                key,
                element,
                CalculateAbsoluteExpirationDate(duration),
                slidingExpiration);
        }

        /// <inheritdoc/>
        public void Insert(
            object key,
            T element,
            DateTime? duration = null,
            TimeSpan? slidingExpiration = null)
        {
            var absoluteExpirationDate = CalculateAbsoluteExpirationDate(duration);
            _cache.Set(
                CreateKey(_serializer.SerializeToString(key)),
                _serializer.SerializeToByte(element),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = absoluteExpirationDate,
                    SlidingExpiration = CalculateSlidingExpirationTime(absoluteExpirationDate, slidingExpiration),
                });
        }

        /// <inheritdoc/>
        public void Insert(
            object key,
            T element,
            int duration,
            TimeSpan? slidingExpiration = null) =>
            Insert(key, element, CalculateAbsoluteExpirationDate(duration), slidingExpiration);

        #region Get

        /// <inheritdoc/>
        public T Get(object key)
        {
            var spanByteArr = _cache.Get(CreateKey(_serializer.SerializeToString(key)));
            return spanByteArr != default(Span<byte>) ? _serializer.DeserializeObject<T>(spanByteArr) : default;
        }

        /// <inheritdoc/>
        public Task<T> GetAsync(object key) =>
            _serializer.SerializeToStringAsync(key)
                .ContinueWith(task => _cache.GetAsync(CreateKey(task.Result)), TaskScheduler.Current).Unwrap()
                .ContinueWith(
                    async task =>
                        await _serializer.DeserializeObjectAsync<T>(task.Result),
                    TaskScheduler.Current).Unwrap();

        /// <inheritdoc/>
        public ICollection GetAllKeys() => ((RedisExtendedCache)_cache).GetAllKeysAsync(_typeKey).Result;

        /// <inheritdoc/>
        public List<T> GetAllValues() => throw new NotImplementedException();

        /// <inheritdoc/>
        public bool Remove(object key)
        {
            if (!HasKey(key))
            {
                return false;
            }

            _cache.Remove(CreateKey(_serializer.SerializeToString(key)));
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveAsync(object key)
        {
            if (!await HasKeyAsync(key))
            {
                return false;
            }

            _ = _cache.RemoveAsync(CreateKey(_serializer.SerializeToString(key)));
            return true;
        }

        /// <inheritdoc/>
        public bool HasKey(object key)
        {
            return HasKeyAsync(key).Result;
        }

        /// <inheritdoc/>
        public bool HasKey(object key, out T result)
        {
            var hasKey = HasKey(key);
            result = hasKey ? Get(key) : default;
            return hasKey;
        }

        /// <inheritdoc/>
        public Task<bool> HasKeyAsync(object key)
        {
            return ((RedisExtendedCache)_cache).ConnectHasKeyAsync(CreateKey(_serializer.SerializeToString(key)));
        }

        #endregion

        #region Private

        /// <summary>
        ///     Creates absolute expiration date by duration in minutes
        /// </summary>
        /// <param name="duration">Absolute expiration in minutes</param>
        /// <returns>Absolute expiration date for the cache</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DateTime CalculateAbsoluteExpirationDate(int duration)
        {
            return DateTime.UtcNow.AddMinutes(
                    duration > 0 ?
                    duration :
                    BaseConstants.DEFAULT_CACHE_TIME);
        }

        /// <summary>
        ///     Creates absolute expiration date.
        ///     If given expiration date is null, date will be set 2 hours in the future
        /// </summary>
        /// <param name="expirationDate">Absolute expiration date</param>
        /// <returns>Absolute expiration date for the cache</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DateTime CalculateAbsoluteExpirationDate(DateTime? expirationDate)
        {
            return expirationDate ?? DateTime.UtcNow.AddMinutes(BaseConstants.DEFAULT_CACHE_TIME);
        }

        /// <summary>
        ///     Creates sliding expiration time.
        ///     If given sliding expiration time is null, expiration will be set
        ///     half of the time until absolute expiration date
        /// </summary>
        /// <param name="absoluteExpirationDate">Absolute expiration date</param>
        /// <param name="slidingExpirationTime">Sliding expiration time</param>
        /// <returns>Sliding expiration time for the cache</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TimeSpan CalculateSlidingExpirationTime(
            DateTime absoluteExpirationDate,
            TimeSpan? slidingExpirationTime = null)
        {
            if (slidingExpirationTime != null)
            {
                return slidingExpirationTime.Value;
            }

            var timeUntilExpiration = absoluteExpirationDate - DateTime.UtcNow;
            return new TimeSpan(timeUntilExpiration.Ticks / 2);
        }

        /// <summary>
        ///     Creates key needed to get the result
        /// </summary>
        /// <param name="key">Key value</param>
        /// <returns>String origin key</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string CreateKey(string key) => $"{_typeKey}:{key}";

        #endregion
    }
}