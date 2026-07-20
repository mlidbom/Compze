using Compze.Contracts;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.Abstractions.TessageTypes;

namespace Compze.Tessaging.Endpoints;

///<summary>The tessage types a readiness await names: "complete when this endpoint can reach handlers for these types"<br/>
/// (<see cref="IEndpoint.AwaitReadinessAsync"/>). A set contains only remotable single-handler tessage types — tueries,<br/>
/// typermedia tommands, exactly-once tommands — because those are the kinds for which "a handler is available" means<br/>
/// anything: a tevent is multi-subscriber, has no one handler to await, and its delivery is fully served by the peer<br/>
/// topology's queue-while-down machinery. Anything else in a set fails loud here, at composition — never as a timeout later.</summary>
///<remarks>The reflection factories exist because hand-enumerating types has a classic failure mode: the one forgotten type<br/>
/// that surfaces as a patience-exhausted timeout in production. Reflect over the assembly or namespace the types live in —<br/>
/// the same idiom type-id mapping uses (<c>MapTypesFromAssemblyContaining</c>) — and every type is covered, including the<br/>
/// ones added after the readiness await was written.</remarks>
public class ReadinessTypes
{
   ///<summary>The remotable single-handler tessage types this readiness await covers.</summary>
   public IReadOnlyList<Type> Types { get; }

   ReadinessTypes(IReadOnlyList<Type> types) => Types = types;

   ///<summary>Every remotable single-handler tessage type in the assembly containing <typeparamref name="TTessage"/> —<br/>
   /// tueries, typermedia tommands, exactly-once tommands; every other type in the assembly is simply not a readiness<br/>
   /// concern and is filtered out. An assembly containing none fails loud: awaiting readiness for nothing is a<br/>
   /// composition error, not an instantly-ready endpoint.</summary>
   public static ReadinessTypes InAssemblyContaining<TTessage>()
   {
      var assembly = typeof(TTessage).Assembly;
      var types = assembly.GetTypes().Where(IsARemotableSingleHandlerTessageType).ToList();
      State.Assert(types.Count > 0, () => $"The assembly {assembly.GetName().Name} contains no remotable single-handler tessage types (tueries, typermedia tommands, exactly-once tommands) — awaiting readiness for nothing is a composition error.");
      return new([..types]);
   }

   ///<summary>Every remotable single-handler tessage type in <typeparamref name="TTessage"/>'s namespace subtree: starting<br/>
   /// from <typeparamref name="TTessage"/>'s own namespace, walk up <paramref name="levelsToWalkUpBeforeRecursingDown"/><br/>
   /// namespace levels, then include every qualifying type in that namespace and every namespace below it (within<br/>
   /// <typeparamref name="TTessage"/>'s assembly). A subtree containing none fails loud, exactly like<br/>
   /// <see cref="InAssemblyContaining{TTessage}"/>.</summary>
   public static ReadinessTypes InNamespaceOf<TTessage>(int levelsToWalkUpBeforeRecursingDown = 0)
   {
      var tessageNamespace = typeof(TTessage).Namespace;
      State.NotNull(tessageNamespace);
      var namespaceLevels = tessageNamespace.Split('.');
      State.Assert(levelsToWalkUpBeforeRecursingDown < namespaceLevels.Length,
                   () => $"Walking up {levelsToWalkUpBeforeRecursingDown} levels from the namespace {tessageNamespace} walks above the namespace root.");
      var rootNamespace = string.Join('.', namespaceLevels[..^levelsToWalkUpBeforeRecursingDown]);

      var types = typeof(TTessage).Assembly.GetTypes()
                                  .Where(type => type.Namespace is {} typeNamespace && (typeNamespace == rootNamespace || typeNamespace.StartsWith(rootNamespace + ".", StringComparison.Ordinal)))
                                  .Where(IsARemotableSingleHandlerTessageType)
                                  .ToList();
      State.Assert(types.Count > 0, () => $"The namespace subtree {rootNamespace} contains no remotable single-handler tessage types (tueries, typermedia tommands, exactly-once tommands) — awaiting readiness for nothing is a composition error.");
      return new([..types]);
   }

   ///<summary>An explicitly enumerated set. Every type must be a remotable single-handler tessage type — a tuery, a<br/>
   /// typermedia tommand, or an exactly-once tommand — and anything else fails loud here, at composition. Prefer the<br/>
   /// reflection factories: an explicit list rots by omission.</summary>
   public static ReadinessTypes These(params Type[] tessageTypes)
   {
      State.Assert(tessageTypes.Length > 0, () => "Awaiting readiness for nothing is a composition error.");
      foreach(var tessageType in tessageTypes)
         State.Assert(IsARemotableSingleHandlerTessageType(tessageType),
                      () => $"{tessageType.GetFullNameCompilable()} is not a remotable single-handler tessage type. Readiness is awaited on concrete tueries, typermedia tommands, and exactly-once tommands — tevents are multi-subscriber, have no one handler to await, and their delivery is fully served by the peer topology's queue-while-down machinery.");
      return new([..tessageTypes]);
   }

   static bool IsARemotableSingleHandlerTessageType(Type type) =>
      type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
      && (type.Is<IExactlyOneReceiverTessage>() && type.Is<IRemotableTessage>());
}
