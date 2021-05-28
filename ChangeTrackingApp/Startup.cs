using AutoMapper;
using ChangeTrackingApp.Dal.Database;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

[assembly: WebJobsStartup(typeof(ChangeTrackingApp.Startup))]
namespace ChangeTrackingApp
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            try
            {
                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("local.settings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables();
                var configuration = configBuilder.Build();

                builder.Services.AddAutoMapper(typeof(Startup));

                var sqlConnectionString = configuration.GetValue<string>("SqlConnectionString");
                builder.Services.AddDbContext<ApiContext>(
                    options => options.UseSqlServer(sqlConnectionString)
                );

                builder.Services.AddSingleton<IConfiguration>(configuration);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Error in Startup.cs:", e);
            }
        }
    }
}
