// ------------------------------------------------------------------------------------------
// <copyright file="MoqCache.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using System;
using ACIM.Sales.Core.Helper.Cache.BaseInterface;

namespace ACIM.Sales.PriceListTest.MoqSetup.CacheMoq
{
    /// <summary>
    /// Moq object for Cache
    /// </summary>
    /// <typeparam name="T">Type to mock with</typeparam>
    public class MoqCache<T>
        : IDisposable 
        where T : IBaseCache<object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MoqCache{T}"/> class.
        /// </summary>
        public MoqCache()
        {
            CacheService = Activator.CreateInstance<T>();
        }

        /// <summary>
        /// Gets Cache object
        /// </summary>
        public T CacheService { get; }

        /// <summary>
        /// Disposes cache object
        /// </summary>
        public void Dispose()
        {
            if (CacheService != null)
            {
                CacheService.Dispose();
            }
        }
    }
}