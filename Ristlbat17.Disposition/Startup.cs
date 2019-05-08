using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Newtonsoft.Json;
using Ristlbat17.Disposition.Material;
using Ristlbat17.Disposition.Reporting;
using Ristlbat17.Disposition.Reporting.Reports;
using Ristlbat17.Disposition.Servants;
using Swashbuckle.AspNetCore.Swagger;

namespace Ristlbat17.Disposition
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                // Avoid IE keeping API response in the local cache
                options.Filters.Add(new ResponseCacheAttribute
                {
                    NoStore = true,
                    Location = ResponseCacheLocation.None
                });
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSwaggerGen(options =>
            {
                options.EnableAnnotations();
                options.DescribeAllEnumsAsStrings();
                options.SwaggerDoc("v1", new Info {Title = "Disposition API", Version = "v1"});
                var filePath = Path.Combine(AppContext.BaseDirectory, "Ristlbat17.Disposition.xml");
                options.IncludeXmlComments(filePath);
            });
            services.Configure<DbSettings>(
                options =>
                {
                    options.ConnectionString = Configuration.GetSection("MongoDb:ConnectionString").Value;
                    options.Database = Configuration.GetSection("MongoDb:Database").Value;
                });

            services.AddSingleton<IDispositionContext, DispositionContext>();
            services.AddSingleton<IMaterialDispositionContext, DispositionContext>();
            services.AddSingleton<IServantDispositionContext, DispositionContext>();
            services.AddSingleton<IMaterialInventoryService, MaterialInventoryService>();
            services.AddSingleton<IServantInventoryService, ServantInventoryService>();
            services.AddTransient<IBataillonReporter, BataillonReporter>();
            services.AddTransient<BataillonOverviewReporter>();
            SetMongoDbConventions();

            SetupInitialData(services.BuildServiceProvider().GetService<IMaterialDispositionContext>());

            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration => { configuration.RootPath = "ClientApp/dist"; });
        }

        private static void SetupInitialData(IMaterialDispositionContext context)
        {            
            var companiesSampleDataPath = Path.Combine(".", "SampleData.Companies.json");
            if(File.Exists(companiesSampleDataPath))
            {
                var companiesJson = File.ReadAllText(companiesSampleDataPath);
                if (context.Material.CountDocuments(FilterDefinition<Material.Material>.Empty) == 0)
                {
                    var companies = JsonConvert.DeserializeObject<List<Company>>(companiesJson);
                    context.Companies.InsertMany(companies);
                }
            }

            var materialSampleDataPath = Path.Combine(".", "SampleData.Material.json");
            if(File.Exists(materialSampleDataPath))
            {
                var materialJson = File.ReadAllText(materialSampleDataPath);
                if (context.Material.CountDocuments(FilterDefinition<Material.Material>.Empty) == 0)
                {
                    var materials = JsonConvert.DeserializeObject<List<Material.Material>>(materialJson);
                    context.Material.InsertMany(materials);
                }
            }
        }

        private static string StreamToString(Stream stream)
        {
            stream.Position = 0;
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private void SetMongoDbConventions()
        {
            var pack = new ConventionPack
            {
                new EnumRepresentationConvention(BsonType.String)
            };

            ConventionRegistry.Register("EnumStringConvention", pack, t => true);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();
            app.UseSpaStaticFiles();
            app.UseSwagger();
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Disposition API V1"); });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    "default",
                    "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                // disable caching ... https://github.com/aspnet/AspNetCore/issues/3147
                spa.Options.DefaultPageStaticFileOptions = new StaticFileOptions
                {
                    OnPrepareResponse = ctx =>
                    {
                        // Do not cache implicit `/index.html`.  See also: `UseSpaStaticFiles` above
                        var headers = ctx.Context.Response.GetTypedHeaders();
                        headers.CacheControl = new CacheControlHeaderValue
                        {
                            Public = true,
                            MaxAge = TimeSpan.FromDays(0)
                        };
                    }
                };
                if (env.IsDevelopment())
                {
                    spa.UseAngularCliServer("start");
                }
            });
        }
    }
}