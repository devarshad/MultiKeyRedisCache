using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MultiKeyRedisCache.Samples
{
    public class Program
    {
        static IConfiguration Configuration;
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        Configuration = config.Build();
                        Configuration = config.AddAzureAppConfiguration(options =>
                           {
                               options.Connect(Configuration["AppSettingsConnectionString"])
                               .Select(KeyFilter.Any, LabelFilter.Null)
                               .Select(KeyFilter.Any, "Development")
                               .ConfigureRefresh(refresh =>
                               {
                                   refresh.Register("TestApp:Settings:Sentinel", refreshAll: true)
                                           .SetCacheExpiration(new TimeSpan(0, 5, 0));
                               });
                           }).Build();
                    })
                    .UseStartup<Startup>()
                    .ConfigureLogging(
                        builder =>
                        {
                            // Providing an instrumentation key here is required if you're using
                            // standalone package Microsoft.Extensions.Logging.ApplicationInsights
                            // or if you want to capture logs from early in the application startup 
                            // pipeline from Startup.cs or Program.cs itself.
                            builder.AddApplicationInsights(Configuration["TestApp:Settings:ApplicationInsightsKey"]);

                            // Adding the filter below to ensure logs of all severity from Startup.cs
                            // is sent to ApplicationInsights.
                            //   builder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>
                            //                  (typeof(Startup).FullName, LogLevel.Trace);
                        }
                    );
                });
    }
}
