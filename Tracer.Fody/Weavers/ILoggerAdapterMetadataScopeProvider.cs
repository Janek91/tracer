using Mono.Cecil;

namespace Tracer.Fody.Weavers
{
    /// <summary>
    /// Provides a way to reach the metadata scope of the logAdapter 
    /// </summary>
    internal interface ILoggerAdapterMetadataScopeProvider
    {
        IMetadataScope GetLoggerAdapterMetadataScope();
    }
}
