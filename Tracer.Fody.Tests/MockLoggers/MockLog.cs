﻿using System;

namespace Tracer.Fody.Tests.MockLoggers
{
    public static class MockLog
    {
        //When rewriting calls to this method they will be redirected to the adapter's MockLogOuter method.
        //All adapter methods will have and additional string methodInfo first parameter so the corresponding method will be
        //public void MockLogOuter(string methodInfo string message)
        public static void Outer(string message)
        {}

        public static void OuterNoParam()
        {}

        public static void Outer(string message, int i)
        {}

        public static void Exception(string message, Exception exception)
        {}

        //When rewriting properties static property get will be rewritten to instance call
        public static bool IsEnabled { get; }

        public static bool ReadWrite { get; set; }

        public static void GenericOuter<T>()
        {}

        public static void GenericOuter<T>(T message)
        {}

        public static void GenericOuter<T, K>(K num1, T message, K num2)
        {}
    }
}
