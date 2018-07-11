using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Bench.RpcSlave.Worker.Counters;
using Bench.RpcSlave.Worker.Savers;
using Bench.RpcSlave.Worker.StartTimeOffsetGenerator;
using Bench.Common.Config;
using Bench.Common;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Bench.RpcSlave.Worker.Operations
{
    

    class EchoOp : BaseOp, IOperation
    {
        private List<System.Timers.Timer> TimerPerConnection;
        private List<TimeSpan> DelayPerConnection;
        private IStartTimeOffsetGenerator StartTimeOffsetGenerator;
        private List<int> _sentMessages;
        private WorkerToolkit _tk;

        public void Do(WorkerToolkit tk)
        {
            var waitTime = 15 * 1000;
            Console.WriteLine($"wait time: {waitTime/1000}s");
            Task.Delay(waitTime).Wait();

            // setup
            _tk = tk;
            Setup();

            _tk.State = Stat.Types.State.SendReady;

            // send message
            StartSendMsg();

            // save counters
            SaveCounters();

            _tk.State = Stat.Types.State.SendComplete;
            Util.Log($"exit echo");
        }

        private void Setup()
        {
            StartTimeOffsetGenerator = new RandomGenerator(new LocalFileSaver());

            _sentMessages = new List<int>(_tk.JobConfig.Connections);
            for (int i = 0; i < _tk.JobConfig.Connections; i++)
            {
                _sentMessages.Add(0);
            }

            SetCallbacks();
            //SetTimers();
            _tk.Counters.ResetCounters();
        }

        private void SetCallbacks()
        {
            for (int i = 0; i < _tk.Connections.Count; i++)
            {
                int ind = i;
                _tk.Connections[i].On(_tk.BenchmarkCellConfig.Scenario, (string uid, string time) =>
                {
                    var receiveTimestamp = Util.Timestamp();
                    var sendTimestamp = Convert.ToInt64(time);

                    _tk.Counters.CountLatency(sendTimestamp, receiveTimestamp);
                    if (ind == 0) Util.Log($"#### echocallback");
                });

                _tk.Connections[i].On("count", (int count) =>
                {
                    _tk.ServerCount = Math.Max(count, _tk.ServerCount);
                    _tk.Counters.SetServerCounter(_tk.ServerCount);
                });
            }
        }

        private void StartSendMsg()
        {
            _tk.State = Stat.Types.State.SendRunning;
            var tasks = new List<Task>(_tk.Connections.Count);
            for (var i = 0; i < _tk.Connections.Count; i++)
            {
                tasks.Add(StartSendingMessageAsync(_tk.Connections[i], i));
            }

            Task.WhenAll(tasks).Wait();
        }

        private async Task StartSendingMessageAsync(HubConnection connection, int ind)
        {
            await Task.Delay(StartTimeOffsetGenerator.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Interval)));
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_tk.JobConfig.Duration)))
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        if (ind == 0) Util.Log($"Start  sending messages");
                        await connection.SendAsync("echo", "id", $"{Util.Timestamp()}");
                        if (ind == 0) Util.Log($"Finish sending messages");
                        _sentMessages[ind]++;
                        _tk.Counters.IncreseSentMsg();
                    }
                    catch
                    {
                        _tk.Counters.IncreseNotSentFromClientMsg();
                    }

                    await Task.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Interval));
                }
            }
        }


        private void SaveCounters()
        {
            _tk.Counters.SaveCounters();
        }
    }
}
