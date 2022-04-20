// ------------------------------------------------------------------------------------------
// <copyright file="RedisCacheTest.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anexia.Caching.GlobalCache.Abstraction.BaseCache;
using Anexia.Caching.GlobalCache.Config.Model;
using Xunit;

namespace Anexia.Caching.GlobalCacheTests.Abstraction.BaseAbstraction
{
    /// <summary>
    /// Test class for Redis caches
    /// </summary>
    public class RedisCacheTest
    {
        private readonly BaseCache<TestClass> _baseCacheWithTypeKey;
        private readonly BaseCache<TestClass> _baseCacheWithoutTypeKey;
        private readonly BaseCache<TestClass> _baseCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheTest"/> class.
        /// </summary>
        public RedisCacheTest()
        {
            var config = new RedisConfigModel()
            {
                Configuration = "localhost",
                InstanceName = "RedisCacheTest",
            };

            _baseCache = new BaseCache<TestClass>(config, typeKey: "baseCache");
            _baseCacheWithTypeKey = new BaseCache<TestClass>(config, typeKey: "TestTypeKey");
            _baseCacheWithoutTypeKey = new BaseCache<TestClass>(config);
        }

        /// <summary>
        /// Get all keys from redis cache, should return two keys
        /// </summary>
        [Fact]
        public void GetAllKeysFromCacheShouldReturnTwoKeys()
        {
            _baseCacheWithTypeKey.Insert("Key01", new TestClass());
            _baseCacheWithTypeKey.Insert("Key02", new TestClass());
            var allKeys = _baseCacheWithTypeKey.GetAllKeys();
            Assert.True(allKeys.Count == 2);
        }

        /// <summary>
        /// Get all keys from redis cache initialized with type key, should return keys
        /// without type key or instance name
        /// </summary>
        [Fact]
        public void GetAllKeysFromCacheWithTypeKeyShouldReturnWithoutInstanceAndTypeKeys()
        {
            _baseCacheWithTypeKey.Insert("Key01", new TestClass());
            var allKeys = _baseCacheWithTypeKey.GetAllKeys();
            foreach (var key in allKeys)
            {
                Assert.False(key.ToString().StartsWith("TestTypeKey"));
                Assert.False(key.ToString().Contains(":"));
                Assert.True(key.ToString().StartsWith("Key"));
            }

            Assert.NotNull(_baseCacheWithTypeKey.Get("Key01"));
        }

        /// <summary>
        /// Get all keys from redis cache initialized without type key, should return keys
        /// without type key or instance name
        /// </summary>
        [Fact]
        public void GetAllKeysFromCacheWithoutTypeKeyShouldReturnWithoutInstance()
        {
            _baseCacheWithoutTypeKey.Insert("Key01", new TestClass());
            var allKeys = _baseCacheWithoutTypeKey.GetAllKeys();
            foreach (var key in allKeys)
            {
                Assert.False(key.ToString().Contains(":"));
                Assert.True(key.ToString().StartsWith("Key"));
            }

            Assert.NotNull(_baseCacheWithoutTypeKey.Get("Key01"));
        }

        [Fact]
        public void RemoveFromRedisTest()
        {
            var differentTestClass = new TestClass() { TestPropertyName = "Different" };
            _baseCache.Insert(new TestClass(), new TestClass());
            _baseCache.Insert(differentTestClass, new TestClass());
            _baseCache.Remove(new TestClass());
            Assert.Null(_baseCache.Get(new TestClass()));
            Assert.NotNull(_baseCache.Get(differentTestClass));
        }

        /// <summary>
        ///     Item should expire through sliding expiration
        /// </summary>
        [Fact]
        public void SetSlidingExpirationLowerThanAbsoluteShouldRemoveCacheItem()
        {
            _baseCacheWithoutTypeKey.Insert(
                "Key02",
                new TestClass(),
                DateTime.UtcNow.AddSeconds(10),
                new TimeSpan(0, 0, 5));
            Assert.NotNull(_baseCacheWithoutTypeKey.Get("Key02"));
            Task.Delay(5000).Wait();
            Assert.Null(_baseCacheWithoutTypeKey.Get("Key02"));
        }

