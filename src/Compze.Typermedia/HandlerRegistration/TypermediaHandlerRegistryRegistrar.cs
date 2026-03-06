using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Typermedia.HandlerRegistration;

public static class TypermediaHandlerRegistryRegistrar
{
   public static IComponentRegistrar TypermediaHandlerRegistry(this IComponentRegistrar registrar, Action<Type> typeValidator, Func<Type, bool> isInternalTessageType)
      => registrar.Register(Singleton.For<ITypermediaHandlerRegistrar, ITypermediaHandlerRegistry, TypermediaHandlerRegistry>()
                                     .CreatedBy((ITypeMapper typeMapper) => new TypermediaHandlerRegistry(typeMapper, typeValidator, isInternalTessageType)));
}
