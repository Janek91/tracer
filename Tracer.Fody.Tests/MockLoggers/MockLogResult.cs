using System;
using System.Collections.Generic;

namespace Tracer.Fody.Tests.MockLoggers
{
    [Serializable]
    public class MockLogResult
    {
        private readonly List<MockCallInfo> _calls;
        private readonly bool _areReturnValuesOk;

        public MockLogResult(List<MockCallInfo> calls, bool areReturnValuesOk)
        {
            _calls = calls;
            _areReturnValuesOk = areReturnValuesOk;
        }

        public int Count
        {
            get { return _calls.Count; }
        }

        public MockCallInfo ElementAt(int idx)
        {
            return _calls[idx];
        }
    }
}
