namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

public class TessageHandlerRegistrarWithDependencyInjectionSupport(ITessageHandlerRegistrar registrar)
{
   internal ITessageHandlerRegistrar Registrar { get; } = registrar;

}
