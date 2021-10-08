// ------------------------------------------------------------------------------------------
// <copyright file="TestDataIterator.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using ACIM.Sales.PriceListTest.TestData.Generic;
using Anexia.Caching.GlobalCacheTests.TestData.Generic;

namespace ACIM.Sales.PriceListTest.TestData.Sales.Core
{
    /// <summary>
    /// Test data iterator which creates faulty objects and healthy objects
    /// </summary>
    /// <typeparam name="TItem">Type of item to create</typeparam>
    public class TestDataIterator<TItem> : IEnumerable<object[]>
        where TItem : class, new()
    {
        private const string NAME = "something";
        private const string FAILNAME = "Failed something";

        IEnumerator<object[]> IEnumerable<object[]>.GetEnumerator() =>
            GetEnumerator<TItem>();

        public IEnumerator GetEnumerator() => GetEnumerator<TItem>();

        public IEnumerator<GenericContainer<T>[]> GetEnumerator<T>()
            where T : class, new()
        {
            var obj = CreateGenericObject<T>(false);
            var objFail = CreateGenericObject<T>(true);

            // Success test
            // First object will be saved
            yield return new[] { obj, obj };

            // fail test
            // wont be saved
            yield return new[] { objFail, null };
        }

        public GenericContainer<T> CreateGenericObject<T>(bool createFail)
            where T : class, new()
        {
            var defObject = new T();
            switch (defObject)
            {
                case List<Block> obj:
                    if (!createFail)
                    {
                        for (var i = 0; i < 512 * 512 * 512; i++)
                        {
                            obj.Add(new Block());
                        }
                    }
                    else
                    {
                        obj = null;
                    }

                    break;
            }

            return new GenericContainer<T> { Data = defObject, BSave = !createFail, BShouldFail = createFail };
        }
    }
}