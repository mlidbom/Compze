using System.Reflection;
using Compze.Abstractions.Refactoring.Naming;

namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

/// <summary>
/// Fluent builder that collects assembly mapping declarations and stable assembly registrations,
/// then produces a configured <see cref="TypeNameMapper"/>.
/// </summary>
class TypeNameMapperBuilder
{
   readonly Dictionary<Type, Guid> _leafTypeMappings = new();
   readonly Dictionary<Type, Guid> _openGenericMappings = new();
   readonly HashSet<string> _stableAssemblyNames = [];
   readonly HashSet<Assembly> _processedAssemblies = [];

   /// <summary>Well-known Microsoft public key tokens. All assemblies signed with these are stable by default.</summary>
   static readonly string[] MicrosoftPublicKeyTokens =
   [
      "7cec85d7bea7798e", // System.Private.CoreLib
      "b03f5f7f11d50a3a", // most System.* runtime libraries
      "b77a5c561934e089", // legacy (mscorlib, System, System.Core)
      "cc7b13ffcd2ddd51", // System.Private.Xml, netstandard, etc.
      "31bf3856ad364e35"  // Microsoft.* libraries
   ];

   static readonly HashSet<string> MicrosoftPublicKeyTokenSet = [.. MicrosoftPublicKeyTokens];

   internal TypeNameMapperBuilder()
   {
      // Auto-detect and register all currently-loaded Microsoft assemblies
      foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
         TryRegisterMicrosoftAssembly(assembly);
   }

   /// <summary>
   /// Load and register type mappings from the assembly containing <typeparamref name="T"/>.
   /// The assembly must have a <see cref="TypeMappingsAttribute"/> pointing to an <see cref="ITypeMappingDeclaration"/>.
   /// </summary>
   internal TypeNameMapperBuilder MapTypesFromAssemblyContaining<T>()
      => MapTypesFromAssembly(typeof(T).Assembly);

   /// <summary>
   /// Load and register type mappings from the specified assembly.
   /// </summary>
   internal TypeNameMapperBuilder MapTypesFromAssembly(Assembly assembly)
   {
      if(!_processedAssemblies.Add(assembly))
         return this; // already processed

      var attribute = assembly.GetCustomAttribute<TypeMappingsAttribute>();
      if(attribute == null)
         throw new InvalidOperationException(
            $"Assembly '{assembly.GetName().Name}' does not have a [{nameof(TypeMappingsAttribute)}]. " +
            $"Add [assembly: {nameof(TypeMappingsAttribute)}(typeof(YourMappingClass))] to the assembly.");

      var declarationType = attribute.DeclarationType;
      if(!typeof(ITypeMappingDeclaration).IsAssignableFrom(declarationType))
         throw new InvalidOperationException(
            $"Type '{declarationType.FullName}' specified in [{nameof(TypeMappingsAttribute)}] " +
            $"does not implement {nameof(ITypeMappingDeclaration)}.");

      var declaration = (ITypeMappingDeclaration)Activator.CreateInstance(declarationType)!;
      var registrar = new TypeMappingRegistrar(assembly);
      declaration.DeclareMappings(registrar);

      foreach(var kvp in registrar.LeafTypeMappings)
         _leafTypeMappings[kvp.Key] = kvp.Value;

      foreach(var kvp in registrar.OpenGenericMappings)
         _openGenericMappings[kvp.Key] = kvp.Value;

      return this;
   }

   /// <summary>
   /// Register all assemblies signed with the given public key token as stable.
   /// </summary>
   internal TypeNameMapperBuilder UseStableNameStrategyForPublicKeyToken(string publicKeyToken)
   {
      foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
         if(GetPublicKeyTokenString(assembly) == publicKeyToken)
            RegisterStableAssembly(assembly);
      }

      return this;
   }

   /// <summary>
   /// Register the assembly containing <typeparamref name="T"/> as stable.
   /// </summary>
   internal TypeNameMapperBuilder UseStableNameStrategyForAssembliesContaining<T>()
   {
      RegisterStableAssembly(typeof(T).Assembly);
      return this;
   }

   /// <summary>
   /// Build the configured <see cref="TypeNameMapper"/>.
   /// </summary>
   internal TypeNameMapper Build()
      => new(_leafTypeMappings, _openGenericMappings, _stableAssemblyNames);

   void RegisterStableAssembly(Assembly assembly)
   {
      var name = assembly.GetName().Name;
      if(name != null)
         _stableAssemblyNames.Add(name);
   }

   void TryRegisterMicrosoftAssembly(Assembly assembly)
   {
      var token = GetPublicKeyTokenString(assembly);
      if(token != null && MicrosoftPublicKeyTokenSet.Contains(token))
         RegisterStableAssembly(assembly);
   }

   static string? GetPublicKeyTokenString(Assembly assembly)
   {
      var tokenBytes = assembly.GetName().GetPublicKeyToken();
      if(tokenBytes == null || tokenBytes.Length == 0)
         return null;

      return Convert.ToHexStringLower(tokenBytes);
   }
}