        /// <summary>
        ///     Item should be accessible before sliding expiration
        /// </summary>
        [Fact]
        public void GetItemBeforeSlidingExpirationShouldReturnCacheItem()
        {
            _baseCacheWithoutTypeKey.Insert(
                "Key03",
                new TestClass(),
                DateTime.UtcNow.AddSeconds(10),
                new TimeSpan(0, 0, 2));
            Assert.NotNull(_baseCacheWithoutTypeKey.Get("Key03"));
            Task.Delay(1000).Wait();
            Assert.NotNull(_baseCacheWithoutTypeKey.Get("Key03"));
        }

        /// <summary>
        ///     Item should expire through absolut expiration since accessing the
        ///     item multiple times should reset sliding expiration
        /// </summary>
        [Fact]
        public void GetItemBeforeSlidingExpirationWaitUntilAbsoluteExpirationShouldRemoveCacheItem()
        {
            _baseCacheWithoutTypeKey.Insert(
                "Key04",
                new TestClass(),
                DateTime.UtcNow.AddSeconds(5),
                new TimeSpan(0, 0, 3));
            Assert.NotNull(_baseCacheWithoutTypeKey.Get("Key04"));
            Task.Delay(2000).Wait();
            Assert.NotNull(_baseCacheWithoutTypeKey.Get("Key04"));
            Task.Delay(1000).Wait();
            Assert.NotNull(_baseCacheWithoutTypeKey.Get("Key04"));
            Task.Delay(2000).Wait();
            Assert.Null(_baseCacheWithoutTypeKey.Get("Key04"));
        }

        /// <summary>
        ///     Item should expire through sliding expiration from asynchronous insert
        /// </summary>
        [Fact]
        public void SetSlidingExpirationLowerThanAbsoluteShouldRemoveCacheItemAsync()
        {
            _baseCacheWithoutTypeKey.InsertAsync(
                "Key05",
                new TestClass(),
                DateTime.UtcNow.AddSeconds(10),
                new TimeSpan(0, 0, 3));
            Task.Delay(1000).Wait();
            Assert.NotNull(_baseCacheWithoutTypeKey.Get("Key05"));
            Task.Delay(3000).Wait();
            Assert.Null(_baseCacheWithoutTypeKey.Get("Key05"));
        }

        /// <summary>
        ///     Item should be accessible before sliding expiration from asynchronous insert
        /// </summary>
        [Fact]
        public void GetItemBeforeSlidingExpirationShouldReturnCacheItemAsync()
        {
            _baseCacheWithoutTypeKey.InsertAsync(
                "Key06",
                new TestClass(),
                DateTime.UtcNow.AddSeconds(3),
                new TimeSpan(0, 0, 3));
            Task.Delay(1000).Wait();
            Assert.NotNull(_baseCacheWithoutTypeKey.Get("Key06"));
            Task.Delay(1000).Wait();
            Assert.NotNull(_baseCacheWithoutTypeKey.Get("Key06"));
        }

        /// <summary>
        ///     Item should expire through absolut expiration since accessing the
        ///     item multiple times should reset sliding expiration from asynchronous insert
        /// </summary>
        [Fact]
        public void GetItemBeforeSlidingExpirationWaitUntilAbsoluteExpirationShouldRemoveCacheItemAsync()
        {
            _baseCacheWithoutTypeKey.InsertAsync(
                "Key07",
                new TestClass(),
                DateTime.UtcNow.AddSeconds(6),
                new TimeSpan(0, 0, 4));
            Task.Delay(2000).Wait();
            Assert.NotNull(_baseCacheWithoutTypeKey.Get("Key07"));
            Task.Delay(2000).Wait();
            Assert.NotNull(_baseCacheWithoutTypeKey.Get("Key07"));
            Task.Delay(1000).Wait();
            Assert.NotNull(_baseCacheWithoutTypeKey.Get("Key07"));
            Task.Delay(2000).Wait();
            Assert.Null(_baseCacheWithoutTypeKey.Get("Key07"));
        }

