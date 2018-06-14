﻿using Bench.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bench.Server.Worker.Operations
{
    class OperationFactory
    {
        public static Tuple<object, Type> CreateOp(string opName)
        {
            opName += "Op";
            var myType = typeof(OperationFactory);
            var nspace = myType.Namespace;

            var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                    where t.IsClass && t.Namespace == nspace
                    select t;

            object obj = new object();
            Type type = typeof(object);

            q.ToList().ForEach(t =>
            {
                if (string.Equals(t.Name, opName, StringComparison.OrdinalIgnoreCase))
                {
                    obj = Activator.CreateInstance(t);
                    type = t;
                }
            });

            if (type == typeof(object))
            {
                Util.Log($"cannot find Operation.");
            }

            return new Tuple<object, Type>(obj, type);
        }

    }
}
