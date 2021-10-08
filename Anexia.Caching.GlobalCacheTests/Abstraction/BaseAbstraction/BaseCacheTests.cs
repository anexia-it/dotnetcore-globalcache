// ------------------------------------------------------------------------------------------
// <copyright file="BaseCacheTests.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Anexia.Caching.GlobalCache.Abstraction.BaseAbstraction;
using Anexia.Caching.GlobalCache.Abstraction.BaseCache;
using Anexia.Caching.GlobalCache.Interface.BaseInterface;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Xunit;
using Xunit.Abstractions;
using JsonSerializer = Utf8Json.JsonSerializer;

namespace ACIM.Sales.Core.Helper.Cache.Base.Tests
{
    /// <summary>
    /// Base caching test
    /// </summary>
    public class BaseCacheTests
    {
        private readonly ITestOutputHelper output;
        private readonly BaseCache<TestClass> _baseCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCacheTests"/> class.
        /// </summary>
        /// <param name="_output">Console log output helper</param>
        public BaseCacheTests(ITestOutputHelper _output)
        {
            output = _output;
            _baseCache = new BaseCache<TestClass>(sizeLimit: null);
        }

        /// <summary>
        /// Test for getting object out of cache
        /// </summary>
        [Fact]
        public void BaseCacheRetrieveTest()
        {
            var baseCache = new BaseCache<object>(sizeLimit: null);
            baseCache.Insert(new TestClass(), new TestClass());
            Assert.NotNull(baseCache.Get(new TestClass()));
        }

        /// <summary>
        /// Test for removing object out of cache
        /// </summary>
        [Fact]
        public void BaseCacheRemoveTest()
        {
            var baseCache = new BaseCache<object>(sizeLimit: null);
            baseCache.Insert(new TestClass(), new TestClass());
            baseCache.Remove(new TestClass());
            Assert.Null(baseCache.Get(new TestClass()));
        }

        /// <summary>
        /// Difference test between serialization techniques
        /// </summary>
        [Fact]
        public void BaseCacheStressDifferentSerializerTest()
        {
            foreach (var ser
                in new IBaseSerializer[]
                {
                    new TestJsonNewtonSerializer(), new BaseTextJsonSerializer(),
                    new TestBsonNewtonsoftSerializer(), new TestUtf8Serializer()
                })
            {
                var stp = Stopwatch.StartNew();
                for (var i = 0; i < 500; i++)
                {
                    var baseCache = new BaseCache<object>(null, ser);
                    baseCache.Insert(new TestClass(), new TestClass());
                    Assert.NotNull(baseCache.Get(new TestClass()));
                }

                stp.Stop();
                output.WriteLine($"{ser.GetType().Name}: {stp.Elapsed.TotalSeconds}");
            }
        }

        /// <summary>
        ///     Item should expire through sliding expiration
        /// </summary>
        [Fact]
        public void SetSlidingExpirationLowerThanAbsoluteShouldRemoveCacheItem()
        {
            _baseCache.Insert("Key01", new TestClass(), DateTime.UtcNow.AddSeconds(10), new TimeSpan(0, 0, 2));
            Assert.NotNull(_baseCache.Get("Key01"));
            Thread.Sleep(3000);
            Assert.Null(_baseCache.Get("Key01"));
        }

        /// <summary>
        ///     Item should be accessable before sliding expiration
        /// </summary>
        [Fact]
        public void GetItemBeforeSlidingExpirationShouldReturnCacheItem()
        {
            _baseCache.Insert("Key01", new TestClass(), DateTime.UtcNow.AddSeconds(10), new TimeSpan(0, 0, 2));
            Assert.NotNull(_baseCache.Get("Key01"));
            Thread.Sleep(1000);
            Assert.NotNull(_baseCache.Get("Key01"));
        }

        /// <summary>
        ///     Item should expire through absolut expiration since accessing the
        ///     item multiple times should reset sliding expiration
        /// </summary>
        [Fact]
        public void GetItemBeforeSlidingExpirationWaitUntilAbsoluteExpirationShouldRemoveCacheItem()
        {
            _baseCache.Insert("Key01", new TestClass(), DateTime.UtcNow.AddSeconds(5), new TimeSpan(0, 0, 3));
            Assert.NotNull(_baseCache.Get("Key01"));
            Thread.Sleep(2000);
            Assert.NotNull(_baseCache.Get("Key01"));
            Thread.Sleep(1000);
            Assert.NotNull(_baseCache.Get("Key01"));
            Thread.Sleep(2000);
            Assert.Null(_baseCache.Get("Key01"));
        }