        /// <summary>
        ///     Item should expire half time of absolute expiration since default of sliding
        ///     expiration will be set to this duration
        /// </summary>
        [Fact]
        public void SetItemWithoutSlidingExpirationShouldExpireAfterHalfOfAbsoluteExpiration()
        {
            _baseCacheWithoutTypeKey.Insert(
                "Key08",
                new TestClass(),
                DateTime.UtcNow.AddSeconds(6));
            Assert.NotNull(_baseCacheWithoutTypeKey.Get("Key08"));
            Task.Delay(3000).Wait();
            Assert.Null(_baseCacheWithoutTypeKey.Get("Key08"));
        }

        /// <summary>
        ///     Item's key should be found because existent and item returned
        /// </summary>
        [Fact]
        public void InsertAndCheckIfHasKeyShouldReturnTrueAndValue()
        {
            var testId = Guid.NewGuid();
            _baseCacheWithoutTypeKey.Insert("Key09", new TestClass { TestPropertyGuid = testId });

            var hasKey = _baseCacheWithoutTypeKey.HasKey("Key09", out var testClass);

            Assert.True(hasKey);
            Assert.True(testClass != null);
            Assert.True(testClass.TestPropertyGuid == testId);
        }

        /// <summary>
        ///     Item's key should be found because existent
        /// </summary>
        [Fact]
        public void InsertAndCheckIfHasKeyShouldReturnTrue()
        {
            var testId = Guid.NewGuid();
            _baseCacheWithoutTypeKey.Insert("Key10", new TestClass { TestPropertyGuid = testId });

            var hasKey = _baseCacheWithoutTypeKey.HasKey("Key10");

            Assert.True(hasKey);
        }

        /// <summary>
        ///     Item's key should be found because existent
        /// </summary>
        [Fact]
        public void InsertAndCheckIfHasKeyAsyncShouldReturnTrue()
        {
            var testId = Guid.NewGuid();
            _baseCacheWithoutTypeKey.Insert("Key10", new TestClass { TestPropertyGuid = testId });

            var hasKey = _baseCacheWithoutTypeKey.HasKeyAsync("Key10");

            Assert.True(hasKey.Result);
        }

        /// <summary>
        ///     Item's key should not be found because not existent and item returned is null
        /// </summary>
        [Fact]
        public void CheckIfHasKeyShouldReturnFalseAndNull()
        {
            var hasKey = _baseCacheWithoutTypeKey.HasKey("Key999", out var testClass);

            Assert.True(!hasKey);
            Assert.True(testClass == null);
        }

        /// <summary>
        ///     Item's key should not be found because not existent
        /// </summary>
        [Fact]
        public void CheckIfHasKeyShouldReturnFalse()
        {
            var hasKey = _baseCacheWithoutTypeKey.HasKey("Key999");

            Assert.True(!hasKey);
        }

        /// <summary>
        ///     Item's key should not be found because not existent
        /// </summary>
        /// <returns>Task to work on</returns>
        [Fact]
        public async Task CheckIfHasKeyAsyncShouldReturnFalse()
        {
            var hasKey = await _baseCacheWithoutTypeKey.HasKeyAsync("Key999");

            Assert.True(!hasKey);
        }

        /// <summary>
        ///     Item's key should not be found because not existent
        /// </summary>
        [Fact]
        public void GetLargeChunkOfValues()
        {
            var counter = 10000;
            var allObjectsSaved = new Dictionary<int, TestClass>();
            while (counter >= 0)
            {
                var differentTestClass = new TestClass()
                {
                    TestPropertyName = "Different",
                    TestPropertyNumber = counter,
                };
                _baseCacheWithoutTypeKey.Insert($"KeyBase{counter}", differentTestClass);
                allObjectsSaved.Add(counter, differentTestClass);
                counter--;
            }
            var listOfData = _baseCacheWithoutTypeKey.GetAllValues();
            foreach (var dataRetrieved in listOfData)
            {
                allObjectsSaved.Remove(dataRetrieved.TestPropertyNumber);
            }

            Assert.NotNull(listOfData);
            Assert.Empty(allObjectsSaved);
        }
    }
}