namespace Compze.Abstractions.Internal.Refactoring.Naming;

public interface IMapCurrentAssemblyTypesForRefactoringSupport
{
   void MapTypesForCurrentAssembly(ITypeMappingRegistrar registrar);
}
