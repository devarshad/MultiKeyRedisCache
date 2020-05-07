using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiKeyRedisCache
{
    public interface ICacheService
    {
        IBatch Batch { get; }
        Task Clear(params string[] keys);
        Task Set<T>(IEnumerable<KeyValuePair<string, T>> keyValuePairs);
        Task Set<T>(String key, T value);
        Task Set<T1, T2>(KeyValuePair<String, T1> keyValuePair1,
                                                   KeyValuePair<String, T2> keyValuePair2);
        Task Set<T1, T2, T3>(KeyValuePair<String, T1> keyValuePair1,
                                                    KeyValuePair<String, T2> keyValuePair2,
                                                    KeyValuePair<String, T3> keyValuePair3);
        Task<IEnumerable<T>> Get<T>(IEnumerable<string> keys);
        Task<T> Get<T>(string key);
        Task<(T1 value1, T2 value2)> Get<T1, T2>(string key1, string key2);
        Task<(T1 value1, T2 value2, T3 value3)> Get<T1, T2, T3>(string key1, string key2, string key3);
    }
}
