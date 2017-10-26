using System;
using System.Collections.Generic;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using Tracer.Fody.Filters;

namespace Tracer.Fody.Tests.Filters
{
    [TestFixture]
    public class AssemblyLevelTraceDefinitionTests
    {
        [Test]
        public void Parse_TraceOn_Missing_ClassAttribute()
        {
            Action action = () => AssemblyLevelTraceOnDefinition.ParseFromConfig(XElement.Parse("<TraceOn />"));
            action.ShouldThrow<ApplicationException>().And.Message.Should().Contain("class");
        }

        [Test]
        public void Parse_TraceOn_Missing_MethodAttribute()
        {
            Action action = () => AssemblyLevelTraceOnDefinition.ParseFromConfig(XElement.Parse("<TraceOn class=\"public\" />"));
            action.ShouldThrow<ApplicationException>().And.Message.Should().Contain("method");
        }

        [Test]
        public void Parse_TraceOn_MethodAttribute_WrongValue()
        {
            Action action = () => AssemblyLevelTraceOnDefinition.ParseFromConfig(XElement.Parse("<TraceOn class=\"public\" method=\"wrng\" />"));
            action.ShouldThrow<ApplicationException>().And.Message.Should().Contain("method").And.Contain("wrng");
        }

        [Test]
        public void Parse_TraceOn_MethodAttribute_Values()
        {
            AssemblyLevelTraceOnDefinition result = AssemblyLevelTraceOnDefinition.ParseFromConfig(XElement.Parse("<TraceOn class=\"public\" method=\"public\" />"));
            result.TargetClass.Should().Be(TraceTargetVisibility.Public);
            result.TargetMethod.Should().Be(TraceTargetVisibility.Public);
            result = AssemblyLevelTraceOnDefinition.ParseFromConfig(XElement.Parse("<TraceOn class=\"private\" method=\"private\" />"));
            result.TargetClass.Should().Be(TraceTargetVisibility.All);
            result.TargetMethod.Should().Be(TraceTargetVisibility.All);
        }

        [Test]
        public void Parse_TraceOn_NamespaceAttribute_Values()
        {
            AssemblyLevelTraceOnDefinition result = AssemblyLevelTraceOnDefinition.ParseFromConfig(XElement.Parse("<TraceOn namespace=\"rootnamespace\" class=\"public\" method=\"public\" />"));
            result.NamespaceScope.IsMatching("rootnamespace").Should().BeTrue();
            result = AssemblyLevelTraceOnDefinition.ParseFromConfig(XElement.Parse("<TraceOn namespace=\"rootnamespace.other\" class=\"public\" method=\"public\" />"));
            result.NamespaceScope.IsMatching("rootnamespace.other").Should().BeTrue();
            result = AssemblyLevelTraceOnDefinition.ParseFromConfig(XElement.Parse("<TraceOn namespace=\"rootnamespace.*\" class=\"public\" method=\"public\" />"));
            result.NamespaceScope.IsMatching("rootnamespace").Should().BeFalse();
            result.NamespaceScope.IsMatching("rootnamespace.other").Should().BeTrue();
        }

        [Test]
        public void Parse_NoTrace_NamespaceAttribute_Values()
        {
            AssemblyLevelNoTraceDefinition result = AssemblyLevelNoTraceDefinition.ParseFromConfig(XElement.Parse("<NoTrace namespace=\"rootnamespace\" />"));
            result.NamespaceScope.IsMatching("rootnamespace").Should().BeTrue();
            result = AssemblyLevelNoTraceDefinition.ParseFromConfig(XElement.Parse("<NoTrace namespace=\"rootnamespace.other\" />"));
            result.NamespaceScope.IsMatching("rootnamespace.other").Should().BeTrue();
            result = AssemblyLevelNoTraceDefinition.ParseFromConfig(XElement.Parse("<NoTrace namespace=\"rootnamespace.*\" />"));
            result.NamespaceScope.IsMatching("rootnamespace").Should().BeFalse();
            result.NamespaceScope.IsMatching("rootnamespace.other").Should().BeTrue();
        }

        [Test]
        public void Sorting_NamespaceNameOrder()
        {
            AssemblyLevelTraceOnDefinition def1 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace"), TraceTargetVisibility.All, TraceTargetVisibility.All);
            AssemblyLevelTraceOnDefinition def2 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace.other"), TraceTargetVisibility.All, TraceTargetVisibility.All);
            AssemblyLevelTraceOnDefinition def3 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace.other.deep"), TraceTargetVisibility.All, TraceTargetVisibility.All);

            List<AssemblyLevelTraceDefinition> list = new List<AssemblyLevelTraceDefinition>() { def1, def2, def3 };
            list.Sort(new AssemblyLevelTraceDefinitionComparer());

            list[0].Should().BeSameAs(def3);
            list[1].Should().BeSameAs(def2);
            list[2].Should().BeSameAs(def1);
        }

