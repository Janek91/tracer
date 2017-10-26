﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Tracer.Log4Net;
using TracerAttributes;

namespace TestApplication
{
    public class MyApplication
    {
        private static string x = "abc";

        private string _testProperty;

        public string TestProperty
        {
            get { return _testProperty; }
            set { _testProperty = value; }
        }

        public void Run()
        {
            Thread.Sleep(500);

            Enumerable(new[] { "hi", "there", "!" });

            NotTracedNamespace.NotTraced.SomeMethod("Hello");

            ExceptionTests();

            AsyncTests();

            PropertyTests();

            GenericMethodTests();
            GenericClassTests();

            StaticLogPropertyRewrites();
            StaticLogRewrites();

            OutParamLogs();

            StructParamClass structParams = new StructParamClass();
            structParams.RunStructs();

            Write(Add(21, 22));
            Write(Add(100, 1));

            PerfComp perfComp = new PerfComp();
            perfComp.SpeedTest();
        }

        public void AsyncTests()
        {
            MyAsyncClass x = new MyAsyncClass();
            x.Run();
        }

        public void PropertyTests()
        {
            TestProperty = "Hello";
            string val = TestProperty;
        }

        public void StaticLogPropertyRewrites()
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("Debug is on");
            }
            if (Log.IsErrorEnabled)
            {
                Log.Error("Error is on");
            }
            if (Log.IsFatalEnabled)
            {
                Log.Fatal("Fatal is on");
            }
            if (Log.IsInfoEnabled)
            {
                Log.Info("Info is on");
            }
            if (Log.IsWarnEnabled)
            {
                Log.Warn("Warn is on");
            }
        }

        public void OutParamLogs()
        {
            OutParamClass op = new OutParamClass();
            string outString;
            op.SetParamString("in", out outString);
            int outInt;
            op.SetParamInt("42", out outInt);
        }

        public void StaticLogRewrites()
        {
            Log.Debug(new MyClass());
            Log.Debug("Hello");
            Log.Debug("Hello", new ApplicationException("Failure."));
            Log.DebugFormat("A{0}", 1);
            Log.DebugFormat("A{0}-B{1}", 1, 2);
            Log.DebugFormat("A{0}-B{1}-C{2}", 1, 2, 3);
            Log.DebugFormat("A{0}-B{1}-C{2}-D{3}", 1, 2, 3, 4);
            Log.DebugFormat(new CultureInfo("en-us"), "A{0}-B{1}-C{2}-D{3}", 1, 2, 3, 4);

            Log.Info(new MyClass());
            Log.Info("Hello");
            Log.Info("Hello", new ApplicationException("Failure."));
            Log.InfoFormat("A{0}", 1);
            Log.InfoFormat("A{0}-B{1}", 1, 2);
            Log.InfoFormat("A{0}-B{1}-C{2}", 1, 2, 3);
            Log.InfoFormat("A{0}-B{1}-C{2}-D{3}", 1, 2, 3, 4);
            Log.InfoFormat(new CultureInfo("en-us"), "A{0}-B{1}-C{2}-D{3}", 1, 2, 3, 4);

            Log.Warn(new MyClass());
            Log.Warn("Hello");
            Log.Warn("Hello", new ApplicationException("Failure."));
            Log.WarnFormat("A{0}", 1);
            Log.WarnFormat("A{0}-B{1}", 1, 2);
            Log.WarnFormat("A{0}-B{1}-C{2}", 1, 2, 3);
            Log.WarnFormat("A{0}-B{1}-C{2}-D{3}", 1, 2, 3, 4);
            Log.WarnFormat(new CultureInfo("en-us"), "A{0}-B{1}-C{2}-D{3}", 1, 2, 3, 4);

            Log.Error(new MyClass());
            Log.Error("Hello");
            Log.Error("Hello", new ApplicationException("Failure."));
            Log.ErrorFormat("A{0}", 1);
            Log.ErrorFormat("A{0}-B{1}", 1, 2);
            Log.ErrorFormat("A{0}-B{1}-C{2}", 1, 2, 3);
            Log.ErrorFormat("A{0}-B{1}-C{2}-D{3}", 1, 2, 3, 4);
            Log.ErrorFormat(new CultureInfo("en-us"), "A{0}-B{1}-C{2}-D{3}", 1, 2, 3, 4);

            Log.Fatal(new MyClass());
            Log.Fatal("Hello");
            Log.Fatal("Hello", new ApplicationException("Failure."));
            Log.FatalFormat("A{0}", 1);
            Log.FatalFormat("A{0}-B{1}", 1, 2);
            Log.FatalFormat("A{0}-B{1}-C{2}", 1, 2, 3);
            Log.FatalFormat("A{0}-B{1}-C{2}-D{3}", 1, 2, 3, 4);
            Log.FatalFormat(new CultureInfo("en-us"), "A{0}-B{1}-C{2}-D{3}", 1, 2, 3, 4);

            //closure
            int idx = 1;
            Action act = () =>
            {
                idx++;
                Log.Debug("Closure log");
            };

            act();

            //lambda w/out closure
            Action act2 = () =>
            {
                Log.Debug("No closure log");
            };

            act2();
        }

        [TraceOn]
        private void GenericMethodTests()
        {
            Write(42);
            Write("Hello");
            Write(0.5);
        }

        private void GenericClassTests()
        {
            GenericClass<int> intGen = new GenericClass<int>();
            Write(intGen.GetDefault(42));

            GenericClass<string> stringGen = new GenericClass<string>();
            Write(stringGen.GetDefault("Hello"));
        }

        private void ExceptionTests()
        {
            ThrowException(1);
            try
            {
                ThrowException(0);
            }
            catch { }
            ThrowExceptionOuter(1);
            try
            {
                ThrowExceptionOuter(0);
            }
            catch { }
        }

        public int ThrowExceptionOuter(int inp)
        {
            return ThrowException(inp);
        }

        public int ThrowException(int inp)
        {
            return 1 / inp;
        }

        public void Write<T>(T input)
        {
            Console.WriteLine(input);
        }

        public int Add(int i1, int i2)
        {
            return i1 + i2;
        }

        public int Enumerable(IEnumerable<string> inp)
        {
            return inp.Count();
        }

        private class MyClass
        {
            public override string ToString()
            {
                return "ToStringOfMyClass";
            }
        }
    }
}
