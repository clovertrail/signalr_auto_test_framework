// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.SignalR.PerfTest.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            useLocalSignalR = true;
            useMessagePack = false;
        }

        public IConfiguration Configuration { get; }
        private bool useLocalSignalR = true;
        private bool useMessagePack = true;
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            if (useLocalSignalR)
                if (useMessagePack)
                    services.AddSignalR().AddMessagePackProtocol();
                else
                    services.AddSignalR();
            else
                if (useMessagePack)
                    services.AddSignalR().AddMessagePackProtocol().AddAzureSignalR(Environment.GetEnvironmentVariable("AzureSignalRConnectionString"));
                else
                    services.AddSignalR().AddAzureSignalR(Environment.GetEnvironmentVariable("AzureSignalRConnectionString"));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvc();
            app.UseFileServer();
            if (useLocalSignalR)
                app.UseSignalR(routes =>
                {
                    routes.MapHub<ServerHub>("/test");
                });
            else
                app.UseAzureSignalR(routes =>
                {
                    routes.MapHub<ServerHub>("/test");
                });

        }
    }
}
