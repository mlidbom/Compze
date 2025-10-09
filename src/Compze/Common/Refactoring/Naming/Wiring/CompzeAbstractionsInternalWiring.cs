using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Common.Refactoring.Naming.Wiring;

static class CompzeCommonRefactoringRenamingWiring
{
   public static IDependencyRegistrar TypeMapper(this IDependencyRegistrar @this)
      => @this.Register(Singleton.For<ITypeMapper, TypeMapper>()
                                 .Instance(Naming.TypeMapper.Instance));
}