        /// <summary>
        ///     Item should expire through sliding expiration from asynchronous insert
        /// </summary>
        [Fact]
        public void SetSlidingExpirationLowerThanAbsoluteShouldRemoveCacheItemAsync()
        {
            _baseCache.InsertAsync("Key01", new TestClass(), DateTime.UtcNow.AddSeconds(10), new TimeSpan(0, 0, 3));
            Thread.Sleep(1000);
            Assert.NotNull(_baseCache.Get("Key01"));
            Thread.Sleep(3500);
            Assert.Null(_baseCache.Get("Key01"));
        }

        /// <summary>
        ///     Item should be accessable before sliding expiration from asynchronous insert
        /// </summary>
        [Fact]
        public void GetItemBeforeSlidingExpirationShouldReturnCacheItemAsync()
        {
            _baseCache.InsertAsync("Key01", new TestClass(), DateTime.UtcNow.AddSeconds(3), new TimeSpan(0, 0, 3));
            Thread.Sleep(1000);
            Assert.NotNull(_baseCache.Get("Key01"));
            Thread.Sleep(1000);
            Assert.NotNull(_baseCache.Get("Key01"));
        }

        /// <summary>
        ///     Item should expire through absolut expiration since accessing the
        ///     item multiple times should reset sliding expiration from asynchronous insert
        /// </summary>
        [Fact]
        public void GetItemBeforeSlidingExpirationWaitUntilAbsoluteExpirationShouldRemoveCacheItemAsync()
        {
            _baseCache.InsertAsync("Key01", new TestClass(), DateTime.UtcNow.AddSeconds(5), new TimeSpan(0, 0, 3));
            Thread.Sleep(1000);
            Assert.NotNull(_baseCache.Get("Key01"));
            Thread.Sleep(2000);
            Assert.NotNull(_baseCache.Get("Key01"));
            Thread.Sleep(1000);
            Assert.NotNull(_baseCache.Get("Key01"));
            Thread.Sleep(2000);
            Assert.Null(_baseCache.Get("Key01"));
        }

        /// <summary>
        ///     Item should expire half time of absolute expiration since default of sliding
        ///     expiration will be set to this duration
        /// </summary>
        [Fact]
        public void SetItemWithoutSlidingExpirationShouldExpireAfterHalfOfAbsoluteExpiration()
        {
            _baseCache.Insert("Key01", new TestClass(), DateTime.UtcNow.AddSeconds(6));
            Assert.NotNull(_baseCache.Get("Key01"));
            Thread.Sleep(3000);
            Assert.Null(_baseCache.Get("Key01"));
        }

        /// <summary>
        ///     Item's key should be found because existent and item returned
        /// </summary>
        [Fact]
        public void InsertAndCheckIfHasKeyShouldReturnTrueAndValue()
        {
            var testId = Guid.NewGuid();
            _baseCache.Insert("Key09", new TestClass { TestPropertyGuid = testId });

            var hasKey = _baseCache.HasKey("Key09", out var testClass);

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
            _baseCache.Insert("Key10", new TestClass { TestPropertyGuid = testId });

            var hasKey = _baseCache.HasKey("Key10");

            Assert.True(hasKey);
        }

        /// <summary>
        ///     Item's key should be found because existent
        /// </summary>
        [Fact]
        public void InsertAndCheckIfHasKeyAsyncShouldReturnTrue()
        {
            var testId = Guid.NewGuid();
            _baseCache.Insert("Key10", new TestClass { TestPropertyGuid = testId });

            var hasKey = _baseCache.HasKeyAsync("Key10");

            Assert.True(hasKey.Result);
        }

        /// <summary>
        ///     Item's key should not be found because not existent and item returned is null
        /// </summary>
        [Fact]
        public void CheckIfHasKeyShouldReturnFalseAndNull()
        {
            var hasKey = _baseCache.HasKey("Key999", out var testClass);

            Assert.True(!hasKey);
            Assert.True(testClass == null);
        }

        /// <summary>
        ///     Item's key should not be found because not existent
        /// </summary>
        [Fact]
        public void CheckIfHasKeyShouldReturnFalse()
        {
            var hasKey = _baseCache.HasKey("Key999");

            Assert.True(!hasKey);
        }

        /// <summary>
        ///     Item's key should not be found because not existent
        /// </summary>
        [Fact]
        public void CheckIfHasKeyAsyncShouldReturnFalse()
        {
            var hasKey = _baseCache.HasKeyAsync("Key999");

            Assert.True(!hasKey.Result);
        }
    }

    /// <summary>
    /// Test Object
    /// </summary>
    public class TestClass
    {
        /// <summary>
        /// Test
        /// </summary>
        public int TestPropertyNumber { get; set; } = 48515;

