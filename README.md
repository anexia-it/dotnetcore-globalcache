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
![Code Coverage](https://img.shields.io/badge/Code%20Coverage-25%25-yellow?style=flat)

Package | Line Rate | Branch Rate | Health
-------- | --------- | ----------- | ------
Anexia.Caching.GlobalCache | 25% | 14% | ➖
**Summary** | **25%** (150 / 591) | **14%** (42 / 296) | ➖

_Minimum allowed line rate is `20%`_
