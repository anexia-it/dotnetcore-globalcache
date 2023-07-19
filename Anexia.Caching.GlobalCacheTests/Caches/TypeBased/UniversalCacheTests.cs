// ------------------------------------------------------------------------------------------
// <copyright file="UniversalCacheTests.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Anexia.Caching.GlobalCache.Caches.TypeBased;
using Anexia.Caching.GlobalCacheTests.CacheMoq;
using Anexia.Caching.GlobalCacheTests.TestData.Generic;
using Anexia.Caching.GlobalCacheTests.TestData.Sales.Core;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;
using ThreadState = System.Threading.ThreadState;

namespace Anexia.Caching.GlobalCacheTests.Caches.TypeBased
{
    /// <summary>
    /// Universal test cache
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Collection("UniCache")]
    [Trait("Category", "GitHub")]
    public class UniversalCacheTests
    {
        private readonly ITestOutputHelper output;
        private readonly UniversalCache universalCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniversalCacheTests"/> class.
        /// </summary>
        /// <param name="_universalCache">Universal cache moq</param>
        /// <param name="_output">Console output log helper</param>
        public UniversalCacheTests(
            MoqCache<UniversalCache> _universalCache,
            ITestOutputHelper _output)
        {
            output = _output;
            universalCache = _universalCache.CacheService;
        }

        /// <summary>
        /// Tests how fast universal cahce is
        /// </summary>
        /// <param name="toSearch">Save and retrive item</param>
        /// <param name="expected">Control item</param>
        /// <returns>Task of execution</returns>
        [Theory]
        [ClassData(typeof(TestDataIterator<List<Block>>))]
        public async Task InsertAsyncStressTest(
            GenericContainer<List<Block>> toSearch,
            GenericContainer<List<Block>> expected)
        {
            var lstThread = new List<Thread>();
            var lstOfThrd = new List<Task>();
            var stp = Stopwatch.StartNew();
            for (var z = 0; z < 5; z++)
            {
                lstThread.Add(
                    new Thread(
                        () =>
                        {
                            for (var i = 0; i < 300; i++)
                            {
                                lstOfThrd.Add(
                                    new Task(
                                        () =>
                                        {
                                            _ = universalCache.InsertAsync(toSearch.Data);
                                        }));
                            }

                            try
                            {
                                lstOfThrd.ForEach(
                                    x =>
                                    {
                                        if (x.Status == TaskStatus.Created)
                                        {
                                            x.Start();
                                        }
                                    });
                                Task.WaitAll(lstOfThrd.ToArray());
                            }
                            catch (Exception x)
                            {
                                Assert.True(false, x.Message);
                            }
                        }));
            }

            lstOfThrd.ForEach(x => x.Start());
            foreach (var thrd in lstThread)
            {
                if (thrd.ThreadState == ThreadState.Running)
                {
                    thrd.Join();
                }
                else
                {
                    thrd.Start();
                    thrd.Join();
                }
            }

            stp.Stop();
            Assert.NotNull(universalCache.Get<List<Block>>());
            output.WriteLine($"Nr: {(await universalCache.GetAsync<int>()).ToString()}");
            output.WriteLine($": {stp.Elapsed.TotalSeconds}");
        }

        /// <summary>
        /// Insert async object test
        /// </summary>
        /// <param name="toSearch">To save object</param>
        /// <param name="expected">Control test object</param>
        /// <returns>Execution task</returns>
        [Theory]
        [ClassData(typeof(TestDataIterator<List<Block>>))]
        public async Task InsertAsyncTest(
            GenericContainer<List<Block>> toSearch,
            GenericContainer<List<Block>> expected)
        {
            await universalCache.InsertAsync(
                toSearch?.Data,
                toSearch?.BShouldFail ?? false ? DateTime.Now.AddDays(-1) : DateTime.Now.AddDays(1));
            if (!(toSearch?.BShouldFail ?? true))
            {
                var obj = await universalCache.GetAsync<List<Block>>();
                Assert.NotNull(await universalCache.GetAsync<List<Block>>());
            }
            else
            {
                Assert.Null(universalCache.Get<List<Block>>());
            }
        }

        /// <summary>
        /// Duration test of async method
        /// </summary>
        /// <param name="toSearch">Object to save</param>
        /// <param name="expected">Control object</param>
        /// <returns>Execution task</returns>
        [Theory]
        [ClassData(typeof(TestDataIterator<List<Block>>))]
        public async Task InsertAsyncTestDuration(
            GenericContainer<List<Block>> toSearch,
            GenericContainer<List<Block>> expected)
        {
            if (!(toSearch?.BShouldFail ?? true))
            {
                await universalCache.InsertAsync(toSearch?.Data, 10);
            }
            else
            {
                await universalCache.InsertAsync(toSearch?.Data, DateTime.UtcNow.AddHours(-1));
            }

            if (!(toSearch?.BShouldFail ?? true))
            {
                var obj = await universalCache.GetAsync<List<Block>>();
                Assert.NotNull(obj);
            }
            else
            {
                Assert.Null(universalCache.Get<List<Block>>());
            }
        }

        /// <summary>
        /// Get all feature test
        /// </summary>
        /// <param name="toSearch">To save object</param>
        /// <param name="expected">Control object</param>
        /// <returns>Execution task</returns>
        [Theory]
        [ClassData(typeof(TestDataIterator<List<Block>>))]
        public async Task GetAll(GenericContainer<List<Block>> toSearch, GenericContainer<List<Block>> expected)
        {
            if (!(toSearch?.BShouldFail ?? true))
            {
                await universalCache.InsertAsync(toSearch?.Data, 10);
            }
            else
            {
                await universalCache.InsertAsync(toSearch?.Data, DateTime.UtcNow.AddHours(-1));
            }

            var getAll = universalCache.GetAll();
            var getKeys = universalCache.GetAllKeys();
            var getValues = universalCache.GetAllValues();

            if (!(toSearch?.BShouldFail ?? true))
            {
                Assert.NotEmpty(getAll);
                Assert.NotEmpty(getValues);
                Assert.NotEmpty(getKeys);
            }
            else
            {
                Assert.Empty(getAll);
                Assert.Empty(getValues);
            }
        }
    }
}