using System.Reflection;
using Compze.TypeIdentifiers.Internal;
using Compze.TypeIdentifiers.Private;

namespace Compze.TypeIdentifiers;

/// <summary>
/// Collects the assembly-level declarations a <see cref="ITypeMap"/> is built from, and builds it. Each declaration
/// names one assembly and says how that assembly's types get their identity: mapped to the GUIDs the assembly itself
/// declares (<see cref="MapTypesFromAssembly"/>), or kept as stable type names that pass through unchanged
/// (<see cref="UseStableNameStrategyForAssembly"/>).
/// </summary>
/// <remarks>
/// The map is built in its entirety, once, from the complete set of declarations — it is never registered into
/// afterwards. That is what makes an <see cref="ITypeMap"/> immutable, lock-free and free of cache invalidation, and it
/// is why the order declarations arrive in cannot change the resulting map.
/// </remarks>
/// <remarks>
/// Declaring the same assembly more than once is expected and fine — several independent components may each need the
/// same assembly's types. Declaring it with two <em>different</em> strategies is a disagreement about what the
/// assembly's type identity is, and throws: one of the two callers would otherwise silently lose.
/// </remarks>
public sealed class TypeMapBuilder
{
   /// <summary>Well-known Microsoft public key tokens. All assemblies signed with these are stable by default.
   /// <c>adb9793829ddae60</c> signs the out-of-band framework packages: <c>Microsoft.Extensions.*</c>,
   /// <c>Microsoft.AspNetCore.*</c>, and most NuGet-delivered framework assemblies.</summary>
   static readonly string[] MicrosoftPublicKeyTokens = ["7cec85d7bea7798e", "b03f5f7f11d50a3a", "b77a5c561934e089", "cc7b13ffcd2ddd51", "31bf3856ad364e35", "adb9793829ddae60"];

   readonly Dictionary<Assembly, AssemblyTypeIdentityStrategy> _declaredStrategies = [];
   readonly Dictionary<Guid, Type> _guidToLeafType = [];
   readonly Dictionary<Type, Guid> _leafTypeToGuid = [];
   readonly Dictionary<Guid, Type> _guidToOpenGeneric = [];
   readonly Dictionary<Type, Guid> _openGenericToGuid = [];
   readonly HashSet<string> _stableAssemblyNames = [];

   // Stability is decided from each type's own assembly public key token at lookup time, so we only need to seed the
   // trusted tokens here — no scan of the currently-loaded assemblies, which would miss any framework assembly loaded later.
   readonly HashSet<string> _stablePublicKeyTokens = [..MicrosoftPublicKeyTokens];

   /// <summary>Declares that the types of the assembly containing <typeparamref name="T"/> are mapped to the GUIDs that
   /// assembly declares — see <see cref="MapTypesFromAssembly"/>.</summary>
   public TypeMapBuilder MapTypesFromAssemblyContaining<T>() => MapTypesFromAssembly(typeof(T).Assembly);

   /// <summary>
   /// Declares that <paramref name="assembly"/>'s types are mapped to the GUIDs it declares. The assembly must carry an
   /// <see cref="AssemblyTypeMapperAttribute"/> naming its <see cref="IAssemblyTypeMapper"/>, which is what actually
   /// declares the type↔GUID pairs; this only says "that assembly's mappings must be part of the map".
   /// </summary>
   public TypeMapBuilder MapTypesFromAssembly(Assembly assembly)
   {
      if(!DeclareStrategyFor(assembly, AssemblyTypeIdentityStrategy.MappedToDeclaredGuids)) return this;

      var registrar = new AssemblyTypeMappingRegistrar(assembly);
      AssemblyTypeMapperFor(assembly).Map(registrar);

      foreach(var (type, guid) in registrar.LeafTypeMappings)
         AddMapping(_leafTypeToGuid, _guidToLeafType, type, guid);

      foreach(var (openGenericType, guid) in registrar.OpenGenericMappings)
         AddMapping(_openGenericToGuid, _guidToOpenGeneric, openGenericType, guid);

      return this;
   }

   /// <summary>Declares the assembly containing <typeparamref name="T"/> stable — see <see cref="UseStableNameStrategyForAssembly"/>.</summary>
   public TypeMapBuilder UseStableNameStrategyForAssemblyContaining<T>() => UseStableNameStrategyForAssembly(typeof(T).Assembly);

   /// <summary>
   /// Declares <paramref name="assembly"/> stable: its type names are persisted unchanged rather than being replaced by
   /// GUIDs. Suitable only for assemblies whose type names will not be renamed or moved, because a persisted name that
   /// later changes no longer resolves.
   /// </summary>
   public TypeMapBuilder UseStableNameStrategyForAssembly(Assembly assembly)
   {
      if(!DeclareStrategyFor(assembly, AssemblyTypeIdentityStrategy.StableTypeNames)) return this;

      var name = assembly.GetName().Name;
      if(name != null) _stableAssemblyNames.Add(name);

      return this;
   }

