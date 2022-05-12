// ------------------------------------------------------------------------------------------
// <copyright file="BaseConstants.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using System;

namespace Anexia.Caching.GlobalCache.Constants
{
    public static class BaseConstants
    {
        public static int DEFAULT_CACHE_TIME = 120;

        /// <summary>
        ///     Default lock expiration of 2 minutes
        /// </summary>
        public static TimeSpan DEFAULT_LOCK_TIME = new TimeSpan(0, 2, 0);
    }
}