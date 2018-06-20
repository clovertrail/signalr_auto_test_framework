using Bench.Common;
using Bench.Common.Config;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Bench.Server.Worker.Operations
{
    class CreateConnOp : BaseOp, IOperation
    {
        private WorkerToolkit _tk;
        public void Do(WorkerToolkit tk)
        {
            _tk = tk;

            // TODO: remove, only for debug
            //_tk.Test = new List<int>();
            //_tk.Test.Add(1);

            _tk.State = Stat.Types.State.HubconnUnconnected;
            _tk.Connections = Create(_tk.JobConfig.Connections/_tk.JobConfig.Slaves, _tk.JobConfig.ServerUrl, _tk.JobConfig.TransportType, _tk.JobConfig.HubProtocol);
        }

        private List<HubConnection> Create(int conn, string url, 
            string transportTypeName = "Websockets",
            string hubProtocol = "json") 
        {
            Util.Log($"transport type: {transportTypeName}");
            var transportType = HttpTransportType.WebSockets;
            switch (transportTypeName)
            {
                case "LongPolling":
                    transportType = HttpTransportType.LongPolling;
                    break;
                case "ServerSentEvents":
                    transportType = HttpTransportType.ServerSentEvents;
                    break;
                case "None":
                    transportType = HttpTransportType.None;
                    break;
                default:
                    transportType = HttpTransportType.WebSockets;
                    break;
            }

            var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                MaxConnectionsPerServer = 200000,
                MaxAutomaticRedirections = 200000
            };

            _tk.State = Stat.Types.State.HubconnCreating;
            var connections = new List<HubConnection>(conn);
            for (var i = 0; i < conn; i++)
            {
                var hubConnectionBuilder = new HubConnectionBuilder()
                .WithUrl(url, httpConnectionOptions =>
                {
                    httpConnectionOptions.HttpMessageHandlerFactory = _ => httpClientHandler;
                    httpConnectionOptions.Transports = transportType;
                    httpConnectionOptions.CloseTimeout = TimeSpan.FromMinutes(100);
                });

                HubConnection connection = null;
                switch (hubProtocol)
                {
                    case "json":
                        connection = hubConnectionBuilder.Build();
                        break;
                    case "messagepack":
                        connection = hubConnectionBuilder.AddMessagePackProtocol().Build();
                        break;
                    default:
                        throw new Exception($"{hubProtocol} is invalid.");
                }

                connections.Add(connection);
            }

            _tk.State = Stat.Types.State.HubconnCreated;
            return connections;

        }
        
    }
}
