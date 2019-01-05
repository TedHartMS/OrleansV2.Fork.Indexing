using System.Reflection;
using Orleans.Runtime;

namespace Orleans.Indexing.Facet
{
    internal class NonFaultTolerantWorkflowIndexWriterAttributeMapper : IndexWriterAttributeMapperBase,
                                                                        IAttributeToFactoryMapper<NonFaultTolerantWorkflowIndexWriterAttribute>
    {
        private static readonly MethodInfo CreateMethod = typeof(IIndexWriterFactory).GetMethod(nameof(IIndexWriterFactory.CreateNonFaultTolerantWorkflowIndexWriter));

        public Factory<IGrainActivationContext, object> GetFactory(ParameterInfo parameter, NonFaultTolerantWorkflowIndexWriterAttribute attribute)
            => base.GetFactory(CreateMethod, parameter, attribute);
    }
}