        /// <summary>
        /// Test
        /// </summary>
        public string TestPropertyName { get; set; } = "will be string";

        /// <summary>
        /// Test
        /// </summary>
        public DateTime TestPropertyDate { get; set; } = DateTime.Today;

        /// <summary>
        /// Test
        /// </summary>
        public Guid TestPropertyGuid { get; set; } = new Guid("434906C8-D8EB-446F-B329-0005E6229EA0");

        /// <summary>
        /// Test
        /// </summary>
        public decimal TestPropertyDecimal { get; set; } = 2.0m;
    }

    /// <summary>
    /// Test serialization with newtonsoft
    /// </summary>
    public class TestJsonNewtonSerializer : IBaseSerializer
    {
        /// <inheritdoc/>
        public Func<object, object> SerializeObject { get; } = obj =>
        {
            return JsonConvert.SerializeObject(obj);
        };

        /// <inheritdoc/>
        public Func<object, string> SerializeToString { get; }

        /// <inheritdoc/>
        public Func<object, byte[]> SerializeToByte { get; }

        /// <inheritdoc/>
        public Func<object, Task<object>> SerializeObjectAsync { get; } = obj =>
        {
            return Task.FromResult<object>(JsonConvert.SerializeObject(obj));
        };

        /// <inheritdoc/>
        public Func<object, Task<string>> SerializeToStringAsync { get; }

        /// <inheritdoc/>
        public Func<object, Task<byte[]>> SerializeToByteAsync { get; }

        /// <inheritdoc/>
        public T DeserializeObject<T>(object obj) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<T> DeserializeObjectAsync<T>(object obj) => throw new NotImplementedException();
    }

    /// <summary>
    /// Test serialization with Bson newtonsoft
    /// </summary>
    public class TestBsonNewtonsoftSerializer : IBaseSerializer
    {
        /// <inheritdoc/>
        public Func<object, string> SerializeToString => throw new NotImplementedException();

        /// <inheritdoc/>
        public Func<object, byte[]> SerializeToByte => throw new NotImplementedException();

        /// <inheritdoc/>
        public Func<object, Task<string>> SerializeToStringAsync => throw new NotImplementedException();

        /// <inheritdoc/>
        public Func<object, Task<byte[]>> SerializeToByteAsync => throw new NotImplementedException();

        /// <inheritdoc/>
        public T DeserializeObject<T>(object obj) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<T> DeserializeObjectAsync<T>(object obj) => throw new NotImplementedException();

        /// <inheritdoc/>
        Func<object, object> IBaseSerializer.SerializeObject { get; } = obj =>
        {
            var ms = new MemoryStream();
            using (var writer = new BsonWriter(ms))
            {
                var serializer = new Newtonsoft.Json.JsonSerializer();
                serializer.Serialize(writer, obj);
            }

            return Convert.ToBase64String(ms.ToArray());
        };

        /// <inheritdoc/>
        Func<object, Task<object>> IBaseSerializer.SerializeObjectAsync { get; } = obj =>
        {
            var ms = new MemoryStream();
            using (var writer = new BsonWriter(ms))
            {
                var serializer = new Newtonsoft.Json.JsonSerializer();
                serializer.Serialize(writer, obj);
            }

            return Task.FromResult<object>(Convert.ToBase64String(ms.ToArray()));
        };
    }

    /// <summary>
    /// Test serialization with UTF8
    /// </summary>
    public class TestUtf8Serializer : IBaseSerializer
    {
        /// <inheritdoc/>
        public Func<object, string> SerializeToString => throw new NotImplementedException();

        /// <inheritdoc/>
        public Func<object, byte[]> SerializeToByte => throw new NotImplementedException();

        /// <inheritdoc/>
        public Func<object, Task<string>> SerializeToStringAsync => throw new NotImplementedException();

        /// <inheritdoc/>
        public Func<object, Task<byte[]>> SerializeToByteAsync => throw new NotImplementedException();

        /// <inheritdoc/>
        Func<object, object> IBaseSerializer.SerializeObject { get; } = JsonSerializer.ToJsonString;

        /// <inheritdoc/>
        Func<object, Task<object>> IBaseSerializer.SerializeObjectAsync { get; } = obj =>
        {
            using var ms = new MemoryStream();
            JsonSerializer.Serialize(ms, obj);

            return Task.FromResult<object>(Convert.ToBase64String(ms.ToArray()));
        };

        /// <inheritdoc/>
        public T DeserializeObject<T>(object obj) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<T> DeserializeObjectAsync<T>(object obj) => throw new NotImplementedException();
    }
}