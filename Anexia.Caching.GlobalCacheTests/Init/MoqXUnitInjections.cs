// ------------------------------------------------------------------------------------------
// <copyright file="MoqXUnitInjections.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using Anexia.Caching.GlobalCache.Caches.TypeBased;
using Anexia.Caching.GlobalCacheTests.CacheMoq;
using Xunit;

namespace Anexia.Caching.GlobalCacheTests.Init
{
    [CollectionDefinition("UniCache")]
    public class MoqXUnitInjectionsCache
        : ICollectionFixture<MoqCache<UniversalCache>>
    {
    }
}