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
            var waitTime = 0 * 1000;
            Console.WriteLine($"wait time: {waitTime/1000}s");
            Task.Delay(waitTime).Wait();

            // setup
            _tk = tk;
            Setup();

            _tk.State = Stat.Types.State.SendReady;

            // send message
            StartSendMsg();

            // wait to stop
            Task.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Duration + 30)).ContinueWith(t => {
                SaveCounters();
            }).Wait();

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
            SetTimers();
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
            for (int i = 0; i < _tk.Connections.Count; i++)
            {
                int ind = i;
                _ = Task.Delay(DelayPerConnection[i]).ContinueWith(_ =>
                {
                    TimerPerConnection[ind].Start();
                });
            }
        }

        private void SetTimers()
        {
            TimerPerConnection = new List<System.Timers.Timer>(_tk.JobConfig.Connections);
            DelayPerConnection = new List<TimeSpan>(_tk.JobConfig.Connections);


            for (int i = 0; i < _tk.Connections.Count; i++)
            {
                var delay = StartTimeOffsetGenerator.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Interval));
                DelayPerConnection.Add(delay);

                TimerPerConnection.Add(new System.Timers.Timer());

                var ind = i;
                TimerPerConnection[i].AutoReset = true;
                TimerPerConnection[i].Elapsed += (sender, e) =>
                {
                    TimerPerConnection[ind].Stop();
                    TimerPerConnection[ind].Interval = _tk.JobConfig.Interval * 1000;
                    TimerPerConnection[ind].Start();

                    if (_sentMessages[ind] >= _tk.JobConfig.Duration / _tk.JobConfig.Interval)
                    {
                        if (_sentMessages[ind] == _tk.JobConfig.Duration / _tk.JobConfig.Interval)
                        {
                            if (ind == 0) Util.Log($"Get Server Count");
                            _tk.Connections[0].SendAsync("count", "echo");
                        }
                        TimerPerConnection[ind].Stop();
                        return;
                    }


                    if (ind == 0)
                    {
                        Util.Log($"@@@@ timer send");
                    }
                    _sentMessages[ind]++;
                    _tk.Counters.IncreseSentMsg();
                    _tk.Connections[ind].SendAsync(_tk.BenchmarkCellConfig.Scenario, $"{Util.GuidEncoder.Encode(Guid.NewGuid())}", $"{Util.Timestamp()}");
                };
            }
        }

        private void SaveCounters()
        {
            _tk.Counters.SaveCounters();
        }
    }
}
