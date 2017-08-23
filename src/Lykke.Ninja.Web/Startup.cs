using System;
using Autofac.Extensions.DependencyInjection;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.Settings.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Swashbuckle.Swagger.Model;
using Lykke.Ninja.Web.Binders;
using Lykke.Ninja.Web.Filters;
using Lykke.Ninja.Web.Proxy;

namespace Lykke.Ninja.Web
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }


        private GeneralSettings GetSettings()
        {
#if DEBUG
            var settings = GeneralSettingsReader.ReadGeneralSettingsLocal<GeneralSettings>("../../settings.json");
#else
            var settings = GeneralSettingsReader.ReadGeneralSettings<GeneralSettings>(Configuration["SettingsUrl"]);
#endif

            GeneralSettingsValidator.Validate(settings);

            return settings;
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
                });

            services.AddSwaggerGen(options =>
            {
                options.SingleApiVersion(new Info
                {
                    Version = "v1",
                    Title = "lykke.ninja"
                });
                options.DescribeAllEnumsAsStrings();
            });

            var settings = GetSettings();
            var builder = new AzureBinder().Bind(settings);
            builder.Populate(services);
            

            return new AutofacServiceProvider(builder.Build());
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {

            var settings = GetSettings();

            if (!settings.LykkeNinja.Proxy.ProxyAllRequests)
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                app.UseSwagger();
                app.UseSwaggerUi("swagger/ui/index");



                app.UseMvc();
            }

            var ninjaUrl = new Uri(settings.LykkeNinja.NinjaUrl);
            app.RunProxy(new ProxyOptions { Host = ninjaUrl.Host, Scheme = ninjaUrl.Scheme });
        }
    }
}
