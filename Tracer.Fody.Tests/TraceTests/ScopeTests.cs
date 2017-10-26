﻿using FluentAssertions;
using NUnit.Framework;
using Tracer.Fody.Tests.MockLoggers;

namespace Tracer.Fody.Tests.TraceTests
{
    [TestFixture]
    public class ScopeTests : TestBase
    {
        [Test]
        public void Test_Returning_From_Try()
        {
            string code = @"
                using System;
                using System.Diagnostics;

                namespace First
                {
                    public class MyClass
                    {
                        public static void Main()
                        {
                            CallMe(""Hello"", ""Hello2"", 42);
                        }

                        private static string CallMe(string param, string param2, int paraInt)
                        {
                            try
                            {
                                return ""response"" + paraInt.ToString();
                            }
                            catch (Exception ex)
                            {
                                return ""exception"" + paraInt.ToString();
                            }
                        }
                    }
                }
            ";

            MockLogResult result = RunTest(code, new PrivateOnlyTraceLoggingFilter(), "First.MyClass::Main");
            result.Count.Should().Be(2);
            result.ElementAt(0).ShouldBeTraceEnterInto("First.MyClass::CallMe", "param", "Hello", "param2", "Hello2", "paraInt", "42");
            result.ElementAt(1).ShouldBeTraceLeaveFrom("First.MyClass::CallMe", "response42");
        }

        [Test]
        public void Test_Returning_From_Catch()
        {
            string code = @"
                using System;
                using System.Diagnostics;

                namespace First
                {
                    public class MyClass
                    {
                        public static void Main()
                        {
                            CallMe(""Hello"", ""Hello2"", 42);
                        }

                        private static string CallMe(string param, string param2, int paraInt)
                        {
                            try
                            {
                                throw new Exception();
                                return ""response"" + paraInt.ToString();
                            }
                            catch (Exception ex)
                            {
                                return ""exception"" + paraInt.ToString();
                            }
                        }
                    }
                }
            ";

            MockLogResult result = RunTest(code, new PrivateOnlyTraceLoggingFilter(), "First.MyClass::Main");
            result.Count.Should().Be(2);
            result.ElementAt(0).ShouldBeTraceEnterInto("First.MyClass::CallMe", "param", "Hello", "param2", "Hello2", "paraInt", "42");
            result.ElementAt(1).ShouldBeTraceLeaveFrom("First.MyClass::CallMe", "exception42");
        }

        [Test]
        public void Test_Returning_From_NestedScope()
        {
            string code = @"
                using System;
                using System.Diagnostics;

                namespace First
                {
                    public class MyClass
                    {
                        public static void Main()
                        {
                            CallMe(""Hello"", ""Hello2"", 42);
                        }

                        private static string CallMe(string param, string param2, int paraInt)
                        {
                            for (int i=0; i < 1; i++)
                            {
                                return ""response"" + paraInt.ToString();
                            }
                            return String.Empty;
                        }
                    }
                }
            ";

            MockLogResult result = RunTest(code, new PrivateOnlyTraceLoggingFilter(), "First.MyClass::Main");
            result.Count.Should().Be(2);
            result.ElementAt(0).ShouldBeTraceEnterInto("First.MyClass::CallMe", "param", "Hello", "param2", "Hello2", "paraInt", "42");
            result.ElementAt(1).ShouldBeTraceLeaveFrom("First.MyClass::CallMe", "response42");
        }
    }
}
