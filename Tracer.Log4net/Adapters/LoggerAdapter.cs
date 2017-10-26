﻿using log4net;
using log4net.Core;
using log4net.ObjectRenderer;
using log4net.Util;
using System;
using System.Collections;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Tracer.Log4Net.Adapters
{
    public class LoggerAdapter
    {
        private const string NullString = "<NULL>";
        private readonly ILogger _logger;
        private readonly Type _type;
        private readonly string _typeName;
        private readonly string _typeNamespace;
        private readonly Func<object, string, string> _renderParameterMethod;

        public LoggerAdapter(Type type)
        {
            _type = type;
            _typeName = PrettyFormat(type);
            _typeNamespace = type.Namespace;
            _logger = LogManager.GetLogger(type).Logger;
            string config = ConfigurationManager.AppSettings["LogUseSafeParameterRendering"];

            if (config != null && config.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                _renderParameterMethod = GetSafeRenderedFormat;
            }
            else
            {
                _renderParameterMethod = GetRenderedFormat;
            }
        }

        #region Methods required for trace enter and leave

        public void TraceEnter(string methodInfo, string[] paramNames, object[] paramValues)
        {
            if (_logger.IsEnabledFor(Level.Trace))
            {
                string message;
                PropertiesDictionary propDict = new PropertiesDictionary();
                propDict["trace"] = "ENTER";

                if (paramNames != null)
                {
                    StringBuilder parameters = new StringBuilder();
                    for (int i = 0; i < paramNames.Length; i++)
                    {
                        parameters.AppendFormat("{0}={1}", paramNames[i], _renderParameterMethod(paramValues[i], NullString));
                        if (i < paramNames.Length - 1) parameters.Append(", ");
                    }
                    string argInfo = parameters.ToString();
                    propDict["arguments"] = argInfo;
                    message = string.Format("Entered into {0} ({1}).", methodInfo, argInfo);
                }
                else
                {
                    message = string.Format("Entered into {0}.", methodInfo);
                }
                Log(Level.Trace, methodInfo, message, null, propDict);
            }
        }

        public void TraceLeave(string methodInfo, long startTicks, long endTicks, string[] paramNames, object[] paramValues)
        {
            if (_logger.IsEnabledFor(Level.Trace))
            {
                PropertiesDictionary propDict = new PropertiesDictionary();
                propDict["trace"] = "LEAVE";

                string returnValue = null;
                if (paramNames != null)
                {
                    StringBuilder parameters = new StringBuilder();
                    for (int i = 0; i < paramNames.Length; i++)
                    {
                        parameters.AppendFormat("{0}={1}", paramNames[i] ?? "$return", _renderParameterMethod(paramValues[i], NullString));
                        if (i < paramNames.Length - 1) parameters.Append(", ");
                    }
                    returnValue = parameters.ToString();
                    propDict["arguments"] = returnValue;
                }

                double timeTaken = ConvertTicksToMilliseconds(endTicks - startTicks);
                propDict["startTicks"] = startTicks;
                propDict["endTicks"] = endTicks;
                propDict["timeTaken"] = timeTaken;

                Log(Level.Trace, methodInfo,
                    string.Format("Returned from {1} ({2}). Time taken: {0:0.00} ms.",
                        timeTaken, methodInfo, returnValue), null, propDict);
            }
        }

        #endregion

        #region ILog methods and properties

        public void LogDebug(string methodInfo, object message)
        {
            if (_logger.IsEnabledFor(Level.Debug))
            {
                Log(Level.Debug, methodInfo, message);
            }
        }

        public void LogDebug(string methodInfo, object message, Exception exception)
        {
            if (_logger.IsEnabledFor(Level.Debug))
            {
                Log(Level.Debug, methodInfo, message, exception);
            }
        }

        public void LogDebugFormat(string methodInfo, string format, params object[] args)
        {
            if (_logger.IsEnabledFor(Level.Debug))
            {
                Log(Level.Debug, methodInfo, string.Format(format, args));
            }
        }

        public void LogDebugFormat(string methodInfo, string format, object arg0)
        {
            if (_logger.IsEnabledFor(Level.Debug))
            {
                Log(Level.Debug, methodInfo, string.Format(format, arg0));
            }
        }

        public void LogDebugFormat(string methodInfo, string format, object arg0, object arg1)
        {
            if (_logger.IsEnabledFor(Level.Debug))
            {
                Log(Level.Debug, methodInfo, string.Format(format, arg0, arg1));
            }
        }

        public void LogDebugFormat(string methodInfo, string format, object arg0, object arg1, object arg2)
        {
            if (_logger.IsEnabledFor(Level.Debug))
            {
                Log(Level.Debug, methodInfo, string.Format(format, arg0, arg1, arg2));
            }
        }

        public void LogDebugFormat(string methodInfo, IFormatProvider provider, string format, params object[] args)
        {
            if (_logger.IsEnabledFor(Level.Debug))
            {
                Log(Level.Debug, methodInfo, string.Format(provider, format, args));
            }
        }

        public void LogInfo(string methodInfo, object message)
        {
            if (_logger.IsEnabledFor(Level.Info))
            {
                Log(Level.Info, methodInfo, message);
            }
        }

        public void LogInfo(string methodInfo, object message, Exception exception)
        {
            if (_logger.IsEnabledFor(Level.Info))
            {
                Log(Level.Info, methodInfo, message, exception);
            }
        }

        public void LogInfoFormat(string methodInfo, string format, params object[] args)
        {
            if (_logger.IsEnabledFor(Level.Info))
            {
                Log(Level.Info, methodInfo, string.Format(format, args));
            }
        }

        public void LogInfoFormat(string methodInfo, string format, object arg0)
        {
            if (_logger.IsEnabledFor(Level.Info))
            {
                Log(Level.Info, methodInfo, string.Format(format, arg0));
            }
        }

        public void LogInfoFormat(string methodInfo, string format, object arg0, object arg1)
        {
            if (_logger.IsEnabledFor(Level.Info))
            {
                Log(Level.Info, methodInfo, string.Format(format, arg0, arg1));
            }
        }

        public void LogInfoFormat(string methodInfo, string format, object arg0, object arg1, object arg2)
        {
            if (_logger.IsEnabledFor(Level.Info))
            {
                Log(Level.Info, methodInfo, string.Format(format, arg0, arg1, arg2));
            }
        }

        public void LogInfoFormat(string methodInfo, IFormatProvider provider, string format, params object[] args)
        {
            if (_logger.IsEnabledFor(Level.Info))
            {
                Log(Level.Info, methodInfo, string.Format(provider, format, args));
            }
        }

        public void LogWarn(string methodInfo, object message)
        {
            if (_logger.IsEnabledFor(Level.Warn))
            {
                Log(Level.Warn, methodInfo, message);
            }
        }

        public void LogWarn(string methodInfo, object message, Exception exception)
        {
            if (_logger.IsEnabledFor(Level.Warn))
            {
                Log(Level.Warn, methodInfo, message, exception);
            }
        }

        public void LogWarnFormat(string methodInfo, string format, params object[] args)
        {
            if (_logger.IsEnabledFor(Level.Warn))
            {
                Log(Level.Warn, methodInfo, string.Format(format, args));
            }
        }

        public void LogWarnFormat(string methodInfo, string format, object arg0)
        {
            if (_logger.IsEnabledFor(Level.Warn))
            {
                Log(Level.Warn, methodInfo, string.Format(format, arg0));
            }
        }

        public void LogWarnFormat(string methodInfo, string format, object arg0, object arg1)
        {
            if (_logger.IsEnabledFor(Level.Warn))
            {
                Log(Level.Warn, methodInfo, string.Format(format, arg0, arg1));
            }
        }

        public void LogWarnFormat(string methodInfo, string format, object arg0, object arg1, object arg2)
        {
            if (_logger.IsEnabledFor(Level.Warn))
            {
                Log(Level.Warn, methodInfo, string.Format(format, arg0, arg1, arg2));
            }
        }

        public void LogWarnFormat(string methodInfo, IFormatProvider provider, string format, params object[] args)
        {
            if (_logger.IsEnabledFor(Level.Warn))
            {
                Log(Level.Warn, methodInfo, string.Format(provider, format, args));
            }
        }

        public void LogError(string methodInfo, object message)
        {
            if (_logger.IsEnabledFor(Level.Error))
            {
                Log(Level.Error, methodInfo, message);
            }
        }

        public void LogError(string methodInfo, object message, Exception exception)
        {
            if (_logger.IsEnabledFor(Level.Error))
            {
                Log(Level.Error, methodInfo, message, exception);
            }
        }

        public void LogErrorFormat(string methodInfo, string format, params object[] args)
        {
            if (_logger.IsEnabledFor(Level.Error))
            {
                Log(Level.Error, methodInfo, string.Format(format, args));
            }
        }

        public void LogErrorFormat(string methodInfo, string format, object arg0)
        {
            if (_logger.IsEnabledFor(Level.Error))
            {
                Log(Level.Error, methodInfo, string.Format(format, arg0));
            }
        }

        public void LogErrorFormat(string methodInfo, string format, object arg0, object arg1)
        {
            if (_logger.IsEnabledFor(Level.Error))
            {
                Log(Level.Error, methodInfo, string.Format(format, arg0, arg1));
            }
        }

        public void LogErrorFormat(string methodInfo, string format, object arg0, object arg1, object arg2)
        {
            if (_logger.IsEnabledFor(Level.Error))
            {
                Log(Level.Error, methodInfo, string.Format(format, arg0, arg1, arg2));
            }
        }

        public void LogErrorFormat(string methodInfo, IFormatProvider provider, string format, params object[] args)
        {
            if (_logger.IsEnabledFor(Level.Error))
            {
                Log(Level.Error, methodInfo, string.Format(provider, format, args));
            }
        }

        public void LogFatal(string methodInfo, object message)
        {
            if (_logger.IsEnabledFor(Level.Fatal))
            {
                Log(Level.Fatal, methodInfo, message);
            }
        }

        public void LogFatal(string methodInfo, object message, Exception exception)
        {
            if (_logger.IsEnabledFor(Level.Fatal))
            {
                Log(Level.Fatal, methodInfo, message, exception);
            }
        }

        public void LogFatalFormat(string methodInfo, string format, params object[] args)
        {
            if (_logger.IsEnabledFor(Level.Fatal))
            {
                Log(Level.Fatal, methodInfo, string.Format(format, args));
            }
        }

        public void LogFatalFormat(string methodInfo, string format, object arg0)
        {
            if (_logger.IsEnabledFor(Level.Fatal))
            {
                Log(Level.Fatal, methodInfo, string.Format(format, arg0));
            }
        }

        public void LogFatalFormat(string methodInfo, string format, object arg0, object arg1)
        {
            if (_logger.IsEnabledFor(Level.Fatal))
            {
                Log(Level.Fatal, methodInfo, string.Format(format, arg0, arg1));
            }
        }

        public void LogFatalFormat(string methodInfo, string format, object arg0, object arg1, object arg2)
        {
            if (_logger.IsEnabledFor(Level.Fatal))
            {
                Log(Level.Fatal, methodInfo, string.Format(format, arg0, arg1, arg2));
            }
        }

        public void LogFatalFormat(string methodInfo, IFormatProvider provider, string format, params object[] args)
        {
            if (_logger.IsEnabledFor(Level.Fatal))
            {
                Log(Level.Fatal, methodInfo, string.Format(provider, format, args));
            }
        }

        public bool LogIsDebugEnabled
        {
            get { return _logger.IsEnabledFor(Level.Debug); }
        }

        public bool LogIsInfoEnabled
        {
            get { return _logger.IsEnabledFor(Level.Info); }
        }

        public bool LogIsWarnEnabled
        {
            get { return _logger.IsEnabledFor(Level.Warn); }
        }

        public bool LogIsErrorEnabled
        {
            get { return _logger.IsEnabledFor(Level.Error); }
        }

        public bool LogIsFatalEnabled
        {
            get { return _logger.IsEnabledFor(Level.Fatal); }
        }

        #endregion

        private void Log(Level level, string methodInfo, object message, Exception exception = null, PropertiesDictionary properties = null)
        {
            LoggingEventData eventData = new LoggingEventData()
            {
                LocationInfo = new LocationInfo(_typeName, methodInfo, "", ""),
                Level = level,
                Message = GetRenderedFormat(message),
                TimeStamp = DateTime.Now,
                LoggerName = _logger.Name,
                ThreadName = Thread.CurrentThread.Name,
                Domain = SystemInfo.ApplicationFriendlyName,
                ExceptionString = GetRenderedFormat(exception),
                Properties = properties ?? new PropertiesDictionary()
            };

            _logger.Log(new LoggingEvent(eventData));
        }

        private string GetSafeRenderedFormat(object message, string stringRepresentationOfNull = "")
        {
            if (message == null)
            {
                return stringRepresentationOfNull;
            }

            string str = message as string;
            if (str != null)
            {
                return str;
            }

            if (_logger.Repository != null)
            {
                //try to escape the default renderer
                IObjectRenderer renderer = _logger.Repository.RendererMap.Get(message);
                if (renderer != null && renderer != _logger.Repository.RendererMap.DefaultRenderer)
                {

                    StringWriter stringWriter = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
                    renderer.RenderObject(_logger.Repository.RendererMap, message, stringWriter);
                    return stringWriter.ToString();
                }

                return message.ToString();
            }

            return message.ToString();
        }

        private string GetRenderedFormat(object message, string stringRepresentationOfNull = "")
        {
            if (message == null)
            {
                return stringRepresentationOfNull;
            }
            else if (message is string)
            {
                return (string)message;
            }
            else if (message is IEnumerator && _logger.Repository != null)
            {
                string retVal = _logger.Repository.RendererMap.FindAndRender(message);
                IEnumerator enumerable = (IEnumerator)message;
                enumerable.Reset();
                return retVal;
            }
            else if (_logger.Repository != null)
            {
                return _logger.Repository.RendererMap.FindAndRender(message);
            }
            else
            {
                return message.ToString();
            }
        }

        private static double ConvertTicksToMilliseconds(long ticks)
        {
            //ticks * tickFrequency * 10000
            return ticks * (10000000 / (double)Stopwatch.Frequency) / 10000L;
        }

        private static string PrettyFormat(Type type)
        {
            StringBuilder sb = new StringBuilder();
            if (type.IsGenericType)
            {
                sb.Append(type.Name.Remove(type.Name.IndexOf('`')));
                AddGenericPrettyFormat(sb, type.GetGenericArguments());
            }
            else
            {
                sb.Append(type.Name);
            }
            return sb.ToString();
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
