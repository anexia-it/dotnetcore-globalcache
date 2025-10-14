// ------------------------------------------------------------------------------------------
// <copyright file="RedisExtensionsLocal.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using StackExchange.Redis;

namespace Anexia.Caching.GlobalCache.Extensions
{
    /// <summary>
    ///     Redis extensions for managing redis connections
    /// </summary>
    internal static class RedisExtensionsLocal
    {
        internal static RedisValue[] HashMemberGet(this IDatabase cache, string key, params string[] members) =>
            cache.HashGet(key, GetRedisMembers(members)); // TODO: Error checking?

        internal static async Task<RedisValue[]> HashMemberGetAsync(
            this IDatabase cache,
            string key,
            params string[] members) =>
            await cache.HashGetAsync(key, GetRedisMembers(members))
                .ConfigureAwait(false); // TODO: Error checking?

        private static RedisValue[] GetRedisMembers(params string[] members)
        {
            var redisMembers = new RedisValue[members.Length];
            for (var i = 0; i < members.Length; i++)
            {
                redisMembers[i] = (RedisValue)members[i];
            }

            return redisMembers;
        }
    }
}