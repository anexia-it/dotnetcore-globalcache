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
It's a threadsafe implementation for Caching with .NET 8.0.
Used in every possible application needed.

## Technical Details

**Target Framework:** .NET 8.0 (LTS)

**Main Dependencies:**
- Humanizer 2.14.1
- Microsoft.Extensions.Caching.Memory 8.0.11
- Microsoft.Extensions.Caching.StackExchangeRedis 8.0.11
- Microsoft.Extensions.Configuration 8.0.0
- Microsoft.Extensions.Configuration.Abstractions 8.0.0
- Microsoft.Extensions.Configuration.Json 8.0.1
- Newtonsoft.Json 13.0.4
- Newtonsoft.Json.Bson 1.0.3
- StackExchange.Redis 2.9.25
- System.Text.Json 8.0.5

## Recommended
If you want to use RedisCache, test the Text.Json serializer with your model that you want to cache.  
If Text.Json delivers error messages you have to create your own serilizer with the IBaseSerializer.

## Code Coverage and Readme Process
If you want to modify Readme-File, always mod README_BASE.md.
README.md will be overwritten after pull request to master-branch. 

## Testing
If you want to run the Redis cache tests, you have to have a Redis cache running on localhost

## List of developers

* Alex Peruzzi <APeruzzi@anexia-it>
* Joachim Eckerl <JEckerl@anexia-it.com>
* Andreas Aigner <AAigner@anexia-it.com>

## Code Coverage
