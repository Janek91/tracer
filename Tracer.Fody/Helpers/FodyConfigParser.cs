using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Tracer.Fody.Filters;
using Tracer.Fody.Weavers;

namespace Tracer.Fody.Helpers
{
    /// <summary>
    /// Class that parses the fody configuration belonging to tracer (in FodyWeavers.xml file).
    /// </summary>
    internal class FodyConfigParser
    {
        private FodyConfigParser()
        { }

        private string _error;
        private string _adapterAssembly;
        private string _logManager;
        private string _logger;
        private string _staticLogger;
        private bool _traceConstructorsFlag;
        private bool _tracePropertiesFlag = true;
        private IEnumerable<XElement> _filterConfigElements;

        public static FodyConfigParser Parse(XElement element)
        {
            FodyConfigParser result = new FodyConfigParser();
            result.DoParse(element);
            return result;
        }

        public TraceLoggingConfiguration Result
        {
            get
            {
                TraceLoggingConfiguration.TraceLoggingConfigurationBuilder result = TraceLoggingConfiguration.New
                    .WithAdapterAssembly(_adapterAssembly)
                    .WithFilter(new DefaultFilter(_filterConfigElements))
                    .WithLogger(_logger)
                    .WithLogManager(_logManager)
                    .WithStaticLogger(_staticLogger);

                if (_traceConstructorsFlag) { result.WithConstructorTraceOn(); }
                else { result.WithConstructorTraceOff(); }

                if (_tracePropertiesFlag) { result.WithPropertiesTraceOn(); }
                else { result.WithPropertiesTraceOff(); }

                return result;
            }
        }

        public bool IsErroneous
        {
            get { return !string.IsNullOrEmpty(_error); }
        }

        public string Error
        {
            get { return _error; }
        }

        private void DoParse(XElement element)
        {

            try
            {
                _adapterAssembly = GetAttributeValue(element, "adapterAssembly", true);
                _logManager = GetAttributeValue(element, "logManager", true);
                _logger = GetAttributeValue(element, "logger", true);
                _staticLogger = GetAttributeValue(element, "staticLogger", false);
                _traceConstructorsFlag = bool.Parse(GetAttributeValueOrDefault(element, "traceConstructors", bool.FalseString));
                _tracePropertiesFlag = bool.Parse(GetAttributeValueOrDefault(element, "traceProperties", bool.TrueString));
                _filterConfigElements = element.Descendants();
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
        }

        private string GetAttributeValue(XElement element, string attributeName, bool isMandatory)
        {
            XAttribute attribute = element.Attribute(attributeName);
            if (isMandatory && (attribute == null || string.IsNullOrWhiteSpace(attribute.Value)))
            {
                throw new ApplicationException(string.Format("Tracer: attribute {0} is missing or empty.", attributeName));
            }

            return attribute != null ? attribute.Value : null;
        }

        private string GetAttributeValueOrDefault(XElement element, string attributeName, string defaultValue)
        {
            XAttribute attribute = element.Attribute(attributeName);
            return attribute != null ? attribute.Value : defaultValue;
        }
    }
}
