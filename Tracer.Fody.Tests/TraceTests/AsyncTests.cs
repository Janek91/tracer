﻿using FluentAssertions;
using NUnit.Framework;
using Tracer.Fody.Tests.MockLoggers;

namespace Tracer.Fody.Tests.TraceTests
{
    //[TestFixture]
    public class AsyncTests : TestBase
    {
        [Test]
        public void Test_AsyncLoggingCallOrder()
        {
            string code = @"
                using System;
                using System.Diagnostics;
                using System.Threading.Tasks;
                using Tracer.Fody.Tests.MockLoggers;

                namespace First
                {
                    public class MyClass
                    {
                        public static void Main()
                        {
                            var myClass = new MyClass();
                            var x1 = myClass.CallMe(""Hello"", ""Hello2"", 42).Result;
                        }

                        private async Task<int> CallMe(string param, string param2, int paraInt)
                        {
                            var result = await Double(paraInt);
                            return result;
                        }

                        private async Task<int> Double(int p)
                        {
                            return await Task.Run(()=>  p * 2);
                        }
                    }
                }
            ";

            MockLogResult result = RunTest(code, new PrivateOnlyTraceLoggingFilter(), "First.MyClass::Main");
            result.Count.Should().Be(4);
            result.ElementAt(0).ShouldBeTraceEnterInto("First.MyClass::CallMe", "param", "Hello", "param2", "Hello2", "paraInt", "42");
            result.ElementAt(1).ShouldBeTraceEnterInto("First.MyClass::Double", "p", "42");
            result.ElementAt(2).ShouldBeTraceLeaveFrom("First.MyClass::Double", "84");
            result.ElementAt(3).ShouldBeTraceLeaveFrom("First.MyClass::CallMe", "84");
        }

        [Test]
        public void Test_AsyncNoReturnValue()
        {
            string code = @"
                using System;
                using System.Diagnostics;
                using System.Threading.Tasks;
                using Tracer.Fody.Tests.MockLoggers;

                namespace First
                {
                    public class MyClass
                    {
                        public static void Main()
                        {
                            var myClass = new MyClass();
                            myClass.CallMe(""Hello"", ""Hello2"", 42).Wait();
                        }

                        private async Task CallMe(string param, string param2, int paraInt)
                        {
                            var result = await Double(paraInt);
                            return;
                        }

                        private async Task<int> Double(int p)
                        {
                            return await Task.Run(()=>  p * 2);
                        }
                    }
                }
            ";

            MockLogResult result = RunTest(code, new PrivateOnlyTraceLoggingFilter(), "First.MyClass::Main");
            result.Count.Should().Be(4);
            result.ElementAt(0).ShouldBeTraceEnterInto("First.MyClass::CallMe", "param", "Hello", "param2", "Hello2", "paraInt", "42");
            result.ElementAt(1).ShouldBeTraceEnterInto("First.MyClass::Double", "p", "42");
            result.ElementAt(2).ShouldBeTraceLeaveFrom("First.MyClass::Double", "84");
            result.ElementAt(3).ShouldBeTraceLeaveFrom("First.MyClass::CallMe");
        }

        [Test]
        public void Test_AsyncMultipleCalls()
        {
            string code = @"
                using System;
                using System.Diagnostics;
                using System.Threading.Tasks;
                using Tracer.Fody.Tests.MockLoggers;

                namespace First
                {
                    public class MyClass
                    {
                        public static void Main()
                        {
                            var myClass = new MyClass();
                            var x1 = myClass.CallMe(""Hello"", ""Hello2"", 42).Result;
                            var x2 = myClass.CallMe(""Ahoy"", ""Ahoy2"", 43).Result;
                        }

                        private async Task<int> CallMe(string param, string param2, int paraInt)
                        {
                            var result = await Double(paraInt);
                            return result;
                        }

                        private async Task<int> Double(int p)
                        {
                            return await Task.Run(()=>  p * 2);
                        }
                    }
                }
            ";

            MockLogResult result = RunTest(code, new PrivateOnlyTraceLoggingFilter(), "First.MyClass::Main");
            result.Count.Should().Be(8);
        }

