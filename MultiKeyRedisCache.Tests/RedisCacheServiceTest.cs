using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using MultiKeyRedisCache.Tests.Models;
using NUnit.Framework;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiKeyRedisCache.Tests
{
    [TestFixture]
    public class RedisCacheServiceTest
    {
        #region Mock Data

        string _key1Name, _key2Name, _key3Name, _key4Name;
        private string _key5Name;
        string _value1;
        private Employee _employee1;
        private Employee _employee2;
        IList<Employee> _value2;
        IList<KeyValuePair<string, Employee>> _value3;
        ICacheService _cacheService;

        #endregion
        [OneTimeSetUp]
        public async Task Setup()
        {

            var config = new ConfigurationBuilder()
                .AddAzureAppConfiguration(options =>
                {
                    options.Connect("Endpoint=https://mvcappconfigurations.azconfig.io;Id=bf6U-l0-s0:EgleWI/fGqR3gIRMiZUB;Secret=YMN9aYLYYPoU4uyaUgy8m+91Aszq9fwHVPHhMTkYcp8=")
                    .Select(KeyFilter.Any, LabelFilter.Null)
                    .Select(KeyFilter.Any, "Development")
                    .ConfigureRefresh(refresh =>
                    {
                        refresh.Register("TestApp:Settings:Sentinel", refreshAll: true)
                                .SetCacheExpiration(new TimeSpan(0, 5, 0));
                    });
                })
            .Build();

            _cacheService = new RedisCacheService(new Lazy<ConnectionMultiplexer>(() =>
            {
                ConfigurationOptions options = ConfigurationOptions.Parse(config["TestApp:Settings:RedisConnectionString"]);
                options.AllowAdmin = true;
                options.SyncTimeout = 30000;
                return ConnectionMultiplexer.Connect(options);
            }));
            _key1Name = "k1";
            _key2Name = "k2";
            _key3Name = "k1p1";
            _key4Name = "k1p2";
            _key5Name = "k1p3";
            _value1 = "v1";
            _employee1 = new Employee { Id = 10, Name = "Ayaz" };
            _employee2 = new Employee { Id = 11, Name = "Saquib" };
            _value2 = new List<Employee> {
                _employee1,
                _employee2
            };

            _value3 = new List<KeyValuePair<String, Employee>>
            {
                new KeyValuePair<string, Employee>(_key4Name, _employee1),
                new KeyValuePair<string, Employee>(_key5Name, _employee2)
            };

            await _cacheService.Clear(_key1Name, _key2Name, _key3Name, _key4Name, _key5Name);
        }

        [Test]
        [Order(1)]
        public async Task Single_Value()
        {
            await _cacheService.Set(_key1Name, _value1);
            var result = await _cacheService.Get<string>(_key1Name);
            Assert.AreEqual(_value1, result);
        }

        [Test]
        public async Task Complex_Value()
        {
            await _cacheService.Set(_key2Name, _value2);
            var result = await _cacheService.Get<IList<Employee>>(_key2Name);
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(_value2, result);
        }

        [Test]
        public async Task Multiple_Value_of_Different_Type()
        {
            await _cacheService.Set(
                    new KeyValuePair<string, string>(_key3Name, _value1),
                    new KeyValuePair<string, IList<Employee>>(_key4Name, _value2)
                );
            var multiple = await _cacheService.Get<string, IList<Employee>>(_key3Name, _key4Name);
            Assert.IsNotNull(multiple);
            Assert.AreEqual(multiple.value1, _value1);
            CollectionAssert.AreEqual(multiple.value2, _value2);
        }

        [Test]
        public async Task Multiple_Value_of_Same_Type()
        {
            await _cacheService.Set(_value3);
            var multiple = await _cacheService.Get<Employee>(new String[] { _key4Name, _key5Name });
            Assert.IsNotNull(multiple);
            CollectionAssert.AreEqual(multiple, _value2); //using value2 instead of value3 just to remove keys from list of KeyValuePair
        }

        [Test]
        public async Task Batch_Mode()
        {
            var batch = _cacheService.Batch;
            var listTask = new List<Task>
            {
                batch.Add(_key1Name, _value1),
                batch.Add(_key2Name, _value2)
            };
            var result1Task = batch.Get<string>(_key1Name);
            batch.Execute();
            Task.WaitAll(listTask.ToArray());
            var result1 = await result1Task;
            Assert.AreEqual(_value1, result1);
        }
    }
}