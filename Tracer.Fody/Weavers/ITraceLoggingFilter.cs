using Mono.Cecil;

namespace Tracer.Fody.Weavers
{
    /// <summary>
    /// Interface is used to decide if a method needs tracing or not
    /// </summary>
    public interface ITraceLoggingFilter
    {
        /// <summary>
        /// Returns true if the method described by the given MethodDefinition should be traced (i.e. add trace entry and leave loglines)
        /// </summary>
        /// <param name="definition">Definition of the method in question</param>
        /// <returns>true if the method should be traced</returns>
        bool ShouldAddTrace(MethodDefinition definition);
    }
}
