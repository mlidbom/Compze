namespace Compze.Tessaging.Typermedia.HandlerRegistration;

public class TypermediaHandlerRegistrarWithDependencyInjectionSupport(ITypermediaHandlerRegistrar registrar)
{
   internal ITypermediaHandlerRegistrar Registrar { get; } = registrar;
}
