// ------------------------------------------------------------------------------------------
// <copyright file="UniversalCache.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using Anexia.Caching.GlobalCache.Caches.CacheTypes;
using System;
using System.Threading.Tasks;

namespace Anexia.Caching.GlobalCache.Caches.TypeBased
{
    /// <summary>
    /// Universal cache with Memory Cache
    /// </summary>
    public class UniversalCache : MemoryCacheLayer<object>
    {
        /// <summary>
        ///     Insert Cache with expiration date
        /// </summary>
        /// <typeparam name="T">Type on which the name gets created</typeparam>
        /// <param name="element">Value objects (only serializable objects)</param>
        /// <param name="duration">Absolute expiration date</param>
        /// <param name="slidingExpiration">Sliding expiration time span</param>
        public void Insert<T>(T element, DateTime? duration = null, TimeSpan? slidingExpiration = null) =>
            Insert(typeof(T).FullName, element, duration, slidingExpiration);

        /// <summary>
        ///     Insert Cache with expiration minutes
        /// </summary>
        /// <typeparam name="T">Type on which the name gets created</typeparam>
        /// <param name="element">Value objects (only serializable objects)</param>
        /// <param name="duration">Expiration minutes</param>
        /// <param name="slidingExpiration">Sliding expiration time span</param>
        public void Insert<T>(T element, int duration, TimeSpan? slidingExpiration = null) =>
            Insert(typeof(T).FullName, element, duration, slidingExpiration);

        /// <summary>
        ///     Insert Cache with expiration minutes
        /// </summary>
        /// <typeparam name="T">Type on which the name gets created</typeparam>
        /// <param name="element">Value objects (only serializable objects)</param>
        /// <param name="duration">Expiration minutes</param>
        /// <param name="slidingExpiration">Sliding expiration time span</param>
        /// <returns>Task on which the function operates</returns>
        public Task InsertAsync<T>(T element, int duration, TimeSpan? slidingExpiration = null) =>
            Task.Factory.StartNew(() => Insert(element, duration, slidingExpiration));

        /// <summary>
        ///     Insert Cache with expiration date
        /// </summary>
        /// <typeparam name="T">Type on which the name gets created</typeparam>
        /// <param name="element">Value objects (only serializable objects)</param>
        /// <param name="duration">Expiration date</param>
        /// <param name="slidingExpiration">Sliding expiration time span</param>
        /// <returns>Task on which the function operates</returns>
        public Task InsertAsync<T>(T element, DateTime? duration = null, TimeSpan? slidingExpiration = null) =>
            Task.Factory.StartNew(() => Insert(element, duration, slidingExpiration));

        /// <summary>
        ///     Insert Cache with expiration date and function which gets called when expiration occurs
        /// </summary>
        /// <typeparam name="T">Type on which the name gets created</typeparam>
        /// <param name="key">Key as prefix</param>
        /// <param name="valueGetter">Function to reset expired object</param>
        /// <param name="duration">Expiration date</param>
        public void InsertFunc<T>(object key, Func<object> valueGetter, DateTime? duration = null) =>
            base.InsertFunc(typeof(T).FullName, valueGetter, duration);

        /// <summary>
        ///     Insert Cache with expiration date and function which gets called when expiration occurs
        /// </summary>
        /// <typeparam name="T">Type on which the name gets created</typeparam>
        /// <param name="key">Key as prefix</param>
        /// <param name="valueGetter">Function to reset expired object</param>
        /// <param name="duration">Expiration date</param>
        /// <returns>Task on which the function operates</returns>
        public Task InsertFuncAsync<T>(object key, Func<Task<object>> valueGetter, DateTime? duration = null) =>
            base.InsertFuncAsync(typeof(T).FullName, valueGetter, duration);

        /// <summary>
        /// Gets the object from Cache
        /// </summary>
        /// <typeparam name="T">Type of the cache object</typeparam>
        /// <returns>Object from cache</returns>
        public T Get<T>() => (T)(base.Get(typeof(T).FullName) ?? default(T));

        /// <summary>
        /// Gets the object from Cache
        /// </summary>
        /// <typeparam name="T">Type of the cache object</typeparam>
        /// <returns>Task with Object from cache</returns>
        public async Task<T> GetAsync<T>() => (T)(await base.GetAsync(typeof(T).FullName) ?? default(T));
    }
}