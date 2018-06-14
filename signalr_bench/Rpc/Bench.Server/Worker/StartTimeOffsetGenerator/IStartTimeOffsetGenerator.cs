using System;
using System.Collections.Generic;
using System.Text;

namespace Bench.Server.Worker.StartTimeOffsetGenerator
{
    public interface IStartTimeOffsetGenerator
    {
        TimeSpan Delay(TimeSpan duration);
        
    }
}
