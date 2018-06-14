using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bench.Server.Worker.Operations
{
    public interface IOperation
    {
        void Do(WorkerToolkit tk);
    }
}
