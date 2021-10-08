// ------------------------------------------------------------------------------------------
// <copyright file="MemoryCacheLayer.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ACIM.Sales.Core.Helper.Cache.BaseInterface;
using Anexia.Caching.GlobalCache.Abstraction.BaseAbstraction;
using Anexia.Caching.GlobalCache.Constants;
using Anexia.Caching.GlobalCache.Interface.BaseInterface;
using Microsoft.Extensions.Caching.Memory;

namespace Anexia.Caching.GlobalCache.Caches.CacheTypes
{
    /// <summary>
    /// Memory Cache Instance layer
    /// </summary>
    /// <typeparam name="T">The type which should be returned from the functions</typeparam>
    public class MemoryCacheLayer<T> : IBaseCache<T>
    {
        private FieldInfo EntriesInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheLayer{T}"/> class.
        /// </summary>
        /// <param name="sizeLimit">Cache size limit in bytes</param>
        /// <param name="serializer">Serialization class which should be used with the cache instance</param>
        internal MemoryCacheLayer(
            long? sizeLimit = null,
            IBaseSerializer serializer = default)
        {
            Cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = sizeLimit });
            _serializer = serializer ?? new BaseTextJsonSerializer();
        }

        /// <summary>
        /// Gets or sets Memory Cache object
        /// </summary>
        private protected MemoryCache Cache { get; set; }

        /// <summary>
        /// Gets or sets Serialization class for Keys
        /// </summary>
        private protected IBaseSerializer _serializer { get; set; }

        #region Inserts

        /// <summary>
        ///     Insert Cache with expiration date
        /// </summary>
        /// <param name="key">Key object (only serializable objects)</param>
        /// <param name="element">Value objects (only serializable objects)</param>
        /// <param name="duration">Absolute expiration date</param>
        /// <param name="slidingExpiration">Sliding expiration time span</param>
        public void Insert(
            object key,
            T element,
            DateTime? duration = null,
            TimeSpan? slidingExpiration = null) =>
            Cache.Set(
            _serializer.SerializeObject(key),
            element,
            GetMemoryCacheEntryOptions(duration, slidingExpiration));

        /// <summary>
        ///     Insert Cache with expiration minutes
        /// </summary>
        /// <param name="key">Key object (only serializable objects)</param>
        /// <param name="element">Value objects (only serializable objects)</param>
        /// <param name="duration">Absolute expiration minutes</param>
        /// <param name="slidingExpiration">Sliding expiration time span</param>
        public void Insert(
            object key,
            T element,
            int duration,
            TimeSpan? slidingExpiration = null) => Insert(
            _serializer.SerializeObject(key),
            element,
            CalculateAbsoluteExpirationDate(duration),
            slidingExpiration);

        /// <summary>
        ///     Insert Cache with expiration date
        /// </summary>
        /// <param name="key">Key object (only serializable objects)</param>
        /// <param name="element">Value objects (only serializable objects)</param>
        /// <param name="duration">Absolute expiration date</param>
        /// <param name="slidingExpiration">Sliding expiration time span</param>
        /// <returns>Task on which the function operates</returns>
        public Task InsertAsync(
            object key,
            T element,
            DateTime? duration = null,
            TimeSpan? slidingExpiration = null) =>
            _serializer.SerializeObjectAsync(key)
                .ContinueWith(
                    task => Insert(
                        task.Result,
                        element,
                        duration,
                        slidingExpiration));

        /// <summary>
        ///     Insert Cache with expiration minutes
        /// </summary>
        /// <param name="key">Key object (only serializable objects)</param>
        /// <param name="element">Value objects (only serializable objects)</param>
        /// <param name="duration">Absolute expiration minutes</param>
        /// <param name="slidingExpiration">Sliding expiration time span</param>
        /// <returns>Task on which the function operates</returns>
        public Task InsertAsync(
            object key,
            T element,
            int duration,
            TimeSpan? slidingExpiration = null) =>
            _serializer.SerializeObjectAsync(key)
                .ContinueWith(
                    task => Insert(
                        task.Result,
                        element,
                        duration,
                        slidingExpiration));

        /// <summary>
        ///     Insert Cache with expiration date
        ///     The cache will do the job with the provided function body
        /// </summary>
        /// <param name="key">Key object (only serializable objects)</param>
        /// <param name="valueGetter">Value objects (only serializable objects)</param>
        /// <param name="duration">Expiration date</param>
        public void InsertFunc(object key, Func<T> valueGetter, DateTime? duration = null) =>
            Cache.GetOrCreate(
                _serializer.SerializeObject(key),
                cacheEntry =>
                {
                    if (cacheEntry != null)
                    {
                        cacheEntry.AbsoluteExpiration =
                            duration ?? DateTime.UtcNow.AddMinutes(BaseConstants.DEFAULT_CACHE_TIME);
                    }

                    return valueGetter();
                });

        /// <summary>
        ///     Insert Cache with expiration date
        ///     The cache will do the job with the provided function body which can be a task also
        /// </summary>
        /// <param name="key">Key object (only serializable objects)</param>
        /// <param name="valueGetter">Function task to set expired object again</param>
        /// <param name="duration">Expiration date</param>
        /// <returns>Task on which the function operates</returns>
        public Task InsertFuncAsync(object key, Func<Task<T>> valueGetter, DateTime? duration = null) =>
            _serializer.SerializeObjectAsync(key)
                .ContinueWith(
                    async task =>
                    {
                        await Cache.GetOrCreate(
                            task.Result,
                            async cacheEntry =>
                            {
                                if (cacheEntry != null)
                                {
                                    cacheEntry.AbsoluteExpiration =
                                        duration ?? DateTime.UtcNow.AddMinutes(BaseConstants.DEFAULT_CACHE_TIME);
                                }

                                return await valueGetter();
                            });
                    })
                .Unwrap();

        #endregion

        #region Getters

        /// <summary>
        /// Gets value synchronously
        /// </summary>
        /// <param name="key">Key of the cache object</param>
        /// <returns><see cref="T"/> or default</returns>
        public T Get(object key) => Cache.Get<T>(_serializer.SerializeObject(key));

        /// <summary>
        /// Gets value asynchronously
        /// </summary>
        /// <param name="key">Key of the cache object</param>
        /// <returns><see cref="T"/> or default</returns>
        public Task<T> GetAsync(object key) =>
            _serializer.SerializeObjectAsync(key)
                .ContinueWith(
                    task => Cache.Get<T>(task.Result) ?? default(T));

        #endregion

        #region Helper Functions

        /// <inheritdoc/>
        public bool HasKey(object key) => Cache.TryGetValue(_serializer.SerializeObject(key), out _);

        /// <inheritdoc/>
        public bool HasKey(object key, out T result) => Cache.TryGetValue(_serializer.SerializeObject(key), out result);

        /// <inheritdoc/>
        public Task<bool> HasKeyAsync(object key) => Task.Run(() => HasKey(key));

        /// <summary>
        ///     Cost intensive operation uses reflection please use it with performance in mind
        /// </summary>
        /// <returns>Collection of cache entries</returns>
        [Obsolete("Reflection please be careful")]
        public ConcurrentDictionary<object, T> GetAll()
        {
            ConcurrentDictionary<object, T> ret = null;
            var entries = GetAllKeys();
            if (entries != null)
            {
                ret = new ConcurrentDictionary<object, T>();
                foreach (var key
                    in entries)
                {
                    if (HasKey(key, out var result))
                    {
                        ret.TryAdd(key, result);
                    }
                }
            }

            return ret;
        }

        /// <summary>
        ///     Cost intensive operation uses reflection please use it with performance in mind
        /// </summary>
        /// <returns>Value collection of cache entries</returns>
        [Obsolete("Reflection please be careful")]
        public List<T> GetAllValues()
        {
            List<T> ret = null;
            var entries = GetAllKeys();
            if (entries != null)
            {
                ret = new List<T>();
                foreach (var key
                    in entries)
                {
                    if (HasKey(key, out var result))
                    {
                        ret.Add(result);
                    }
                }
            }

            return ret;
        }

        /// <inheritdoc/>
        public bool Remove(object key)
        {
            if (HasKey(key, out _))
            {
                Cache.Remove(_serializer.SerializeObject(key));
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public Task<bool> RemoveAsync(object key)
        {
            return Task.Run(() => Remove(key));
        }

        /// <summary>
        ///     Cost intensive operation uses reflection please use it with performance in mind
        /// </summary>
        /// <returns>Key collection of cache entries</returns>
        [Obsolete("Reflection please be careful")]
        public ICollection GetAllKeys()
        {
            GetEntryInfo();
            var entries = EntriesInfo.GetValue(Cache) as IDictionary;
            return entries?.Keys;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetEntryInfo()
        {
            if (EntriesInfo == null)
            {
                EntriesInfo = typeof(MemoryCache).GetField(
                    "_entries",
                    BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance);
            }
        }

        /// <summary>
        /// Disposes memory cache
        /// </summary>
        public void Dispose() => Cache?.Dispose();

        #endregion

        #region Private

        /// <summary>
        ///     Gets memory cache entry options with
        ///     absolute and sliding expiration for a cache element.
        ///     Default for absolute expiration is 2 hours,
        ///     sliding expiration half of the absolute expiration.
        /// </summary>
        /// <param name="absoluteExpiration">Absolute expiration date</param>
        /// <param name="slidingExpiration">Sliding expiration time</param>
        /// <returns>Cache entry options</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static MemoryCacheEntryOptions GetMemoryCacheEntryOptions(
            DateTime? absoluteExpiration,
            TimeSpan? slidingExpiration = null)
        {
            absoluteExpiration ??= DateTime.UtcNow.AddMinutes(BaseConstants.DEFAULT_CACHE_TIME);
            TimeSpan slidingExpirationDate;
            if (slidingExpiration != null)
            {
                slidingExpirationDate = slidingExpiration.Value;
            }
            else
            {
                var timeUntilExpiration = absoluteExpiration.Value - DateTime.UtcNow;
                slidingExpirationDate = timeUntilExpiration.Ticks <= 0 ?
                    new TimeSpan(1) :
                    new TimeSpan(timeUntilExpiration.Ticks / 2);
            }

            return new MemoryCacheEntryOptions()
            {
                AbsoluteExpiration = absoluteExpiration,
                SlidingExpiration = slidingExpirationDate,
            };
        }

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

        #endregion
    }
}