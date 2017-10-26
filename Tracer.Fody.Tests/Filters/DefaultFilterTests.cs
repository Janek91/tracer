using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Mono.Cecil;
using NUnit.Framework;
using Tracer.Fody.Filters;
using Tracer.Fody.Weavers;

namespace Tracer.Fody.Tests.Filters
{
    [TestFixture]
    public class DefaultFilterTests : TestBase
    {
        [Test]
        public void Parse_MostBasicConfiguration()
        {
            List<AssemblyLevelTraceDefinition> result = DefaultFilter.ParseConfig(XElement.Parse(@"<root>
                <TraceOn class=""public"" method =""public"" />
            </root>").Descendants()).ToList();

            result.Count.Should().Be(1);
            result[0].Should().BeOfType<AssemblyLevelTraceOnDefinition>();
        }

        [Test]
        public void Parse_MultiElement_Configuration()
        {
            List<AssemblyLevelTraceDefinition> result = DefaultFilter.ParseConfig(XElement.Parse(@"<root>
                <TraceOn class=""public"" method =""public"" />
                <TraceOn namespace=""rootnamespace"" class=""public"" method =""public"" />
                <NoTrace namespace=""rootnamespace.other"" />
            </root>").Descendants()).ToList();

            result.Count.Should().Be(3);
            result[0].Should().BeOfType<AssemblyLevelTraceOnDefinition>();
            result[1].Should().BeOfType<AssemblyLevelTraceOnDefinition>();
            result[2].Should().BeOfType<AssemblyLevelNoTraceDefinition>();
        }

        [Test]
        public void Creation_MultiElementConfig()
        {
            DefaultFilter filter = new DefaultFilter(XElement.Parse(@"<root>
                <TraceOn class=""public"" method =""public"" />
                <TraceOn namespace=""rootnamespace"" class=""public"" method =""public"" />
                <NoTrace namespace=""rootnamespace.other"" />
            </root>").Descendants());

            string code = @"
                using TracerAttributes;

                namespace rootnamespace
                {
                    public class MyClass
                    {
                        public void PublicMethod()
                        {}

                        internal void InternalMethod()
                        {}

                        protected void ProtectedMethod()
                        {}

                        private void PrivateMethod()
                        {}
                    }
                }

                namespace rootnamespace.other
                {
                    public class OtherClass
                    {
                        public void OtherPublicMethod()
                        {}
                    }
                }

                namespace rootnamespace.another
                {
                    public class AnotherClass
                    {
                        public void AnotherPublicMethod()
                        {}
                    }
                }
            ";

            MethodDefinition publicMethodDef = GetMethodDefinition(code, "PublicMethod");
            MethodDefinition internalMethodDef = GetMethodDefinition(code, "InternalMethod");

            filter.ShouldAddTrace(publicMethodDef).Should().BeTrue();
            filter.ShouldAddTrace(internalMethodDef).Should().BeFalse();

            MethodDefinition otherPublicMethodDef = GetMethodDefinition(code, "OtherPublicMethod");
            filter.ShouldAddTrace(otherPublicMethodDef).Should().BeFalse();

            MethodDefinition anotherPublicMethodDef = GetMethodDefinition(code, "AnotherPublicMethod");
            filter.ShouldAddTrace(anotherPublicMethodDef).Should().BeTrue();
        }


        [Test]
        public void AssemblyLevelSpecification_PublicClass_PublicFilter()
        {
            string code = @"
                using TracerAttributes;

                namespace First
                {
                    public class MyClass
                    {
                        public void PublicMethod()
                        {}

                        internal void InternalMethod()
                        {}

                        protected void ProtectedMethod()
                        {}

                        private void PrivateMethod()
                        {}
                    }
                }
            ";

            MethodDefinition publicMethodDef = GetMethodDefinition(code, "PublicMethod");
            MethodDefinition internalMethodDef = GetMethodDefinition(code, "InternalMethod");
            MethodDefinition protectedMethodDef = GetMethodDefinition(code, "ProtectedMethod");
            MethodDefinition privateMethodDef = GetMethodDefinition(code, "PrivateMethod");
            ITraceLoggingFilter filter = GetDefaultFilter(TraceTargetVisibility.Public, TraceTargetVisibility.Public);
            filter.ShouldAddTrace(publicMethodDef).Should().BeTrue("public");
            filter.ShouldAddTrace(internalMethodDef).Should().BeFalse("internal");
            filter.ShouldAddTrace(protectedMethodDef).Should().BeFalse("protected");
            filter.ShouldAddTrace(privateMethodDef).Should().BeFalse("private");
        }

        [Test]
        public void AssemblyLevelSpecification_PublicClass_AllFilter()
        {
            string code = @"
                using TracerAttributes;

                namespace First
                {
                    public class MyClass
                    {
                        public void PublicMethod()
                        {}

                        internal void InternalMethod()
                        {}

                        protected void ProtectedMethod()
                        {}

                        private void PrivateMethod()
                        {}
                    }
                }
            ";

            MethodDefinition publicMethodDef = GetMethodDefinition(code, "PublicMethod");
            MethodDefinition internalMethodDef = GetMethodDefinition(code, "InternalMethod");
            MethodDefinition protectedMethodDef = GetMethodDefinition(code, "ProtectedMethod");
            MethodDefinition privateMethodDef = GetMethodDefinition(code, "PrivateMethod");
            ITraceLoggingFilter filter = GetDefaultFilter(TraceTargetVisibility.Public, TraceTargetVisibility.All);
            filter.ShouldAddTrace(publicMethodDef).Should().BeTrue("public");
            filter.ShouldAddTrace(internalMethodDef).Should().BeTrue("internal");
            filter.ShouldAddTrace(protectedMethodDef).Should().BeTrue("protected");
            filter.ShouldAddTrace(privateMethodDef).Should().BeTrue("private");
        }

        [Test]
        public void AssemblyLevelSpecification_InternalClass_PublicFilter()
        {
            string code = @"
                using TracerAttributes;

                namespace First
                {
                    internal class MyClass
                    {
                        public void PublicMethod()
                        {}

                        internal void InternalMethod()
                        {}

                        protected void ProtectedMethod()
                        {}

                        private void PrivateMethod()
                        {}
                    }
                }
            ";

            MethodDefinition publicMethodDef = GetMethodDefinition(code, "PublicMethod");
            MethodDefinition internalMethodDef = GetMethodDefinition(code, "InternalMethod");
            MethodDefinition protectedMethodDef = GetMethodDefinition(code, "ProtectedMethod");
            MethodDefinition privateMethodDef = GetMethodDefinition(code, "PrivateMethod");
            ITraceLoggingFilter filter = GetDefaultFilter(TraceTargetVisibility.Public, TraceTargetVisibility.All);
            filter.ShouldAddTrace(publicMethodDef).Should().BeFalse("public");
            filter.ShouldAddTrace(internalMethodDef).Should().BeFalse("internal");
            filter.ShouldAddTrace(protectedMethodDef).Should().BeFalse("protected");
            filter.ShouldAddTrace(privateMethodDef).Should().BeFalse("private");
        }

        [Test]
        public void AssemblyLevelSpecification_InternalClass_AllFilter()
        {
            string code = @"
                using TracerAttributes;

                namespace First
                {
                    internal class MyClass
                    {
                        public void PublicMethod()
                        {}

                        internal void InternalMethod()
                        {}

                        protected void ProtectedMethod()
                        {}

                        private void PrivateMethod()
                        {}
                    }
                }
            ";

            MethodDefinition publicMethodDef = GetMethodDefinition(code, "PublicMethod");
            MethodDefinition internalMethodDef = GetMethodDefinition(code, "InternalMethod");
            MethodDefinition protectedMethodDef = GetMethodDefinition(code, "ProtectedMethod");
            MethodDefinition privateMethodDef = GetMethodDefinition(code, "PrivateMethod");
            ITraceLoggingFilter filter = GetDefaultFilter(TraceTargetVisibility.All, TraceTargetVisibility.ProtectedOrMoreVisible);
            filter.ShouldAddTrace(publicMethodDef).Should().BeTrue("public");
            filter.ShouldAddTrace(internalMethodDef).Should().BeTrue("internal");
            filter.ShouldAddTrace(protectedMethodDef).Should().BeTrue("protected");
            filter.ShouldAddTrace(privateMethodDef).Should().BeFalse("private");
        }

        [Test]
        public void MethodLevelTraceOn_Overrides_AssemblyLevel()
        {
            string code = @"
                using TracerAttributes;

                namespace First
                {
                    public class MyClass
                    {
                        [TraceOn]
                        private void MyMethod()
                        {}
                    }
                }
            ";

            MethodDefinition methodDef = GetMethodDefinition(code, "MyMethod");
            ITraceLoggingFilter filter = GetDefaultFilter(TraceTargetVisibility.Public, TraceTargetVisibility.Public);
            filter.ShouldAddTrace(methodDef).Should().BeTrue();
        }

        [Test]
        public void MethodLevelTraceOn_Overrides_ClassLevel()
        {
            string code = @"
                using TracerAttributes;

                namespace First
                {
                    [NoTrace]
                    public class MyClass
                    {
                        [TraceOn]
                        private void MyMethod()
                        {}
                    }
                }
            ";

            MethodDefinition methodDef = GetMethodDefinition(code, "MyMethod");
            ITraceLoggingFilter filter = GetDefaultFilter(TraceTargetVisibility.Public, TraceTargetVisibility.Public);
            filter.ShouldAddTrace(methodDef).Should().BeTrue();
        }

        [Test]
        public void MethodLevelNoTrace_Overrides_AssemblyLevel()
        {
            string code = @"
                using TracerAttributes;

                namespace First
                {
                    public class MyClass
                    {
                        [NoTrace]
                        public void MyMethod()
                        {}
                    }
                }
            ";

            MethodDefinition methodDef = GetMethodDefinition(code, "MyMethod");
            ITraceLoggingFilter filter = GetDefaultFilter(TraceTargetVisibility.Public, TraceTargetVisibility.Public);
            filter.ShouldAddTrace(methodDef).Should().BeFalse();
        }

        [Test]
        public void ClassLevelTraceOn_Overrides_AssemblyLevel_PrivateLevel()
        {
            string code = @"
                using TracerAttributes;

                namespace First
                {
                    [TraceOn(Target=TraceTarget.Private)]
                    public class MyClass
                    {
                        public void PublicMethod()
                        {}

                        internal void InternalMethod()
                        {}

                        protected void ProtectedMethod()
                        {}

                        private void PrivateMethod()
                        {}
                    }
                }
            ";

            MethodDefinition publicMethodDef = GetMethodDefinition(code, "PublicMethod");
            MethodDefinition internalMethodDef = GetMethodDefinition(code, "InternalMethod");
            MethodDefinition protectedMethodDef = GetMethodDefinition(code, "ProtectedMethod");
            MethodDefinition privateMethodDef = GetMethodDefinition(code, "PrivateMethod");
            ITraceLoggingFilter filter = GetDefaultFilter(TraceTargetVisibility.Public, TraceTargetVisibility.Public);
            filter.ShouldAddTrace(publicMethodDef).Should().BeTrue("public");
            filter.ShouldAddTrace(internalMethodDef).Should().BeTrue("internal");
            filter.ShouldAddTrace(protectedMethodDef).Should().BeTrue("protected");
            filter.ShouldAddTrace(privateMethodDef).Should().BeTrue("private");
        }

        [Test]
        public void ClassLevelTraceOn_Overrides_AssemblyLevel_InternalLevel()
        {
            string code = @"
                using TracerAttributes;

                namespace First
                {
                    [TraceOn(TraceTarget.Internal)]
                    public class MyClass
                    {
                        public void PublicMethod()
                        {}

                        internal void InternalMethod()
                        {}

                        protected void ProtectedMethod()
                        {}

                        private void PrivateMethod()
                        {}
                    }
                }
            ";

            MethodDefinition publicMethodDef = GetMethodDefinition(code, "PublicMethod");
            MethodDefinition internalMethodDef = GetMethodDefinition(code, "InternalMethod");
            MethodDefinition protectedMethodDef = GetMethodDefinition(code, "ProtectedMethod");
            MethodDefinition privateMethodDef = GetMethodDefinition(code, "PrivateMethod");
            ITraceLoggingFilter filter = GetDefaultFilter(TraceTargetVisibility.Public, TraceTargetVisibility.All);
            filter.ShouldAddTrace(publicMethodDef).Should().BeTrue("public");
            filter.ShouldAddTrace(internalMethodDef).Should().BeTrue("internal");
            filter.ShouldAddTrace(protectedMethodDef).Should().BeFalse("protected");
            filter.ShouldAddTrace(privateMethodDef).Should().BeFalse("private");
        }

        [Test]
        public void ClassLevelTraceOn_Overrides_AssemblyLevel_PublicLevel()
        {
            string code = @"
                using TracerAttributes;

                namespace First
                {
                    [TraceOn(TraceTarget.Public)]
                    public class MyClass
                    {
                        public void PublicMethod()
                        {}

                        internal void InternalMethod()
                        {}

                        protected void ProtectedMethod()
                        {}

                        private void PrivateMethod()
                        {}
                    }
                }
            ";

            MethodDefinition publicMethodDef = GetMethodDefinition(code, "PublicMethod");
            MethodDefinition internalMethodDef = GetMethodDefinition(code, "InternalMethod");
            MethodDefinition protectedMethodDef = GetMethodDefinition(code, "ProtectedMethod");
            MethodDefinition privateMethodDef = GetMethodDefinition(code, "PrivateMethod");
            ITraceLoggingFilter filter = GetDefaultFilter(TraceTargetVisibility.Public, TraceTargetVisibility.All);
            filter.ShouldAddTrace(publicMethodDef).Should().BeTrue("public");
            filter.ShouldAddTrace(internalMethodDef).Should().BeFalse("internal");
            filter.ShouldAddTrace(protectedMethodDef).Should().BeFalse("protected");
            filter.ShouldAddTrace(privateMethodDef).Should().BeFalse("private");
        }

        [Test]
        public void ClassLevelNoTrace_Overrides_AssemblyLevel()
        {
            string code = @"
                using TracerAttributes;

                namespace First
                {
                    [NoTrace]
                    public class MyClass
                    {
                        public void PublicMethod()
                        {}

                        internal void InternalMethod()
                        {}

                        protected void ProtectedMethod()
                        {}

                        private void PrivateMethod()
                        {}
                    }
                }
            ";

            MethodDefinition publicMethodDef = GetMethodDefinition(code, "PublicMethod");
            MethodDefinition internalMethodDef = GetMethodDefinition(code, "InternalMethod");
            MethodDefinition protectedMethodDef = GetMethodDefinition(code, "ProtectedMethod");
            MethodDefinition privateMethodDef = GetMethodDefinition(code, "PrivateMethod");
            ITraceLoggingFilter filter = GetDefaultFilter(TraceTargetVisibility.Public, TraceTargetVisibility.All);
            filter.ShouldAddTrace(publicMethodDef).Should().BeFalse("public");
            filter.ShouldAddTrace(internalMethodDef).Should().BeFalse("internal");
            filter.ShouldAddTrace(protectedMethodDef).Should().BeFalse("protected");
            filter.ShouldAddTrace(privateMethodDef).Should().BeFalse("private");
        }

        [Test]
        public void NestedClassLevelNoTrace_Overrides_AssemblyLevel()
        {
            string code = @"
                using TracerAttributes;

                namespace First
                {
                    [NoTrace]
                    public class MyClass
                    {
                        public class InnerClass
                        {
                            public void PublicMethod()
                            {}

                            public void PublicMethod2()
                            {}
                        }
                    }
                }
            ";

            MethodDefinition publicMethodDef = GetMethodDefinition(code, "PublicMethod");
            MethodDefinition publicMethodDef2 = GetMethodDefinition(code, "PublicMethod");
            ITraceLoggingFilter filter = GetDefaultFilter(TraceTargetVisibility.Public, TraceTargetVisibility.All);
            filter.ShouldAddTrace(publicMethodDef).Should().BeFalse("public");
            filter.ShouldAddTrace(publicMethodDef2).Should().BeFalse("public");
        }

        [Test]
        public void NestedClassLevelTraceOn_Overrides_AssemblyLevel()
        {
            string code = @"
                using TracerAttributes;

                namespace First
                {
                    [TraceOn(TraceTarget.Protected)]
                    public class MyClass
                    {
                        public class InnerClass
                        {
                            public void PublicMethod()
                            {}

                            internal void InternalMethod()
                            {}

                            protected void ProtectedMethod()
                            {}

                            private void PrivateMethod()
                            {}
                        }
                    }
                }
            ";

            MethodDefinition publicMethodDef = GetMethodDefinition(code, "PublicMethod");
            MethodDefinition internalMethodDef = GetMethodDefinition(code, "InternalMethod");
            MethodDefinition protectedMethodDef = GetMethodDefinition(code, "ProtectedMethod");
            MethodDefinition privateMethodDef = GetMethodDefinition(code, "PrivateMethod");
            ITraceLoggingFilter filter = GetDefaultFilter(TraceTargetVisibility.Public, TraceTargetVisibility.All);
            filter.ShouldAddTrace(publicMethodDef).Should().BeTrue("public");
            filter.ShouldAddTrace(internalMethodDef).Should().BeTrue("internal");
            filter.ShouldAddTrace(protectedMethodDef).Should().BeTrue("protected");
            filter.ShouldAddTrace(privateMethodDef).Should().BeFalse("private");
        }

        [Test]
        public void ParseConfig_DefaultConfig_Parsed()
        {
            XElement input = new XElement("Tracer",
                new XElement("TraceOn", new XAttribute("class", "public"), new XAttribute("method", "public"))
                );

            List<AssemblyLevelTraceDefinition> parseResult = DefaultFilter.ParseConfig(input.Descendants()).ToList();
            parseResult.Count.Should().Be(1);
            parseResult[0].Should().BeOfType<AssemblyLevelTraceOnDefinition>();
            ((AssemblyLevelTraceOnDefinition)parseResult[0]).TargetClass.Should().Be(TraceTargetVisibility.Public);
            ((AssemblyLevelTraceOnDefinition)parseResult[0]).TargetMethod.Should().Be(TraceTargetVisibility.Public);
        }

        [Test]
        public void ParseConfig_PrivateConfig_Parsed()
        {
            XElement input = new XElement("Tracer",
                new XElement("TraceOn", new XAttribute("class", "internal"), new XAttribute("method", "private"))
                );

            List<AssemblyLevelTraceDefinition> parseResult = DefaultFilter.ParseConfig(input.Descendants()).ToList();
            parseResult.Count.Should().Be(1);
            parseResult[0].Should().BeOfType<AssemblyLevelTraceOnDefinition>();
            ((AssemblyLevelTraceOnDefinition)parseResult[0]).TargetClass.Should().Be(TraceTargetVisibility.InternalOrMoreVisible);
            ((AssemblyLevelTraceOnDefinition)parseResult[0]).TargetMethod.Should().Be(TraceTargetVisibility.All);
        }

        [Test]
        public void ParseConfig_MissingAttribute_Throws()
        {
            XElement input = new XElement("Tracer",
                new XElement("TraceOn", new XAttribute("method", "private"))
                );

            Action runParse = () => DefaultFilter.ParseConfig(input.Descendants());
            runParse.ShouldThrow<ApplicationException>();
        }

        [Test]
        public void ParseConfig_WrongAttributeValue_Throws()
        {
            XElement input = new XElement("Tracer",
                new XElement("TraceOn", new XAttribute("class", "wrongvalue"), new XAttribute("method", "private"))
                );

            Action runParse = () => DefaultFilter.ParseConfig(input.Descendants());
            runParse.ShouldThrow<ApplicationException>();
        }


        private ITraceLoggingFilter GetDefaultFilter(TraceTargetVisibility classTarget,
            TraceTargetVisibility methodTarget)
        {
            AssemblyLevelTraceOnDefinition[] config = new[] {new AssemblyLevelTraceOnDefinition(NamespaceScope.All, classTarget, methodTarget)};
            return new DefaultFilter(config);
        }
    }
}
