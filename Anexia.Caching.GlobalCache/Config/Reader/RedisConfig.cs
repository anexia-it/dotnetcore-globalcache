// ------------------------------------------------------------------------------------------
// <copyright file="RedisConfig.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using System;
using System.IO;
using Anexia.Caching.GlobalCache.Config.Model;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;

namespace Anexia.Caching.GlobalCache.Config.Reader
{
    // dotcover disable

    /// <summary>
    /// Redis configuration reader
    /// </summary>
    public static class RedisConfig
    {
        /// <summary>
        /// Reads redis options from config
        /// </summary>
        /// <returns>RedisCacheOptions or default</returns>
        public static RedisCacheOptions ReadRedisFromConfig()
        {
            var redisConfig = ReadFromConfigFile(
                new ConfigurationBuilder()
                    .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                    .AddJsonFile("appsettings.json", false)
                    .Build());
            if (redisConfig != null)
            {
                return ReadRedisFromConfig(redisConfig);
            }

            return null;
        }

        /// <summary>
        /// Reads redis from config object
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <returns>RedisCacheOptions or default</returns>
        public static RedisConfigModel ReadFromConfigFile(IConfigurationRoot config)
        {
            if (config?.GetSection("RedisConfiguration:Configuration")?.Value != null)
            {
                return new RedisConfigModel
                {
                    Configuration = config.GetSection("RedisConfiguration:Configuration")?.Value ?? default,
                    InstanceName = config.GetSection("RedisConfiguration:InstanceName")?.Value ?? default
                };
            }

            return null;
        }

        /// <summary>
        /// Reads redis cache options from RedisConfigModel
        /// </summary>
        /// <param name="options">Options for RedisConfig</param>
        /// <returns>RedisCacheOptions or default</returns>
        public static RedisCacheOptions ReadRedisFromConfig(RedisConfigModel options)
        {
            RedisCacheOptions ret = default;
            if (options != null)
            {
                ret = new RedisCacheOptions
                {
                    Configuration = options.Configuration,
                    InstanceName = options.InstanceName,
                    ConfigurationOptions = options.ConfigurationOptions
                };
            }

            return ret;
        }
    }

    // dotcover enable
}