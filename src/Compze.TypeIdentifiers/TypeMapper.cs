using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Compze.TypeIdentifiers;

/// <summary>
/// Mutable implementation of <see cref="ITypeMapper"/> and <see cref="ITypeMap"/> that supports incremental
/// assembly registration. Leaf types and open generic definitions are mapped to GUIDs; constructed and
/// stable types resolve structurally via <see cref="TypeNameMapper"/>. The produced <see cref="TypeId"/>
/// is always canonical for its type.
/// </summary>
public class TypeMapper : ITypeMapper, ITypeMap
{
   readonly TypeNameMapper _typeNameMapper = new();
   volatile Caches _caches = new();
   readonly HashSet<Assembly> _processedAssemblies = [];
   readonly object _registrationLock = new();

   /// <summary>Well-known Microsoft public key tokens. All assemblies signed with these are stable by default.
   /// <c>adb9793829ddae60</c> signs the out-of-band framework packages: <c>Microsoft.Extensions.*</c>,
   /// <c>Microsoft.AspNetCore.*</c>, and most NuGet-delivered framework assemblies.</summary>
   static readonly HashSet<string> MicrosoftPublicKeyTokens = ["7cec85d7bea7798e", "b03f5f7f11d50a3a", "b77a5c561934e089", "cc7b13ffcd2ddd51", "31bf3856ad364e35", "adb9793829ddae60"];

   /// <summary>Creates a mapper with well-known Microsoft assemblies pre-registered as stable, so framework types resolve without explicit registration.</summary>
   public TypeMapper() => SeedMicrosoftPublicKeyTokensAsStable();

   /// <inheritdoc />
   public void MapTypesFromAssemblyContaining<T>() => MapTypesFromAssembly(typeof(T).Assembly);

   /// <inheritdoc />
   public void MapTypesFromAssembly(Assembly assembly)
   {
      lock(_registrationLock)
      {
         if(_processedAssemblies.Contains(assembly))
            return;

         var attribute = assembly.GetCustomAttribute<AssemblyTypeMapperAttribute>();
         if(attribute == null)
            throw new InvalidOperationException(
               $"Assembly '{assembly.GetName().Name}' does not have a [{nameof(AssemblyTypeMapperAttribute)}]. " +
               $"Add [assembly: {nameof(AssemblyTypeMapperAttribute)}(typeof(YourMapper))] to the assembly.");

         var mapperType = attribute.Mapper;
         if(!typeof(IAssemblyTypeMapper).IsAssignableFrom(mapperType))
            throw new InvalidOperationException(
               $"Type '{mapperType.FullName}' specified in [{nameof(AssemblyTypeMapperAttribute)}] " +
               $"does not implement {nameof(IAssemblyTypeMapper)}.");

         var mapper = (IAssemblyTypeMapper)Activator.CreateInstance(mapperType)!;
         var registrar = new AssemblyTypeMappingRegistrar(assembly);
         mapper.Map(registrar);

         // Publish all of this assembly's mappings atomically: a collision throws here, before the assembly is
         // marked processed, so the live state is untouched and the attempt stays retryable rather than leaving a
         // half-registered assembly that the early-return above would skip forever.
         _typeNameMapper.AddAssemblyMappings(registrar.LeafTypeMappings, registrar.OpenGenericMappings);

         _processedAssemblies.Add(assembly);
         ClearCaches();
      }
   }

   /// <inheritdoc />
   public void UseStableNameStrategyForAssemblyContaining<T>()
   {
      var name = typeof(T).Assembly.GetName().Name;
      if(name != null)
         _typeNameMapper.AddStableAssemblyName(name);

      ClearCaches();
   }

   /// <inheritdoc />
   public void UseStableNameStrategyForPublicKeyToken(string publicKeyToken)
   {
#pragma warning disable CA1308 // .NET assembly-qualified-name public-key tokens are lowercase hex by convention; ToUpperInvariant would break type resolution.
      _typeNameMapper.AddStablePublicKeyToken(publicKeyToken.ToLowerInvariant());
#pragma warning restore CA1308
      ClearCaches();
   }

   /// <inheritdoc />
   public TypeId GetId(Type type) => _caches.IdCache.GetOrAdd(type, t => new TypeId(t, _typeNameMapper.GetId(t).StringRepresentation));

   // Resolve the string to its .NET type first, then route through GetId(Type) so the same mapped-or-stable rule
   // applies: a string that resolves to a runtime type with no registered identity is rejected, not silently
   // handed back a type that could never be re-serialized. The resulting TypeId carries both the Type and the
   // canonical string.
   /// <inheritdoc />
   public TypeId GetId(string persistedTypeString) => GetId(_typeNameMapper.GetTypeFromPersistedString(persistedTypeString));

   /// <inheritdoc />
   public bool TryGetId(Type type, [NotNullWhen(true)] out TypeId? id)
   {
      if(!CanResolve(type))
      {
         id = null;
         return false;
      }

      id = GetId(type);
      return true;
   }

   /// <inheritdoc />
   public void AssertMappingsExistFor(IEnumerable<Type> types)
   {
      var missing = types.Where(type => !CanResolve(type)).ToList();
      if(missing.Count > 0)
         throw new InvalidOperationException(
            $"Missing type mappings for: {string.Join(", ", missing.Select(t => t.FullName))}");
   }

   bool CanResolve(Type type)
   {
      if(_typeNameMapper.HasLeafMapping(type))
         return true;

      if(type.IsConstructedGenericType)
         return _typeNameMapper.HasMappingForOpenGeneric(type.GetGenericTypeDefinition())
             && type.GetGenericArguments().All(CanResolve);

      if(type.IsArray)
         return CanResolve(type.GetElementType()!);

      return _typeNameMapper.IsStableType(type);
   }

   // Registration swaps in a fresh, empty cache set rather than clearing in place. A lookup that is mid-flight
   // when this runs stores its result into the now-abandoned previous set, so it can never poison the current one.
   void ClearCaches() => _caches = new Caches();

   sealed class Caches
   {
      internal readonly ConcurrentDictionary<Type, TypeId> IdCache = new();
   }

   // Stability is decided from each type's own assembly public key token at lookup time (see TypeNameMapper.State),
   // so we only need to seed the trusted tokens here — no scan of the currently-loaded assemblies, which would miss
   // any framework assembly loaded later.
   void SeedMicrosoftPublicKeyTokensAsStable()
   {
      foreach(var token in MicrosoftPublicKeyTokens)
         _typeNameMapper.AddStablePublicKeyToken(token);
   }
}
