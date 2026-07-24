using System.Reflection;

namespace Compze.TypeIdentifiers._private;

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

      AddMapping(LeafTypeMappings, type, Guid.Parse(id));
      return this;
   }

   public IAssemblyTypeMappingRegistrar MapOpenGeneric(Type openGenericType, string id)
   {
      AssertTypeIsFromDeclaringAssembly(openGenericType);

      if(!openGenericType.IsGenericTypeDefinition)
         throw new InvalidOperationException(
            $"{nameof(MapOpenGeneric)} requires an open generic definition (e.g. typeof(MyType<>)). Got: {openGenericType.FullName}");

      AddMapping(OpenGenericMappings, openGenericType, Guid.Parse(id));
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

   // A type↔GUID mapping is permanent identity. Re-mapping a type, or reusing a GUID for a second type, silently
   // corrupts that identity — so both are rejected here, at the declaration site, before they can reach storage.
   static void AddMapping(Dictionary<Type, Guid> mappings, Type type, Guid guid)
   {
      if(mappings.TryGetValue(type, out var existingGuid))
         throw new InvalidOperationException(
            $"Type '{type.FullName}' is already mapped to GUID '{existingGuid}'. A type may be mapped only once — remove the duplicate registration.");

      foreach(var existing in mappings)
         if(existing.Value == guid)
            throw new InvalidOperationException(
               $"GUID '{guid}' is already mapped to type '{existing.Key.FullName}' and cannot also be mapped to '{type.FullName}'. " +
               $"Each type must have its own GUID — this is most likely a copy-paste error where the GUID was not changed.");

      mappings[type] = guid;
   }
}
