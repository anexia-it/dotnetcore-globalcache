using Anexia.Caching.GlobalCache.Caches.TypeBased;
using Anexia.Caching.GlobalCacheTests.CacheMoq;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Anexia.Caching.GlobalCacheTests.Init
{
    [ExcludeFromCodeCoverage]
    [CollectionDefinition("UniCache")]
    public class MoqXUnitInjectionsCache
        : ICollectionFixture<MoqCache<UniversalCache>>
    {
    }
}