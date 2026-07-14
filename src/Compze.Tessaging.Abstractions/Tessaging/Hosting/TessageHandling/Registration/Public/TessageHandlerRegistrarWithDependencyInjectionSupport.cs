namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

//todo:urgent:  This adds literally nothing to a raw ITessageHandlerRegistrar. Remove it. 
public class TessageHandlerRegistrarWithDependencyInjectionSupport(ITessageHandlerRegistrar registrar)
{
   internal ITessageHandlerRegistrar Registrar { get; } = registrar;

}
