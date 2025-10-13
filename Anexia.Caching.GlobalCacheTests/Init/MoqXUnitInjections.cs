using Anexia.Caching.GlobalCache.Caches.TypeBased;
using Anexia.Caching.GlobalCacheTests.CacheMoq;
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