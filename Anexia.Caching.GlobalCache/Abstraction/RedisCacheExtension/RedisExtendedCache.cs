// ------------------------------------------------------------------------------------------
// <copyright file="RedisExtendedCache.cs" company="ANEXIA® Internetdienstleistungs GmbH">
// Copyright (c) ANEXIA® Internetdienstleistungs GmbH. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Anexia.Caching.GlobalCache.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Anexia.Caching.GlobalCache.Abstraction.RedisCacheExtension
{
    /// <summary>
    /// Redis Cache manipulation class
    /// </summary>
    public class RedisExtendedCache : IDistributedCache, IDisposable
    {
        // KEYS[1] = = key
        // ARGV[1] = absolute-expiration - ticks as long (-1 for none)
        // ARGV[2] = sliding-expiration - ticks as long (-1 for none)
        // ARGV[3] = relative-expiration (long, in seconds, -1 for none) - Min(absolute-expiration - Now, sliding-expiration)
        // ARGV[4] = data - byte[]
        // this order should not change LUA script depends on it
        private const string SET_SCRIPT = @"
                redis.call('HMSET', KEYS[1], 'absexp', ARGV[1], 'sldexp', ARGV[2], 'data', ARGV[4])
                if ARGV[3] ~= '-1' then
                  redis.call('EXPIRE', KEYS[1], ARGV[3])
                end
                return 1";

        private const string ABSOLUTE_EXPIRATION_KEY = "absexp";
        private const string SLIDING_EXPIRATION_KEY = "sldexp";
        private const string DATA_KEY = "data";
        private const long NOT_PRESENT = -1;

        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private readonly string _instance;

        private readonly RedisCacheOptions _options;
        private IDatabase _cache;
        private volatile ConnectionMultiplexer _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisExtendedCache"/> class.
        /// </summary>
        /// <param name="optionsAccessor">Redis cache option object from Config</param>
        public RedisExtendedCache(IOptions<RedisCacheOptions> optionsAccessor)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _options = optionsAccessor.Value;

            // This allows partitioning a single backend cache for use with multiple apps/services.
            _instance = _options.InstanceName ?? string.Empty;
        }

        /// <summary>
        /// Redis Cache connection close
        /// </summary>
        public void Dispose()
        {
            _connection?.Close();
        }

        /// <summary>
        /// Gets cache entry from redis connection
        /// </summary>
        /// <param name="key">Key of the cache</param>
        /// <returns>Byte array of data</returns>
        public byte[] Get(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return GetAndRefresh(key, true);
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();
            return await GetAndRefreshAsync(key, true, token).ConfigureAwait(false);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Connect();

            var creationTime = DateTimeOffset.UtcNow;
            var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);

            _cache.ScriptEvaluate(
                SET_SCRIPT,
                new RedisKey[] { _instance + key },
                new RedisValue[]
                {
                    absoluteExpiration?.Ticks ?? NOT_PRESENT, options.SlidingExpiration?.Ticks ?? NOT_PRESENT,
                    GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? NOT_PRESENT, value
                });
        }

        public async Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            token.ThrowIfCancellationRequested();

            await ConnectAsync(token).ConfigureAwait(false);
            var creationTime = DateTimeOffset.UtcNow;
            var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);

            await _cache.ScriptEvaluateAsync(
                SET_SCRIPT,
                new RedisKey[] { _instance + key },
                new RedisValue[]
                {
                    absoluteExpiration?.Ticks ?? NOT_PRESENT, options.SlidingExpiration?.Ticks ?? NOT_PRESENT,
                    GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? NOT_PRESENT, value,
                }).ConfigureAwait(false);
        }

        public void Refresh(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            GetAndRefresh(key, false);
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();
            await GetAndRefreshAsync(key, false, token).ConfigureAwait(false);
        }

        public void Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Connect();
            _cache.KeyDelete(_instance + key);

            // TODO: Error handling
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            await ConnectAsync(token).ConfigureAwait(false);
            await _cache.KeyDeleteAsync(_instance + key).ConfigureAwait(false);

            // TODO: Error handling
        }

        public async Task<string[]> GetAllKeysAsync(string keyPrefix = default, CancellationToken token = default)
        {
            var result = new ConcurrentBag<string>();
            token.ThrowIfCancellationRequested();
            var i = 0;

            await ConnectAsync(token);

            var lengthToCut = _instance.Length;
            var keyPrefixLengthToCut = keyPrefix == default ? 1 : keyPrefix.Length + 1;
            var tasks = _options.ConfigurationOptions?.EndPoints != null
                ? new Task[_options.ConfigurationOptions.EndPoints.Count]
                : new Task[1];

            if (_options.ConfigurationOptions?.EndPoints != null)
            {
                foreach (var configurationOptionsEndPoint in _options.ConfigurationOptions?.EndPoints)
                {
                    tasks[i] = ConnectGetKeysAsync(configurationOptionsEndPoint, token: token).ContinueWith(
                        async task =>
                        {
                            if (task.IsCompletedSuccessfully)
                            {
                                await foreach (var redisKey in task.Result.WithCancellation(token))
                                {
                                    var key = redisKey.ToString()[lengthToCut..];
                                    if (keyPrefix != default && !key.StartsWith(keyPrefix))
                                    {
                                        continue;
                                    }

                                    key = key[keyPrefixLengthToCut..];
                                    result.Add(key);
                                }
                            }
                        },
                        TaskScheduler.Current).Unwrap();
                    i++;
                }
            }
            else
            {
                var connection = _connection.Configuration.Split(",").First();
                tasks[0] = ConnectGetKeysAsync(connection, token: token).ContinueWith(
                    async task =>
                    {
                        if (task.IsCompletedSuccessfully)
                        {
                            await foreach (var redisKey in task.Result.WithCancellation(token))
                            {
                                var key = redisKey.ToString()[lengthToCut..];
                                if (keyPrefix == default || key.StartsWith(keyPrefix))
                                {
                                    key = key[keyPrefixLengthToCut..];
                                    result.Add(key);
                                }
                            }
                        }
                    },
                    TaskScheduler.Current).Unwrap();
            }

            await Task.WhenAll(tasks);
            return result.ToArray();
        }

        public async Task<T[]> GetAllValuesAsync<T>(
            Func<byte[], Task<T>> bodyToConvert,
            string keyPrefix = default,
            CancellationToken token = default)
        {
            var result = new ConcurrentBag<T>();
            token.ThrowIfCancellationRequested();

            await ConnectAsync(token);

            var i = 0;
            var _instanceStringLength = _instance.Length;
            var processorToUse = Environment.ProcessorCount > 2 ? Environment.ProcessorCount - 1 : 1;
            var matchString = _instance + keyPrefix;
            var tasks = _options.ConfigurationOptions?.EndPoints != null
                ? new Task[_options.ConfigurationOptions.EndPoints.Count]
                : new Task[1];
            var getAllValueTasks = new Task[processorToUse];
            var counter = 0;

            async Task AsyncWorker(object key)
            {
                result.Add(await bodyToConvert(await GetAsync((string)key, token)));
            }

            Task ContinueWorker(Task task, object key)
            {
                return AsyncWorker(key);
            }

            async Task GetValuesOutOfKey(RedisKey redisKey)
            {
                if (processorToUse == counter)
                {
                    counter = 0;
                }

                var key = redisKey.ToString();
                if (keyPrefix != default && !key.StartsWith(matchString))
                {
                    return;
                }

                getAllValueTasks[counter] = getAllValueTasks[counter] != null ?
                    getAllValueTasks[counter]
                        .ContinueWith(
                            ContinueWorker,
                            key[_instanceStringLength..],
                            cancellationToken: token,
                            continuationOptions: TaskContinuationOptions.None,
                            scheduler: TaskScheduler.Current).Unwrap() :
                    Task.Factory.StartNew(
                        AsyncWorker,
                        key[_instanceStringLength..],
                        cancellationToken: token,
                        creationOptions: TaskCreationOptions.None,
                        scheduler: TaskScheduler.Current).Unwrap();
                counter++;
            }

            if (_options.ConfigurationOptions?.EndPoints != null)
            {
                foreach (var configurationOptionsEndPoint in _options.ConfigurationOptions?.EndPoints)
                {
                    tasks[i] = ConnectGetKeysAsync(configurationOptionsEndPoint, token: token)
                        .ContinueWith(
                        async task =>
                        {
                            if (task.IsCompletedSuccessfully)
                            {
                                await foreach (var redisKey in task.Result.WithCancellation(token))
                                {
                                    await GetValuesOutOfKey(redisKey);
                                }
                            }
                        },
                        TaskScheduler.Current).Unwrap();
                    i++;
                }
            }
            else
            {
                var connection = _connection.Configuration.Split(",").First();
                tasks[0] = ConnectGetKeysAsync(connection, token: token).ContinueWith(
                    async task =>
                    {
                        if (task.IsCompletedSuccessfully)
                        {
                            await foreach (var redisKey in task.Result.WithCancellation(token))
                            {
                                await GetValuesOutOfKey(redisKey);
                            }
                        }
                    },
                    TaskScheduler.Current).Unwrap();
            }

            await Task.WhenAll(tasks);
            return await Task.WhenAll(getAllValueTasks.Where(task => task != null))
                .ContinueWith(
                    task =>
                    {
                        if (task.IsFaulted && task.Exception != null)
                        {
                            throw task.Exception;
                        }

                        return result.ToArray();
                    },
                    token,
                    TaskContinuationOptions.None,
                    TaskScheduler.Current);
        }

        private void Connect()
        {
            if (_cache != null)
            {
                return;
            }

            _connectionLock.Wait();
            try
            {
                _connection = _options.ConfigurationOptions != null
                    ? ConnectionMultiplexer.Connect(_options.ConfigurationOptions)
                    : ConnectionMultiplexer.Connect(_options.Configuration);
                _cache = _connection.GetDatabase();
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        /// <summary>
        ///     Creates a connection to RedisCache and get the keys
        /// </summary>
        /// <param name="endPoint">Connection endpoint</param>
        /// <param name="pageOffset">Optional page offset to start from</param>
        /// <param name="pageSize">Optional page size</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Task with keys</returns>
        private async Task<IAsyncEnumerable<RedisKey>> ConnectGetKeysAsync(
            EndPoint endPoint,
            int pageOffset = 0,
            int pageSize = 250,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            if (_cache != null)
            {
                return _connection.GetServer(endPoint).KeysAsync(pageOffset: pageOffset, pageSize: pageSize);
            }

            await ConnectWithoutReleaseAsync(token).ConfigureAwait(false);
            try
            {
                return _connection.GetServer(endPoint).KeysAsync(pageOffset: pageOffset, pageSize: pageSize);
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        /// <summary>
        ///     Creates a connection to RedisCache and gets the keys
        /// </summary>
        /// <param name="connection">Connection string</param>
        /// <param name="pageOffset">Optional page offset to start from</param>
        /// <param name="pageSize">Optional page size</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Task with keys</returns>
        private async Task<IAsyncEnumerable<RedisKey>> ConnectGetKeysAsync(
            string connection,
            int pageOffset = 0,
            int pageSize = 250,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            if (_cache != null)
            {
                return _connection.GetServer(connection).KeysAsync(pageOffset: pageOffset, pageSize: pageSize);
            }

            await ConnectWithoutReleaseAsync(token).ConfigureAwait(false);
            try
            {
                return _connection.GetServer(connection).KeysAsync(pageOffset: pageOffset, pageSize: pageSize);
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        /// <summary>
        ///     Creates a connection to RedisCache and gets the keys
        /// </summary>
        /// <param name="key">Key to object</param>
        /// <param name="token">Cancellation token to act upon</param>
        /// <returns>Task with keys</returns>
        public async Task<bool> ConnectHasKeyAsync(
            string key,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            if (_cache != null)
            {
                return await _cache.KeyExistsAsync(new RedisKey(_instance + key));
            }

            await ConnectWithoutReleaseAsync(token).ConfigureAwait(false);
            try
            {
                return await _cache.KeyExistsAsync(new RedisKey(_instance + key));
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        /// <summary>
        ///     Creates a connection without releasing
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>Task on which it operates</returns>
        private async Task ConnectWithoutReleaseAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (_cache != null)
            {
                return;
            }

            await _connectionLock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                await SetConnectionAndCacheAsync();
            }
            catch
            {
                _connectionLock.Release();
            }
        }

        private async Task ConnectAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (_cache != null)
            {
                return;
            }

            await _connectionLock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                await SetConnectionAndCacheAsync();
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private byte[] GetAndRefresh(string key, bool getData)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Connect();

            // This also resets the LRU status as desired.
            // TODO: Can this be done in one operation on the server side? Probably, the trick would just be the DateTimeOffset math.
            var results = getData
                ? _cache.HashMemberGet(_instance + key, ABSOLUTE_EXPIRATION_KEY, SLIDING_EXPIRATION_KEY, DATA_KEY)
                : _cache.HashMemberGet(_instance + key, ABSOLUTE_EXPIRATION_KEY, SLIDING_EXPIRATION_KEY);

            // TODO: Error handling
            if (results.Length >= 2)
            {
                _ = Task.Run(
                    () =>
                    {
                        MapMetadata(results, out var absExpr, out var sldExpr);
                        Refresh(key, absExpr, sldExpr);
                    });
            }

            if (results.Length >= 3 && results[2].HasValue)
            {
                return results[2];
            }

            return null;
        }

        private async Task<byte[]> GetAndRefreshAsync(string key, bool getData, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            await ConnectAsync(token).ConfigureAwait(false);

            // This also resets the LRU status as desired.
            // TODO: Can this be done in one operation on the server side? Probably, the trick would just be the DateTimeOffset math.
            var results = getData
                ? await _cache.HashMemberGetAsync(
                    _instance + key,
                    ABSOLUTE_EXPIRATION_KEY,
                    SLIDING_EXPIRATION_KEY,
                    DATA_KEY).ConfigureAwait(false)
                : await _cache.HashMemberGetAsync(_instance + key, ABSOLUTE_EXPIRATION_KEY, SLIDING_EXPIRATION_KEY)
                    .ConfigureAwait(false);

            if (results.Length >= 2)
            {
                // TODO: Error handling
                _ = Task.Run(
                    async () =>
                    {
                        MapMetadata(results, out var absExpr, out var sldExpr);
                        await RefreshAsync(key, absExpr, sldExpr, token).ConfigureAwait(false);
                    },
                    token);
            }

            if (results.Length >= 3 && results[2].HasValue)
            {
                return results[2];
            }

            return null;
        }

        private void MapMetadata(
            RedisValue[] results,
            out DateTimeOffset? absoluteExpiration,
            out TimeSpan? slidingExpiration)
        {
            absoluteExpiration = null;
            slidingExpiration = null;
            var absoluteExpirationTicks = (long?)results[0];
            if (absoluteExpirationTicks.HasValue && absoluteExpirationTicks.Value != NOT_PRESENT)
            {
                absoluteExpiration = new DateTimeOffset(absoluteExpirationTicks.Value, TimeSpan.Zero);
            }

            var slidingExpirationTicks = (long?)results[1];
            if (slidingExpirationTicks.HasValue && slidingExpirationTicks.Value != NOT_PRESENT)
            {
                slidingExpiration = new TimeSpan(slidingExpirationTicks.Value);
            }
        }

        private void Refresh(string key, DateTimeOffset? absExpr, TimeSpan? sldExpr)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            // Note Refresh has no effect if there is just an absolute expiration (or neither).
            if (sldExpr.HasValue)
            {
                Connect();
                _cache.KeyExpire(
                    _instance + key,
                    CalculateUpdatedExpiration(absExpr, sldExpr.Value),
                    CommandFlags.FireAndForget);

                // TODO: Error handling
            }
        }

        private async Task RefreshAsync(
            string key,
            DateTimeOffset? absExpr,
            TimeSpan? sldExpr,
            CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            // Note Refresh has no effect if there is just an absolute expiration (or neither).
            if (sldExpr.HasValue)
            {
                await ConnectAsync(token);
                await _cache.KeyExpireAsync(
                        _instance + key,
                        CalculateUpdatedExpiration(absExpr, sldExpr.Value),
                        CommandFlags.FireAndForget)
                    .ConfigureAwait(false);

                // TODO: Error handling
            }
        }

        private static long? GetExpirationInSeconds(
            DateTimeOffset creationTime,
            DateTimeOffset? absoluteExpiration,
            DistributedCacheEntryOptions options)
        {
            if (absoluteExpiration.HasValue && options.SlidingExpiration.HasValue)
            {
                return (long)Math.Min(
                    (absoluteExpiration.Value - creationTime).TotalSeconds,
                    options.SlidingExpiration.Value.TotalSeconds);
            }

            if (absoluteExpiration.HasValue)
            {
                return (long)(absoluteExpiration.Value - creationTime).TotalSeconds;
            }

            if (options.SlidingExpiration.HasValue)
            {
                return (long)options.SlidingExpiration.Value.TotalSeconds;
            }

            return null;
        }

        private static DateTimeOffset? GetAbsoluteExpiration(
            DateTimeOffset creationTime,
            DistributedCacheEntryOptions options)
        {
            if (options.AbsoluteExpiration.HasValue && options.AbsoluteExpiration <= creationTime)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(DistributedCacheEntryOptions.AbsoluteExpiration),
                    options.AbsoluteExpiration.Value,
                    "The absolute expiration value must be in the future.");
            }

            var absoluteExpiration = options.AbsoluteExpiration;
            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                absoluteExpiration = creationTime + options.AbsoluteExpirationRelativeToNow;
            }

            return absoluteExpiration;
        }

        /// <summary>
        ///     Sets the connection through configuration (options) and sets the
        ///     cache database.
        /// </summary>
        private async Task SetConnectionAndCacheAsync()
        {
            _connection = _options.ConfigurationOptions != null
                ? await ConnectionMultiplexer.ConnectAsync(_options.ConfigurationOptions)
                    .ConfigureAwait(false)
                : await ConnectionMultiplexer.ConnectAsync(_options.Configuration)
                    .ConfigureAwait(false);
            _cache = _connection.GetDatabase();
        }

        /// <summary>
        ///     Calculates the updated absolute expiration date for an item
        /// </summary>
        /// <param name="absExpr">The current absolute expiration date offset</param>
        /// <param name="sldExpr">The current sliding expiration</param>
        /// <returns>The calculated expiration</returns>
        private TimeSpan? CalculateUpdatedExpiration(DateTimeOffset? absExpr, TimeSpan sldExpr)
        {
            TimeSpan? expr;
            if (absExpr.HasValue)
            {
                var relExpr = absExpr.Value - DateTimeOffset.Now;
                expr = relExpr <= sldExpr
                    ? relExpr
                    : sldExpr;
            }
            else
            {
                expr = sldExpr;
            }

            return expr;
        }
    }
}