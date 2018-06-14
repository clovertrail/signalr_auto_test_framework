// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR;
using System;


namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    public class EchoHub : Hub
    {
        public void Echo(string uid, string time)
        {
            //var receiveTime = DateTime.Now.ToString("hh:mm:ss.fff");
            
            // TODO: configuer callback name from config file
            Clients.Client(Context.ConnectionId).SendAsync("EchoCallback", uid, time);
        }
    }
}
