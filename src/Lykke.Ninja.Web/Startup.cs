using System;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Logs;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.Settings.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Lykke.Ninja.Web.Binders;
using Lykke.Ninja.Web.Filters;
using Lykke.Ninja.Web.Proxy;
using Lykke.SettingsReader;
using Swashbuckle.AspNetCore.Swagger;

namespace Lykke.Ninja.Web
{
    public class Startup
    {
        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; private set; }
        public IConfigurationRoot Configuration { get; }
        public ILog Log { get; private set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }
        
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(o =>
                {
                    o.Filters.Add(new HandleAllExceptionsFilterFactory());
                })
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Formatting = Formatting.Indented;
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Info { Title = "lykke.ninja", Version = "v1" });

                options.DescribeAllEnumsAsStrings();
            });

            var settings = Configuration.LoadSettings<GeneralSettings>();

            Log = CreateLog(services, settings);
            var builder = new AzureBinder().Bind(settings.CurrentValue, Log);
            builder.Populate(services);
            

            return new AutofacServiceProvider(builder.Build());
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            var settings = Configuration.LoadSettings<GeneralSettings>();


            if (!settings.CurrentValue.LykkeNinja.Proxy.ProxyAllRequests)
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseStaticFiles();

                app.UseSwagger();

                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                });

                app.UseMvc();
            }

            appLifetime.ApplicationStarted.Register(() => StartApplication().Wait());
            appLifetime.ApplicationStopping.Register(() => StopApplication().Wait());
            appLifetime.ApplicationStopped.Register(() => CleanUp().Wait());

            var ninjaUrl = new Uri(settings.CurrentValue.LykkeNinja.NinjaUrl);
            app.RunProxy(new ProxyOptions { Host = ninjaUrl.Host, Scheme = ninjaUrl.Scheme });
        }



        private async Task StartApplication()
        {
            try
            {
                await Log.WriteMonitorAsync("", "", "Started");
            }
            catch (Exception ex)
            {
                await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StartApplication), "", ex);
                throw;
            }
        }

        private async Task StopApplication()
        {
            try
            {

            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StopApplication), "", ex);
                }
                throw;
            }
        }

        private async Task CleanUp()
        {
            try
            {
                if (Log != null)
                {
                    await Log.WriteMonitorAsync("", "", "Terminating");
                }

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(CleanUp), "", ex);
                    (Log as IDisposable)?.Dispose();
                }
                throw;
            }
        }


        private static ILog CreateLog(IServiceCollection services, IReloadingManager<GeneralSettings> settings)
        {
            var consoleLogger = new LogToConsole();
            var aggregateLogger = new AggregateLogger();

            aggregateLogger.AddLog(consoleLogger);

            var dbLogConnectionStringManager = settings.Nested(x => x.LykkeNinja.Db.LogsConnString);
            var dbLogConnectionString = dbLogConnectionStringManager.CurrentValue;
            
            if (!string.IsNullOrEmpty(dbLogConnectionString) && !(dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}")))
            {
                var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(
                    AzureTableStorage<LogEntity>.Create(dbLogConnectionStringManager, "LykkeNinjaWebLogs", consoleLogger),
                    consoleLogger);
                

                var azureStorageLogger = new LykkeLogToAzureStorage(
                    persistenceManager,
                    null,
                    consoleLogger);

                azureStorageLogger.Start();

                aggregateLogger.AddLog(azureStorageLogger);
            }

            return aggregateLogger;
        }
    }
}
