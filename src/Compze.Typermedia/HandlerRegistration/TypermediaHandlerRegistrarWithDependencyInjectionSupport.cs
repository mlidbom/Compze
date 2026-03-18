namespace Compze.Typermedia.HandlerRegistration;

public class TypermediaHandlerRegistrarWithDependencyInjectionSupport(ITypermediaHandlerRegistrar registrar)
{
   internal ITypermediaHandlerRegistrar Registrar { get; } = registrar;
}
