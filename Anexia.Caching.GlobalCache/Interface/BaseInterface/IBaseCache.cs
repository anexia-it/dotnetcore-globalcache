// ------------------------------------------------------------------------------------------
// <copyright file="IBaseCache.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anexia.Caching.GlobalCache.Interface.BaseInterface
{
    /// <summary>
    /// Base cache interface for standardizing calls
    /// </summary>
    /// <typeparam name="T">Type of the return value</typeparam>
    public interface IBaseCache<T> : IDisposable
    {
        /// <summary>
        /// Inserts into cache
        /// </summary>
        /// <param name="key">Key of cache</param>
        /// <param name="element">Object of cache</param>
        /// <param name="duration">Absolute expiration date</param>
        /// <param name="slidingExpiration">Sliding expiration time span</param>
        public void Insert(
            object key,
            T element,
            DateTime? duration = null,
            TimeSpan? slidingExpiration = null);

        /// <summary>
        /// Inserts into cache
        /// </summary>
        /// <param name="key">Key of cache</param>
        /// <param name="element">Object of cache</param>
        /// <param name="duration">Absolute expiration minutes</param>
        /// <param name="slidingExpiration">Sliding expiration time span</param>
        public void Insert(
            object key,
            T element,
            int duration,
            TimeSpan? slidingExpiration = null);

        /// <summary>
        /// Inserts into cache
        /// </summary>
        /// <param name="key">Key of cache</param>
        /// <param name="element">Object of cache</param>
        /// <param name="duration">Absolute expiration date</param>
        /// <param name="slidingExpiration">Sliding expiration time span</param>
        /// <returns>Task on which the function operates</returns>
        public Task InsertAsync(
            object key,
            T element,
            DateTime? duration = null,
            TimeSpan? slidingExpiration = null);

        /// <summary>
        /// Inserts into cache
        /// </summary>
        /// <param name="key">Key of cache</param>
        /// <param name="element">Object of cache</param>
        /// <param name="duration">Absolute expiration minutes</param>
        /// <param name="slidingExpiration">Sliding expiration time span</param>
        /// <returns>Task on which the function operates</returns>
        public Task InsertAsync(
            object key,
            T element,
            int duration,
            TimeSpan? slidingExpiration = null);

        /// <summary>
        /// Gets object from Cache
        /// </summary>
        /// <param name="key">Object key for Cache</param>
        /// <returns>Object or default</returns>
        public T Get(object key);

        /// <summary>
        /// Gets object from Cache
        /// </summary>
        /// <param name="key">Object key for Cache</param>
        /// <returns>Task with object or default</returns>
        public Task<T> GetAsync(object key);

        /// <summary>
        ///     Checks if key exists
        /// </summary>
        /// <param name="key">Key of the object</param>
        /// <returns>Bool whether the key exists or not</returns>
        public bool HasKey(object key);

        /// <summary>
        ///     Checks if key exists and returns its value if existent
        /// </summary>
        /// <param name="key">Key of the object</param>
        /// <param name="result">Object if found</param>
        /// <returns>Bool whether the key exists or not</returns>
        public bool HasKey(object key, out T result);

        /// <summary>
        ///     Checks if key exists
        /// </summary>
        /// <param name="key">Key of the object</param>
        /// <returns>Task containing bool whether the key exists or not</returns>
        public Task<bool> HasKeyAsync(object key);

        /// <summary>
        /// Gets all key from cache
        /// </summary>
        /// <returns>ICollection of keys or default</returns>
        public ICollection GetAllKeys();

        /// <summary>
        /// Gets all values from cache
        /// </summary>
        /// <returns>List of values</returns>
        public List<T> GetAllValues();

        /// <summary>
        /// Removes object from Cache
        /// </summary>
        /// <param name="key">Object key for Cache</param>
        /// <returns>True if key existed and got deleted false if either of them is false</returns>
        public bool Remove(object key);

        /// <summary>
        /// Removes object from Cache
        /// </summary>
        /// <param name="key">Object key for Cache</param>
        /// <returns>Task of True if key existed and got deleted false if either of them is false</returns>
        public Task<bool> RemoveAsync(object key);

        /// <summary>
        ///     Acquires the lock
        /// </summary>
        /// <returns><c>true</c>, if lock was acquired, <c>false</c> otherwise.</returns>
        /// <param name="key">Lock key</param>
        /// <param name="expiration">Absolute expiration</param>
        public bool AcquireLock(object key, TimeSpan expiration = default);

        /// <summary>
        ///     Releases the lock
        /// </summary>
        /// <returns><c>true</c>, if lock was released, <c>false</c> otherwise.</returns>
        /// <param name="key">Lock key</param>
        public bool ReleaseLock(object key);
    }
}