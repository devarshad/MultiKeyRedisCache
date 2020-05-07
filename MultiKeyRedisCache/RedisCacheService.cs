using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiKeyRedisCache
{
    public class RedisCacheService : ICacheService
    {
        private readonly Lazy<ConnectionMultiplexer> _connectionMultiplexer;
        private ConnectionMultiplexer Connection => _connectionMultiplexer.Value;

        private IDatabase Database => Connection.GetDatabase();

        public RedisCacheService(string ConnectionString) : this(new Lazy<ConnectionMultiplexer>(() =>
          {
              ConfigurationOptions options = ConfigurationOptions.Parse(ConnectionString);
              options.AllowAdmin = true;
              options.SyncTimeout = 30000;
              return ConnectionMultiplexer.Connect(options);
          }))
        {

        }

        public RedisCacheService(Lazy<ConnectionMultiplexer> connectionMultiplexer) => this._connectionMultiplexer = connectionMultiplexer;
        public async Task Clear(params string[] keys)
        {
            var redisKeys = new RedisKey[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                redisKeys[i] = keys[i];
            }
            await Database.KeyDeleteAsync(redisKeys);
        }

        public async Task Set<T>(IEnumerable<KeyValuePair<string, T>> keyValuePairs)
        {
            var redisKeyValues = new List<KeyValuePair<RedisKey, RedisValue>>();
            foreach (var keyValuePair in keyValuePairs)
            {
                if (!String.IsNullOrWhiteSpace(keyValuePair.Key))
                {
                    redisKeyValues.Add(new KeyValuePair<RedisKey, RedisValue>(keyValuePair.Key, JsonConvert.SerializeObject(keyValuePair.Value, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore })));
                }
            }

            await Database.StringSetAsync(redisKeyValues.ToArray());
        }

        public async Task Set<T>(String key, T value)
        {
            await Set<T>(new KeyValuePair<String, T>(key, value));
        }
        private async Task Set<T1>(KeyValuePair<String, T1> keyValuePair1)
        {
            var keyValuePair2 = new KeyValuePair<String, Object>();
            var keyValuePair3 = new KeyValuePair<String, Object>();
            await Set(keyValuePair1, keyValuePair2, keyValuePair3);
        }
        public async Task Set<T1, T2>(KeyValuePair<String, T1> keyValuePair1,
                                                   KeyValuePair<String, T2> keyValuePair2)
        {
            var keyValuePair3 = new KeyValuePair<String, Object>();
            await Set(keyValuePair1, keyValuePair2, keyValuePair3);
        }
        public async Task Set<T1, T2, T3>(KeyValuePair<String, T1> keyValuePair1,
                                                    KeyValuePair<String, T2> keyValuePair2,
                                                    KeyValuePair<String, T3> keyValuePair3)
        {
            var keyValues = new List<KeyValuePair<RedisKey, RedisValue>>
            {
                new KeyValuePair<RedisKey, RedisValue>(keyValuePair1.Key, JsonConvert.SerializeObject(keyValuePair1.Value, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }))
            };
            if (keyValuePair2.Key != null)
            {
                keyValues.Add(new KeyValuePair<RedisKey, RedisValue>(keyValuePair2.Key, JsonConvert.SerializeObject(keyValuePair2.Value, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore })));

            }
            if (keyValuePair3.Key != null)
            {
                keyValues.Add(new KeyValuePair<RedisKey, RedisValue>(keyValuePair3.Key, JsonConvert.SerializeObject(keyValuePair3.Value, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore })));
            }

            await Database.StringSetAsync(keyValues.ToArray());
        }
        public async Task<IEnumerable<T>> Get<T>(IEnumerable<string> keys)
        {
            var redisKeyValues = new List<RedisKey>();
            foreach (var key in keys)
            {
                if (!String.IsNullOrWhiteSpace(key))
                {
                    redisKeyValues.Add(key);
                }
            }

            var radisValus = await Database.StringGetAsync(redisKeyValues.ToArray());
            var result = new List<T>();

            foreach (var redisValue in radisValus)
            {
                if (redisValue.HasValue)
                {
                    result.Add(JsonConvert.DeserializeObject<T>(redisValue, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
                }
            }
            return result;
        }
        public async Task<T> Get<T>(string key)
        {
            var (value1, _, _) = await Get<T, T, T>(key, null, null);
            return (value1);

        }
        public async Task<(T1 value1, T2 value2)> Get<T1, T2>(string key1, string key2)
        {
            var (value1, value2, _) = await Get<T1, T2, T2>(key1, key2, null);
            return (value1, value2);
        }
        public async Task<(T1 value1, T2 value2, T3 value3)> Get<T1, T2, T3>(string key1, string key2, string key3)
        {
            var redisKeys = new List<RedisKey>
            {
                key1
            };

            if (key2 != null)
            {
                redisKeys.Add(key2);
            }
            if (key3 != null)
            {
                redisKeys.Add(key3);
            }

            var resultPre = await Database.StringGetAsync(redisKeys.ToArray());
            var result = (default(T1), default(T2), default(T3));
            if (resultPre[0].HasValue)
            {
                result.Item1 = JsonConvert.DeserializeObject<T1>(resultPre[0], new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
            if (resultPre.Length > 1 && resultPre[1].HasValue)
            {
                result.Item2 = JsonConvert.DeserializeObject<T2>(resultPre[1], new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
            else if (resultPre.Length > 2 && resultPre[2].HasValue)
            {
                result.Item3 = JsonConvert.DeserializeObject<T3>(resultPre[2], new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            }
            return result;
        }

        public IBatch Batch => Database.CreateBatch();
    }

    public static class BatchExtensions
    {
        public static async Task Add<T>(this IBatch batch, string key, T value)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                await batch.StringSetAsync(key, JsonConvert.SerializeObject(value, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
            }
        }

        public static async Task<T> Get<T>(this IBatch batch, string key)
        {
            var redisValue = await batch.StringGetAsync(key);
            if (redisValue.HasValue)
            {
                return JsonConvert.DeserializeObject<T>(redisValue, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
            return default;
        }

        public static void Execute(this IBatch batch)
        {
            batch.Execute();
        }
    }
}
