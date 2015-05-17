﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tracer.Fody.Tests.MockLoggers
{
    public interface IMockLogAdapter
    {
        void TraceEnter(string methodInfo, string[] paramNames, object[] paramValues);
        void TraceLeave(string methodInfo, long numberOfTicks, string[] paramNames, object[] paramValues);

        void MockLogOuterNoParam(string methodInfo);
        void MockLogOuter(string methodInfo, string message);
        void MockLogOuter(string methodInfo, string message, int i);
    }
}