        [Test]
        public void Test_AsyncWithStaticOverwrites()
        {
            string code = @"
                using System;
                using System.Diagnostics;
                using System.Threading.Tasks;
                using Tracer.Fody.Tests.MockLoggers;

                namespace First
                {
                    public class MyClass
                    {
                        public static void Main()
                        {
                            var myClass = new MyClass();
                            var x1 = myClass.CallMe(""Hello"", ""Hello2"", 42).Result;
                        }

                        private async Task<int> CallMe(string param, string param2, int paraInt)
                        {
                            var result = await Double(paraInt);
                            MockLog.OuterNoParam();
                            return result;
                        }

                        private async Task<int> Double(int p)
                        {
                            MockLog.Outer(""hello"");
                            return await Task.Run(()=>  p * 2);
                        }
                    }
                }
            ";

            MockLogResult result = RunTest(code, new PrivateOnlyTraceLoggingFilter(), "First.MyClass::Main");
            result.Count.Should().Be(6);
            result.ElementAt(0).ShouldBeTraceEnterInto("First.MyClass::CallMe", "param", "Hello", "param2", "Hello2", "paraInt", "42");
            result.ElementAt(1).ShouldBeTraceEnterInto("First.MyClass::Double", "p", "42");
            result.ElementAt(2).ShouldBeLogCall("First.MyClass::Double", "MockLogOuter", "hello");
            result.ElementAt(3).ShouldBeTraceLeaveFrom("First.MyClass::Double", "84");
            result.ElementAt(4).ShouldBeLogCall("First.MyClass::CallMe", "MockLogOuterNoParam");
            result.ElementAt(5).ShouldBeTraceLeaveFrom("First.MyClass::CallMe", "84");
        }

        [Test]
        public void Test_AsyncStringReturnValue()
        {
            string code = @"
                using System;
                using System.Diagnostics;
                using System.Threading.Tasks;
                using Tracer.Fody.Tests.MockLoggers;

                namespace First
                {
                    public class MyClass
                    {
                        public static void Main()
                        {
                            var myClass = new MyClass();
                            var x1 = myClass.CallMe(""Hello"", ""Hello2"", 42).Result;
                        }

                        private async Task<string> CallMe(string param, string param2, int paraInt)
                        {
                            var result = await Double(paraInt);
                            return result;
                        }

                        private async Task<string> Double(int p)
                        {
                            return await Task.Run(()=>  p.ToString());
                        }
                    }
                }
            ";

            MockLogResult result = RunTest(code, new PrivateOnlyTraceLoggingFilter(), "First.MyClass::Main");
            result.Count.Should().Be(4);
            result.ElementAt(0).ShouldBeTraceEnterInto("First.MyClass::CallMe", "param", "Hello", "param2", "Hello2", "paraInt", "42");
            result.ElementAt(1).ShouldBeTraceEnterInto("First.MyClass::Double", "p", "42");
            result.ElementAt(2).ShouldBeTraceLeaveFrom("First.MyClass::Double", "42");
            result.ElementAt(3).ShouldBeTraceLeaveFrom("First.MyClass::CallMe", "42");
        }

        [Test]
        public void Test_AsyncUsingOtherClass()
        {
            string code = @"
                using System;
                using System.Diagnostics;
                using System.Threading.Tasks;
                using Tracer.Fody.Tests.MockLoggers;

                namespace First
                {
                    public class MyClass
                    {
                        public static void Main()
                        {
                            var myClass = new MyClass();
                            var x1 = myClass.CallMe(""Hello"", ""Hello2"", 42).Result;
                        }
                        
                        private OtherClass _otc = new OtherClass();
                        private int _num = 2;

                        private async Task<int> CallMe(string param, string param2, int paraInt)
                        {
                            var result = await _otc.Double(paraInt);
                            MockLog.OuterNoParam();
                            return result * _num;
                        }

                    }

                    public class OtherClass
                    {
                        public async Task<int> Double(int p)
                        {
                            return await DoubleInt(p);
                        }

                        private async Task<int> DoubleInt(int p)
                        {
                            MockLog.OuterNoParam();
                            return await Task.Run(()=>  p * 2);
                        }
                    }
                }
            ";

            MockLogResult result = RunTest(code, new PrivateOnlyTraceLoggingFilter(), "First.MyClass::Main");
            result.Count.Should().Be(6);
            result.ElementAt(0).ShouldBeTraceEnterInto("First.MyClass::CallMe", "param", "Hello", "param2", "Hello2", "paraInt", "42");
            result.ElementAt(1).ShouldBeTraceEnterInto("First.OtherClass::DoubleInt", "p", "42");
            result.ElementAt(2).ShouldBeLogCall("First.OtherClass::DoubleInt", "MockLogOuterNoParam");
            result.ElementAt(3).ShouldBeTraceLeaveFrom("First.OtherClass::DoubleInt", "84");
            result.ElementAt(4).ShouldBeLogCall("First.MyClass::CallMe", "MockLogOuterNoParam");
            result.ElementAt(5).ShouldBeTraceLeaveFrom("First.MyClass::CallMe", "168");
        }

