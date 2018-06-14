﻿using Bench.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bench.Server.Worker.Operations
{
    class BaseOp
    {
        public BaseOp()
        {
            var opName = GetType().Name;
            Util.Log(opName.Substring(0, opName.Length - 2) + " Operation is Created.");
        }
    }
}
