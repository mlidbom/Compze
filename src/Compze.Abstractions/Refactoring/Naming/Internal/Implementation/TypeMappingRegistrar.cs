using System.Reflection;
using Compze.Abstractions.Refactoring.Naming;

namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

/// <summary>
/// Collects type mappings from an <see cref="ITypeMappingDeclaration"/> and validates
/// that only types from the declaring assembly are mapped.
/// </summary>
sealed class TypeMappingRegistrar(Assembly declaringAssembly) : ITypeMappingRegistrar
{
   readonly Assembly _declaringAssembly = declaringAssembly;
   internal readonly Dictionary<Type, Guid> LeafTypeMappings = new();
   internal readonly Dictionary<Type, Guid> OpenGenericMappings = new();

   public ITypeMappingRegistrar Map<T>(string id)
   {
      var type = typeof(T);
      ValidateTypeIsFromDeclaringAssembly(type);

      if(type.IsGenericTypeDefinition)
         throw new InvalidOperationException(
            $"Use {nameof(MapOpenGeneric)} for open generic definitions. Type: {type.FullName}");

      LeafTypeMappings[type] = Guid.Parse(id);
      return this;
   }

   public ITypeMappingRegistrar MapOpenGeneric(Type openGenericType, string id)
   {
      ValidateTypeIsFromDeclaringAssembly(openGenericType);

      if(!openGenericType.IsGenericTypeDefinition)
         throw new InvalidOperationException(
            $"{nameof(MapOpenGeneric)} requires an open generic definition (e.g. typeof(MyType<>)). Got: {openGenericType.FullName}");

      OpenGenericMappings[openGenericType] = Guid.Parse(id);
      return this;
   }

   void ValidateTypeIsFromDeclaringAssembly(Type type)
   {
      if(type.Assembly != _declaringAssembly)
         throw new InvalidOperationException(
            $"Type '{type.FullName}' is from assembly '{type.Assembly.GetName().Name}', " +
            $"but the mapping declaration is for assembly '{_declaringAssembly.GetName().Name}'. " +
            $"Each assembly should only map its own types.");
   }
}
