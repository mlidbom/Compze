using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Common.Refactoring.Naming.Wiring;

static class CompzeCommonRefactoringRenamingWiring
{
   public static IComponentRegistrar TypeMapper(this IComponentRegistrar @this)
      => @this.Register(Singleton.For<ITypeMapper, TypeMapper>()
                                 .Instance(Naming.TypeMapper.Instance));
}