        [Test]
        public void Sorting_NamespaceDefinitionTypeOrder()
        {
            AssemblyLevelTraceOnDefinition def1 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace"), TraceTargetVisibility.All, TraceTargetVisibility.All);
            AssemblyLevelTraceOnDefinition def2 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace.*"), TraceTargetVisibility.All, TraceTargetVisibility.All);
            AssemblyLevelTraceOnDefinition def3 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace+*"), TraceTargetVisibility.All, TraceTargetVisibility.All);

            List<AssemblyLevelTraceDefinition> list = new List<AssemblyLevelTraceDefinition>() { def1, def2, def3 };
            list.Sort(new AssemblyLevelTraceDefinitionComparer());

            list[0].Should().BeSameAs(def1);
            list[1].Should().BeSameAs(def2);
            list[2].Should().BeSameAs(def3);
        }

        [Test]
        public void Sorting_NamespaceDefinitionTypeAndNamespaceLengthOrder()
        {
            AssemblyLevelTraceOnDefinition def1 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace"), TraceTargetVisibility.All, TraceTargetVisibility.All);
            AssemblyLevelTraceOnDefinition def2 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace.*"), TraceTargetVisibility.All, TraceTargetVisibility.All);
            AssemblyLevelTraceOnDefinition def3 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace.other"), TraceTargetVisibility.All, TraceTargetVisibility.All);

            List<AssemblyLevelTraceDefinition> list = new List<AssemblyLevelTraceDefinition>() { def1, def2, def3 };
            list.Sort(new AssemblyLevelTraceDefinitionComparer());

            list[0].Should().BeSameAs(def3);
            list[1].Should().BeSameAs(def1);
            list[2].Should().BeSameAs(def2);
        }

        [Test]
        public void Sorting_TraceOnNoTraceOrder()
        {
            AssemblyLevelTraceOnDefinition def1 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace"), TraceTargetVisibility.All, TraceTargetVisibility.All);
            AssemblyLevelTraceOnDefinition def2 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace.other"), TraceTargetVisibility.All, TraceTargetVisibility.All);
            AssemblyLevelNoTraceDefinition def3 = new AssemblyLevelNoTraceDefinition(NamespaceScope.Parse("rootnamespace"));
            AssemblyLevelNoTraceDefinition def4 = new AssemblyLevelNoTraceDefinition(NamespaceScope.Parse("rootnamespace.other"));

            List<AssemblyLevelTraceDefinition> list = new List<AssemblyLevelTraceDefinition>() { def1, def2, def3, def4 };
            list.Sort(new AssemblyLevelTraceDefinitionComparer());

            list[0].Should().BeSameAs(def4);
            list[1].Should().BeSameAs(def2);
            list[2].Should().BeSameAs(def3);
            list[3].Should().BeSameAs(def1);
        }

        [Test]
        public void Sorting_ClassVisibility()
        {
            AssemblyLevelTraceOnDefinition def1 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace.other"), TraceTargetVisibility.All, TraceTargetVisibility.All);
            AssemblyLevelTraceOnDefinition def2 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace.other"), TraceTargetVisibility.InternalOrMoreVisible, TraceTargetVisibility.All);
            AssemblyLevelTraceOnDefinition def3 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace.other"), TraceTargetVisibility.Public, TraceTargetVisibility.All);
            AssemblyLevelTraceOnDefinition def4 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace.other"), TraceTargetVisibility.ProtectedOrMoreVisible, TraceTargetVisibility.All);
            AssemblyLevelTraceOnDefinition def5 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace.other"), TraceTargetVisibility.None, TraceTargetVisibility.All);

            List<AssemblyLevelTraceDefinition> list = new List<AssemblyLevelTraceDefinition>() { def1, def2, def3, def4, def5 };
            list.Sort(new AssemblyLevelTraceDefinitionComparer());

            list[0].Should().BeSameAs(def5);
            list[1].Should().BeSameAs(def3);
            list[2].Should().BeSameAs(def2);
            list[3].Should().BeSameAs(def4);
            list[4].Should().BeSameAs(def1);
        }

        [Test]
        public void Sorting_MethodVisibility()
        {
            AssemblyLevelTraceOnDefinition def1 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace.other"), TraceTargetVisibility.All, TraceTargetVisibility.All);
            AssemblyLevelTraceOnDefinition def2 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace.other"), TraceTargetVisibility.All, TraceTargetVisibility.InternalOrMoreVisible);
            AssemblyLevelTraceOnDefinition def3 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace.other"), TraceTargetVisibility.All, TraceTargetVisibility.Public);
            AssemblyLevelTraceOnDefinition def4 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace.other"), TraceTargetVisibility.All, TraceTargetVisibility.ProtectedOrMoreVisible);
            AssemblyLevelTraceOnDefinition def5 = new AssemblyLevelTraceOnDefinition(NamespaceScope.Parse("rootnamespace.other"), TraceTargetVisibility.All, TraceTargetVisibility.None);

            List<AssemblyLevelTraceDefinition> list = new List<AssemblyLevelTraceDefinition>() { def1, def2, def3, def4, def5 };
            list.Sort(new AssemblyLevelTraceDefinitionComparer());

            list[0].Should().BeSameAs(def5);
            list[1].Should().BeSameAs(def3);
            list[2].Should().BeSameAs(def2);
            list[3].Should().BeSameAs(def4);
            list[4].Should().BeSameAs(def1);
        }
    }
}
