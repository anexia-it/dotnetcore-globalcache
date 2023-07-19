// ------------------------------------------------------------------------------------------
// <copyright file="GenericContainer.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Anexia.Caching.GlobalCacheTests.TestData.Generic
{
    /// <summary>
    /// Test data container which contains result changes
    /// </summary>
    /// <typeparam name="T">Type of the object</typeparam>
    [ExcludeFromCodeCoverage]
    public class GenericContainer<T>
        where T : class
    {
        /// <summary>
        /// Gets or sets a value indicating whether it should be saved in DB
        /// </summary>
        public bool BSave { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the object to be tested
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Test should fail
        /// </summary>
        public bool BShouldFail { get; set; }
    }
}