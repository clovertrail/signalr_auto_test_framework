using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using Client.UtilNs;
using Client.Statistics;
using Client.Tools;
using System.Threading.Tasks;
using Client.ClientJobNs;
using Client.StartTimeOffsetGenerator;
using Client.Statistics.Savers;

namespace Client.Workers.OperationsNs
{
    class BaseOp
    {
        
    }

    class EchoOp: BaseOp, IOperation
    {
        public ICounters Counters { get; set; } = new Counters(new LocalFileSaver());
        public List<System.Timers.Timer> TimerPerConnection;
        public List<TimeSpan> DelayPerConnection;
        private BaseTool _pkg;
        public IStartTimeOffsetGenerator StartTimeOffsetGenerator;

        public EchoOp(BaseTool pkg)
        {
            _pkg = pkg;
            StartTimeOffsetGenerator = new RandomGenerator(new LocalFileSaver());
        }

        public void Setup()
        {
            SetCallbacks();
            SetTimers();

            for(int i = 0; i < _pkg.SentMassage.Count; i++)
            {
                _pkg.SentMassage[i] = 0;
            }
        }


        public void Process()
        {
            StartSendMsg();
        }

        private void SetCallbacks()
        {
            for (int i = 0; i < _pkg.Connections.Count; i++)
            {
                int ind = i;
                _pkg.Connections[i].On(_pkg.Job.CallbackName, (string uid, string time) =>
                {
                    var receiveTimestamp = Util.Timestamp();
                    var sendTimestamp = Convert.ToInt64(time);
                    //Util.Log($"diff time: {receiveTimestamp - sendTimestamp}");
                    Counters.CountLatency(sendTimestamp, receiveTimestamp);
                    if (ind == 0) Util.Log($"#### echocallback");
                });
            }
        }

        public void StartSendMsg()
        {
            for (int i = 0; i < _pkg.Connections.Count; i++)
            {
                int ind = i;
                _ = Task.Delay(DelayPerConnection[i]).ContinueWith(_ =>
                {
                    TimerPerConnection[ind].Start();
                });
            }
        }

        protected void SetTimers()
        {
            TimerPerConnection = new List<System.Timers.Timer>(_pkg.Job.Connections);
            DelayPerConnection = new List<TimeSpan>(_pkg.Job.Connections);

            for (int i = 0; i < _pkg.Connections.Count; i++)
            {
                var delay = StartTimeOffsetGenerator.Delay(TimeSpan.FromSeconds(_pkg.Job.Interval));
                DelayPerConnection.Add(delay);

                TimerPerConnection.Add(new System.Timers.Timer());

                var ind = i;
                var startTime = Util.Timestamp();
                TimerPerConnection[i].AutoReset = true;
                TimerPerConnection[i].Elapsed += (sender, e) =>
                {
                    // set new interval
                    TimerPerConnection[ind].Stop();
                    TimerPerConnection[ind].Interval = _pkg.Job.Interval * 1000;
                    TimerPerConnection[ind].Start();

                    if (_pkg.SentMassage[ind] >= _pkg.Job.Duration * _pkg.Job.Interval)
                    {
                        TimerPerConnection[ind].Stop();
                        return;
                    }

                    if (ind == 0)
                    {
                        Util.Log($"Sending Message: {ind}th epoach");
                    }
                    _pkg.Connections[ind].SendAsync("Echo", $"{GuidEncoder.Encode(Guid.NewGuid())}", $"{Util.Timestamp()}");
                    _pkg.SentMassage[ind]++;
                    Counters.IncreseSentMsg();

                };
            }
        }

        public void SaveCounters()
        {
            Counters.SaveCounters();
        }
    }
}
