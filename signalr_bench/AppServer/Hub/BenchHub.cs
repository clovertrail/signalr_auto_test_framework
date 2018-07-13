// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR;
using System;
using Interlocked = System.Threading.Interlocked;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    public class BenchHub : Hub
    {
        private static int _totolReceivedEcho = 0;
        private static int _totolReceivedBroadcast = 0;
        public void Echo(string uid, string time)
        {
            Interlocked.Increment(ref _totolReceivedEcho);
            Clients.Client(Context.ConnectionId).SendAsync("echo", uid, time);
        }

        public void Broadcast(string uid, string time)
        {
            Interlocked.Increment(ref _totolReceivedBroadcast);
            Clients.All.SendAsync("broadcast", uid, time);
        }

        public void Count(string name)
        {
            var count = 0;
            if (name == "echo") count = _totolReceivedEcho; 
            if (name == "broadcast") count = _totolReceivedBroadcast;
            Clients.Client(Context.ConnectionId).SendAsync("count", count);
        }
    }
}
