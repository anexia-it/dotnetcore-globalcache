// ------------------------------------------------------------------------------------------
// <copyright file="MoqXUnitInjections.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using ACIM.Sales.Core.Helper.Cache;
using ACIM.Sales.PriceListTest.MoqSetup.CacheMoq;
using Xunit;

namespace ACIM.Sales.PriceListTest.MoqSetup.Init
{
    [CollectionDefinition("UniCache")]
    public class MoqXUnitInjectionsCache
        : ICollectionFixture<MoqCache<UniversalCache>>
    {
    }
}