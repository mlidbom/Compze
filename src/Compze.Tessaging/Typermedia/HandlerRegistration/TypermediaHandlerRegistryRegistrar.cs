using Compze.TypeIdentifiers;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Typermedia.HandlerRegistration;

public static class TypermediaHandlerRegistryRegistrar
{
   public static IComponentRegistrar TypermediaHandlerRegistry(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITypermediaHandlerRegistrar, ITypermediaHandlerRegistry, TypermediaHandlerRegistry>()
                                     .CreatedBy((ITypeMap typeMap) => new TypermediaHandlerRegistry(typeMap)));
}