        [Test]
        public void Test_GenericAsync()
        {
            string code = @"
                using System;
                using System.Diagnostics;
                using System.Threading.Tasks;
                using Tracer.Fody.Tests.MockLoggers;

                namespace First
                {
                    public class MyClass
                    {
                        public static void Main()
                        {
                            var myClass = new MyClass();
                            var x1 = myClass.CallMe<string>(""Hello"", ""Hello2"", 42).Result;
                            var x2 = myClass.CallMe<int>(12, ""Ahoy2"", 43).Result;
                        }

                        private async Task<int> CallMe<T>(T param, string param2, int paraInt)
                        {
                            var result = await Double(paraInt);
                            return result;
                        }

                        private async Task<int> Double(int p)
                        {
                            return await Task.Run(()=>  p * 2);
                        }
                    }
                }
            ";

            MockLogResult result = RunTest(code, new PrivateOnlyTraceLoggingFilter(), "First.MyClass::Main");
            result.Count.Should().Be(8);
            result.ElementAt(0).ShouldBeTraceEnterInto("First.MyClass::CallMe", "param", "Hello", "param2", "Hello2", "paraInt", "42");
            result.ElementAt(1).ShouldBeTraceEnterInto("First.MyClass::Double", "p", "42");
            result.ElementAt(2).ShouldBeTraceLeaveFrom("First.MyClass::Double", "84");
            result.ElementAt(3).ShouldBeTraceLeaveFrom("First.MyClass::CallMe", "84");
            result.ElementAt(4).ShouldBeTraceEnterInto("First.MyClass::CallMe", "param", "12", "param2", "Ahoy2", "paraInt", "43");
            result.ElementAt(5).ShouldBeTraceEnterInto("First.MyClass::Double", "p", "43");
            result.ElementAt(6).ShouldBeTraceLeaveFrom("First.MyClass::Double", "86");
            result.ElementAt(7).ShouldBeTraceLeaveFrom("First.MyClass::CallMe", "86");
        }

        [Test]
        public void Test_GenericAsyncRetval()
        {
            string code = @"
                using System;
                using System.Diagnostics;
                using System.Threading.Tasks;
                using Tracer.Fody.Tests.MockLoggers;

                namespace First
                {
                    public class MyClass
                    {
                        public static void Main()
                        {
                            var myClass = new MyClass();
                            var x1 = myClass.CallMe<string>(""Hello"", ""Hello2"", 42).Result;
                            var x2 = myClass.CallMe<int>(12, ""Ahoy2"", 43).Result;
                        }

                        private async Task<T> CallMe<T>(T param, string param2, int paraInt)
                        {
                            var result = await Double(paraInt);
                            return param;
                        }

                        public async Task<int> Double(int p)
                        {
                            return await Task.Run(()=>  p * 2);
                        }
                    }
                }
            ";

            MockLogResult result = RunTest(code, new PrivateOnlyTraceLoggingFilter(), "First.MyClass::Main");
            result.Count.Should().Be(4);
            result.ElementAt(0).ShouldBeTraceEnterInto("First.MyClass::CallMe", "param", "Hello", "param2", "Hello2", "paraInt", "42");
            result.ElementAt(1).ShouldBeTraceLeaveFrom("First.MyClass::CallMe", "Hello");
            result.ElementAt(2).ShouldBeTraceEnterInto("First.MyClass::CallMe", "param", "12", "param2", "Ahoy2", "paraInt", "43");
            result.ElementAt(3).ShouldBeTraceLeaveFrom("First.MyClass::CallMe", "12");
        }

        [Test]
        public void Test_AsyncLoggingCallOrderWithException()
        {
            string code = @"
                using System;
                using System.Diagnostics;
                using System.Threading.Tasks;
                using Tracer.Fody.Tests.MockLoggers;

                namespace First
                {
                    public class MyClass
                    {
                        public static void Main()
                        {
                            try {
                            var myClass = new MyClass();
                            var x1 = myClass.CallMe(""Hello"", ""Hello2"", 42).Result;
                            } catch {}
                        }

                        private async Task<int> CallMe(string param, string param2, int paraInt)
                        {
                            var result = await Double(paraInt);
                            return result;
                        }

                        private async Task<int> Double(int p)
                        {
                            return await Task.Run(() => Calculate());
                        }

                        public int Calculate()
                        {
                            throw new ApplicationException(""Err""); return 1;
                        }
                    }
                }
            ";

            MockLogResult result = RunTest(code, new PrivateOnlyTraceLoggingFilter(), "First.MyClass::Main");
            result.Count.Should().Be(6);
            result.ElementAt(0).ShouldBeTraceEnterInto("First.MyClass::CallMe", "param", "Hello", "param2", "Hello2", "paraInt", "42");
            result.ElementAt(1).ShouldBeTraceEnterInto("First.MyClass::Double", "p", "42");
            result.ElementAt(4).ShouldBeTraceLeaveWithExceptionFrom("First.MyClass::Double", "Err");
            result.ElementAt(5).ShouldBeTraceLeaveWithExceptionFrom("First.MyClass::CallMe", "Err");
        }
    }
}
