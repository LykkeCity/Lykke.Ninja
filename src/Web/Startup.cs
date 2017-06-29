using System;
using Autofac.Extensions.DependencyInjection;
using Core.Settings;
using Core.Settings.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.Swagger.Model;
using Web.Binders;
using Web.Proxy;

namespace Web
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


        private BaseSettings GetSettings()
        {
#if DEBUG
            var settings = GeneralSettingsReader.ReadGeneralSettingsLocal<BaseSettings>("../../settings.json");
#else
            var generalSettings = GeneralSettingsReader.ReadGeneralSettings<GeneralSettings>(Configuration["SettingsUrl"]);
            var settings = generalSettings?.LykkeNinja;
#endif

            GeneralSettingsValidator.Validate(settings);

            return settings;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

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

            if (!settings.Proxy.ProxyAllRequests)
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                app.UseSwagger();
                app.UseSwaggerUi("swagger/ui/index");



                app.UseMvc();
            }

            var ninjaUrl = new Uri(settings.NinjaUrl);
            app.RunProxy(new ProxyOptions { Host = ninjaUrl.Host, Scheme = ninjaUrl.Scheme });
        }
    }
}
