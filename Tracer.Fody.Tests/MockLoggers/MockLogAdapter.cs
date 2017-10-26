﻿using System;
using System.Linq;
using System.Text;

namespace Tracer.Fody.Tests.MockLoggers
{
    public class MockLogAdapter
    {
        private readonly Type _type;

        public MockLogAdapter(Type type)
        {
            _type = type;
        }

        public void TraceEnter(string methodInfo, string[] paramNames, object[] paramValues)
        {
            if (paramNames != null)
            {
                string[] stringValues = paramValues.Select(val => val != null ? val.ToString() : null).ToArray();
                MockLogManagerAdapter.TraceEnterCalled(TypePrettyName, methodInfo, paramNames, stringValues);
            }
            else
            {
                MockLogManagerAdapter.TraceEnterCalled(TypePrettyName, methodInfo, null, null);
            }
        }

        public void TraceLeave(string methodInfo, long startTicks, long endTicks, string[] paramNames, object[] paramValues)
        {
            long numberOfTicks = endTicks - startTicks;
            if (paramNames != null)
            {
                string[] stringValues = paramValues.Select(val => val != null ? val.ToString() : null).ToArray();
                MockLogManagerAdapter.TraceLeaveCalled(TypePrettyName, methodInfo, numberOfTicks, paramNames, stringValues);
            }
            else
            {
                MockLogManagerAdapter.TraceLeaveCalled(TypePrettyName, methodInfo, numberOfTicks, null, null);
            }
        }

        public void MockLogOuter(string methodInfo, string message)
        {
            MockLogManagerAdapter.LogCalled(TypePrettyName, methodInfo, "MockLogOuter", new[] { message });
        }

        public void MockLogOuterNoParam(string methodInfo)
        {
            MockLogManagerAdapter.LogCalled(TypePrettyName, methodInfo, "MockLogOuterNoParam");
        }

        public void MockLogOuter(string methodInfo, string message, int i)
        {
            MockLogManagerAdapter.LogCalled(TypePrettyName, methodInfo, "MockLogOuter", new[] { message, i.ToString() });
        }
        public void MockLogGenericOuter<T>(string methodInfo)
        {
            MockLogManagerAdapter.LogCalled(TypePrettyName, methodInfo, "MockLogGenericOuter", new[] { typeof(T).Name });
        }

        public void MockLogGenericOuter<T>(string methodInfo, T message)
        {
            MockLogManagerAdapter.LogCalled(TypePrettyName, methodInfo, "MockLogGenericOuter", new[] { message.ToString() });
        }

        public void MockLogGenericOuter<T, K>(string methodInfo, K num1, T message, K num2)
        {
            MockLogManagerAdapter.LogCalled(TypePrettyName, methodInfo, "MockLogGenericOuter", new[] { num1.ToString(), message.ToString(), num2.ToString() });
        }

        public void MockLogException(string methodInfo, string message, Exception exception)
        {
            MockLogManagerAdapter.LogCalled(TypePrettyName, methodInfo, "MockLogException", new[] { message, exception.ToString() });
        }

        public bool MockLogIsEnabled
        {
            get
            {
                MockLogManagerAdapter.LogPropertyCalled(TypePrettyName, "MockLogIsEnabled");
                return true;
            }
        }

        public bool MockLogReadWrite { get; set; }

        private string TypePrettyName
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (!string.IsNullOrEmpty(_type.Namespace))
                {
                    sb.Append(_type.Namespace);
                    sb.Append(".");
                }

                if (_type.IsGenericType)
                {
                    sb.Append(_type.Name.Remove(_type.Name.IndexOf('`')));
                    AddGenericPrettyFormat(sb, _type.GetGenericArguments());
                }
                else
                {
                    sb.Append(_type.Name);
                }
                return sb.ToString();
            }
        }

        private static void AddGenericPrettyFormat(StringBuilder sb, Type[] genericArgumentTypes)
        {
            sb.Append("<");
            for (int i = 0; i < genericArgumentTypes.Length; i++)
            {
                sb.Append(genericArgumentTypes[i].Name);
                if (i < genericArgumentTypes.Length - 1) sb.Append(", ");
            }
            sb.Append(">");
        }
    }
}
