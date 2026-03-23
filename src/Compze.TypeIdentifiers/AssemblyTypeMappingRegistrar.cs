using System.Reflection;

namespace Compze.TypeIdentifiers;

/// <summary>
/// Collects type mappings from an <see cref="IAssemblyTypeMapper"/> and validates
/// that only types from the declaring assembly are mapped.
/// </summary>
sealed class AssemblyTypeMappingRegistrar(Assembly declaringAssembly) : IAssemblyTypeMappingRegistrar
{
   readonly Assembly _declaringAssembly = declaringAssembly;
   internal readonly Dictionary<Type, Guid> LeafTypeMappings = new();
   internal readonly Dictionary<Type, Guid> OpenGenericMappings = new();

   public IAssemblyTypeMappingRegistrar Map<T>(string id)
   {
      var type = typeof(T);
      AssertTypeIsFromDeclaringAssembly(type);

      if(type.IsGenericTypeDefinition)
         throw new InvalidOperationException(
            $"Use {nameof(MapOpenGeneric)} for open generic definitions. Type: {type.FullName}");

      LeafTypeMappings[type] = Guid.Parse(id);
      return this;
   }

   public IAssemblyTypeMappingRegistrar MapOpenGeneric(Type openGenericType, string id)
   {
      AssertTypeIsFromDeclaringAssembly(openGenericType);

      if(!openGenericType.IsGenericTypeDefinition)
         throw new InvalidOperationException(
            $"{nameof(MapOpenGeneric)} requires an open generic definition (e.g. typeof(MyType<>)). Got: {openGenericType.FullName}");

      OpenGenericMappings[openGenericType] = Guid.Parse(id);
      return this;
   }

   void AssertTypeIsFromDeclaringAssembly(Type type)
   {
      if(type.Assembly != _declaringAssembly)
         throw new InvalidOperationException(
            $"Type '{type.FullName}' is from assembly '{type.Assembly.GetName().Name}', " +
            $"but the mapping declaration is for assembly '{_declaringAssembly.GetName().Name}'. " +
            $"Each assembly should only map its own types.");
   }
}
