// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            useLocalSignalR = false;
            useMessagePack = true;

            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"use local signalr: {useLocalSignalR}, use msgpack: {useMessagePack}");
            Console.BackgroundColor = ConsoleColor.Black;
        }

        public IConfiguration Configuration { get; }
        private bool useLocalSignalR = false;
        private bool useMessagePack = true;
        
        public void ConfigureServices(IServiceCollection services)
        {
            string connectionStr = Environment.GetEnvironmentVariable("AzureSignalRConnectionString");
            Console.WriteLine($"@@@ connection string: {connectionStr}");
            services.AddMvc();
            if (useLocalSignalR)
                if (useMessagePack)
                    services.AddSignalR().AddMessagePackProtocol();
                else
                    services.AddSignalR();
            else
                if (useMessagePack)
                    services.AddSignalR().AddMessagePackProtocol().AddAzureSignalR(connectionStr);
                else
                   services.AddSignalR().AddAzureSignalR(connectionStr);
        }

        public void Configure(IApplicationBuilder app)
        {
            // TODO: configure endpoint from file
            app.UseMvc();
            app.UseFileServer();
            if (useLocalSignalR)
                app.UseSignalR(routes =>
                {
                    routes.MapHub<BenchHub>("/signalrbench");
                });
            else
                app.UseAzureSignalR(routes =>
                {
                    routes.MapHub<BenchHub>("/signalrbench");
                });

        }

    }
}
