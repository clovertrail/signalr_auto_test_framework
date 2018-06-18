// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR;
using System;


namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    public class BenchHub : Hub
    {
        public void Echo(string uid, string time)
        {
            Clients.Client(Context.ConnectionId).SendAsync("EchoCallback", uid, time);
        }

        public void Broadcast(string uid, string time)
        {
            Clients.All.SendAsync("BroadcastCallback", uid, time);
        }
    }
}
