using System;
using System.Collections.Generic;
using System.Text;

namespace Bench.Client.Allocators
{
    public interface IAllocator
    {
        Dictionary<string, Dictionary<string, int>> Allocate(List<string> slaves, int totalConn, Dictionary<string, int> criteria);
    }
}
