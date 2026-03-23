using Compze.TypeIdentifiers;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Typermedia.HandlerRegistration;

public static class TypermediaHandlerRegistryRegistrar
{
   public static IComponentRegistrar TypermediaHandlerRegistry(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITypermediaHandlerRegistrar, ITypermediaHandlerRegistry, TypermediaHandlerRegistry>()
                                     .CreatedBy((ITypeMapper typeMapper) => new TypermediaHandlerRegistry(typeMapper)));
}
