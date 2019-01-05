using System.Reflection;
using Orleans.Runtime;

namespace Orleans.Indexing.Facet
{
    internal class FaultTolerantWorkflowIndexWriterAttributeMapper : IndexWriterAttributeMapperBase,
                                                                     IAttributeToFactoryMapper<FaultTolerantWorkflowIndexWriterAttribute>
    {
        private static readonly MethodInfo CreateMethod = typeof(IIndexWriterFactory).GetMethod(nameof(IIndexWriterFactory.CreateFaultTolerantWorkflowIndexWriter));

        public Factory<IGrainActivationContext, object> GetFactory(ParameterInfo parameter, FaultTolerantWorkflowIndexWriterAttribute attribute)
            => base.GetFactory(CreateMethod, parameter, attribute);
    }
}