   /// <summary>Declares every assembly signed with <paramref name="publicKeyToken"/> stable — the by-signature form of
   /// <see cref="UseStableNameStrategyForAssembly"/>, for a whole publisher at once.</summary>
   public TypeMapBuilder UseStableNameStrategyForPublicKeyToken(string publicKeyToken)
   {
#pragma warning disable CA1308 // .NET assembly-qualified-name public-key tokens are lowercase hex by convention; ToUpperInvariant would break type resolution.
      _stablePublicKeyTokens.Add(publicKeyToken.ToLowerInvariant());
#pragma warning restore CA1308
      return this;
   }

   /// <summary>Builds the finished, immutable <see cref="ITypeMap"/> from everything declared so far.</summary>
   /// <remarks>
   /// The declarations are copied, not handed over: a builder used again after building must not reach into a map that
   /// already exists. That map has caches computed against the mappings it was built with, so a later declaration
   /// leaking into it would resurrect exactly the stale-lookup corruption an immutable map exists to make impossible.
   /// </remarks>
   public ITypeMap Build() =>
      new TypeMap(new TypeNameMapper(new Dictionary<Guid, Type>(_guidToLeafType),
                                     new Dictionary<Type, Guid>(_leafTypeToGuid),
                                     new Dictionary<Guid, Type>(_guidToOpenGeneric),
                                     new Dictionary<Type, Guid>(_openGenericToGuid),
                                     [.._stableAssemblyNames],
                                     [.._stablePublicKeyTokens]));

   /// <summary>Records <paramref name="strategy"/> as this assembly's, and reports whether it is new — a repeat of the
   /// same declaration is a no-op, a contradicting one throws.</summary>
   bool DeclareStrategyFor(Assembly assembly, AssemblyTypeIdentityStrategy strategy)
   {
      if(!_declaredStrategies.TryGetValue(assembly, out var alreadyDeclared))
      {
         _declaredStrategies.Add(assembly, strategy);
         return true;
      }

      if(alreadyDeclared != strategy)
         throw new InvalidOperationException(
            $"Assembly '{assembly.GetName().Name}' was declared both {Describe(alreadyDeclared)} and {Describe(strategy)}. "
          + "An assembly's types get their identity one way or the other — the two declarations disagree about what this assembly's persisted type identity is, and one of them would silently lose.");

      return false;
   }

   static string Describe(AssemblyTypeIdentityStrategy strategy) =>
      strategy switch
      {
         AssemblyTypeIdentityStrategy.MappedToDeclaredGuids => "mapped to its declared GUIDs",
         AssemblyTypeIdentityStrategy.StableTypeNames => "stable-named",
         _ => strategy.ToString()
      };

   static IAssemblyTypeMapper AssemblyTypeMapperFor(Assembly assembly)
   {
      var attribute = assembly.GetCustomAttribute<AssemblyTypeMapperAttribute>()
                   ?? throw new InvalidOperationException(
                         $"Assembly '{assembly.GetName().Name}' does not have a [{nameof(AssemblyTypeMapperAttribute)}]. "
                       + $"Add [assembly: {nameof(AssemblyTypeMapperAttribute)}(typeof(YourMapper))] to the assembly.");

      var mapperType = attribute.Mapper;
      if(!typeof(IAssemblyTypeMapper).IsAssignableFrom(mapperType))
         throw new InvalidOperationException(
            $"Type '{mapperType.FullName}' specified in [{nameof(AssemblyTypeMapperAttribute)}] "
          + $"does not implement {nameof(IAssemblyTypeMapper)}.");

      return (IAssemblyTypeMapper)Activator.CreateInstance(mapperType)!;
   }

   // A type↔GUID mapping is permanent identity. Re-mapping a type, or binding a GUID to a second type, silently
   // corrupts the reverse lookup and makes already-persisted data resolve to the wrong type. This is the funnel every
   // mapping passes through — including across assemblies — so it is where both are rejected.
   static void AddMapping(Dictionary<Type, Guid> typeToGuid, Dictionary<Guid, Type> guidToType, Type type, Guid guid)
   {
      if(typeToGuid.TryGetValue(type, out var existingGuid))
         throw new InvalidOperationException(
            $"Type '{type.FullName}' is already mapped to GUID '{existingGuid}'. A type may be mapped only once.");

      if(guidToType.TryGetValue(guid, out var existingType))
         throw new InvalidOperationException(
            $"GUID '{guid}' is already mapped to type '{existingType.FullName}' and cannot also be mapped to '{type.FullName}'. Each type must have its own GUID.");

      typeToGuid.Add(type, guid);
      guidToType.Add(guid, type);
   }

   /// <summary>How one assembly's types get their persisted identity. Two declarations naming one assembly must agree.</summary>
   enum AssemblyTypeIdentityStrategy
   {
      /// <summary>The assembly's <see cref="IAssemblyTypeMapper"/> declares a GUID per type; persisted identity is that GUID.</summary>
      MappedToDeclaredGuids,

      /// <summary>The assembly's type names are persisted unchanged.</summary>
      StableTypeNames
   }
}
