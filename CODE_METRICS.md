# Anexia Caching
Package with wrapper arround MemoryCache with specified functions to get/set,
cache objects. 

## Installation and configuration
Install the package via NuGet: "Anexia.Caching.GlobalCache"

	public class CacheApp : BaseCache<AppModelToCache>
	{
		//for MemoryCache
		public CacheApp()
			: base(sizeLimit: null, serializer: UTF8JsonSerializer.Instance)
		{}
		
		//for RedisCache
		public CacheApp(RedisConfigModel config)
			: base(config, typeKey: nameof(AppModelToCache))
		{}
	}
	
	protected void Application_Start()
	{
		IBaseCache<AppModelToCache> bsCache = new CacheApp();
		bsCache.Insert(
			"key as object",
			"value as object",
			DateTime.UtcNow.AddMinutes(15));
	}

## Usage
It's a threadsafe implementation for Caching with .Net Standard 2.1.
Used in every possible application needed.

## Recommended
If you want to use RedisCache, test the Text.Json serializer with your model that you want to cache.  
If Text.Json delivers error messages you have to create your own serilizer with the IBaseSerializer.

## Testing
If you want to run the Redis cache tests, you have to have a Redis cache running on localhost

## List of developers

* Alex Peruzzi <APeruzzi@anexia-it>
* Joachim Eckerl <JEckerl@anexia-it.com>
* Andreas Aigner <AAigner@anexia-it.com>