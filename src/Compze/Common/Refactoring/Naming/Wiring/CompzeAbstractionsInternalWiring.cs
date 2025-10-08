using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Common.Refactoring.Naming.Wiring;

static class CompzeCommonRefactoringRenamingWiring
{
   public static IDependencyInjectionContainer RegisterTypeMapper(this IDependencyInjectionContainer @this)
      => @this.Register(Singleton.For<ITypeMapper, TypeMapper>()
                                 .Instance(TypeMapper.Instance));
}
