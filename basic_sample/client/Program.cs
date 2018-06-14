using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace client
{
    class Program
    {
        static private bool useMessagePack = false;

        static async Task Main(string[] args)
        {
            var url = "http://localhost:5000/benchmark";
            
            var transportType = HttpTransportType.WebSockets;
            // var transportType = HttpTransportType.ServerSentEvents;
            // var transportType = HttpTransportType.LongPolling;

            var hubConnectionBuilder = new HubConnectionBuilder()
                .WithUrl(url, httpConnectionOptions => 
                {
                    // set transport type
                    httpConnectionOptions.Transports = transportType;
                });

                if (useMessagePack)
                    hubConnectionBuilder.AddMessagePackProtocol();

            var connection = hubConnectionBuilder.Build();

            await connection.StartAsync();

            Console.WriteLine($"Starting connection with transport type {transportType}. Press Ctrl-C to close.");

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, a) =>
            {
                a.Cancel = true;
                cts.Cancel();
            };

            connection.Closed += e =>
            {
                if (e != null) 
                {
                    Console.WriteLine("Connection closed with error: {0}", e);
                } 
                else 
                {
                    Console.WriteLine("Connection closed");
                }
                cts.Cancel();
                return Task.CompletedTask;
            };

            connection.On("echo", (string name, string message) =>
            {
                Console.WriteLine($"INFO: server -> client: {name}: {message}");
            });

            await connection.InvokeAsync<string>("Echo", "albert", "hello");

            await connection.StopAsync();
        }
    }
}
