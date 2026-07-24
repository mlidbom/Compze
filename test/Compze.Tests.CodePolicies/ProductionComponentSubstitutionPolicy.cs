using System.Reflection;
using System.Runtime.CompilerServices;
using Compze.Internals.SystemCE;
using Compze.Must;
using Compze.Tests.CodePolicies.Infrastructure;
using Compze.xUnitBDD;

namespace Compze.Tests.CodePolicies;

///<summary>Enforces the use-the-real-thing half of the black-box-testing strategy
/// (<c>.claude-shared/rules/path-scoped/csharp-bdd-specifications.md</c>: "All production components that can work in tests SHOULD
/// be used in the tests, not be replaced by test doubles"): a test project may not declare its own implementation of an abstraction
/// Compze itself implements.</summary>
///<remarks>A test double for a component Compze ships is not a shortcut, it is a different system under test. The specification then
/// exercises wiring that exists nowhere in production, and the double tends to grow observation and synchronization the real
/// component does not have — so the specification ends up asserting against, and timing itself by, machinery no consumer will ever
/// run. The rule that catches this is not <see cref="TestProjectInternalsVisibleToPolicy"/>: substituting a component needs no
/// <see cref="InternalsVisibleToAttribute"/> grant at all, because the seam it goes through is public by design.</remarks>
///<remarks>The discriminator is whether Compze implements the abstraction. An abstraction Compze declares but never implements is a
/// pure extension point, and a test implementing one is doing exactly what a consumer's application does. An abstraction Compze
/// <em>does</em> implement is a component Compze owns and composes, so a second implementation declared by a test stands in for the
/// real one. Compze also implements some of the abstractions it invites consumers to implement — a tessage, a taggregate, an
/// endpoint environment — so those are named in <see cref="ExtensionPointsConsumersImplement"/>, which is a statement about the
/// framework's design and not a list of tolerated violations. The fix for a violation is to compose the shipped implementation, and
/// — when the specification needs to observe or await something that implementation does not expose — to add that observation to the
/// production abstraction, where the consumers who need the same thing can reach it too.</remarks>
public static partial class ProductionComponentSubstitutionPolicy
{
   [XF] public static void No_test_project_declares_its_own_implementation_of_an_abstraction_Compze_implements()
   {
      CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

      var abstractionsCompzeImplements = AbstractionsCompzeImplements();

      var violations = TestAssemblies()
                      .SelectMany(assembly => assembly.GetTypes()
                                                      .Where(type => !type.IsInterface
                                                                  && !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
                                                                  && !KnownViolations.TestDoublesForProductionComponents.Contains(type.FullName))
                                                      .SelectMany(type => SubstitutedComponentsOf(type, abstractionsCompzeImplements)))
                      .Distinct()
                      .Order(StringComparer.Ordinal)
                      .ToList();

      violations.Must().SequenceEqual(Array.Empty<string>());
   }

   ///<summary>The abstractions Compze declares for a consumer's own application to implement, each named by the root of its family
   /// — everything deriving from a root is an extension point too. Compze implements these itself as well, so without naming them
   /// every specification that models a domain or configures an endpoint would read as a substitution.</summary>
   static readonly Type[] ExtensionPointsConsumersImplement =
   [
      typeof(Tessaging.TessageTypes.ITessage),                        // every tevent, tommand and tuery
      typeof(Abstractions.IEntity<>),                                 // taggregates and the entities within them
      typeof(Abstractions.Infrastructure.ValueWrapper<>),             // the ids and value objects entities are built from
      typeof(Teventive.Taggregates.BaseClasses.Shared.SharedTomponent<>), // the components shared between taggregates
      typeof(Tessaging.Endpoints.IEndpointEnvironment),               // what an endpoint's surroundings supply: transport, serializer, databases
      typeof(TypeIdentifiers.IAssemblyTypeMapper),                    // an assembly's declaration of the persistent names of its types
      typeof(Abstractions.Configuration.IConfigurationParameterProvider), // where configuration parameters are read from
      typeof(DbPool.SystemCE.IStrictlyManagedResource),               // a resource whose disposal the consumer wants policed
      typeof(Threading.Interprocess.ISignalPollingPolicy),            // how often a waiter re-checks an interprocess signal
      typeof(DependencyInjection.Abstractions.IComponentSet<>),       // a set of components registered together
      typeof(Internals.Logging.Logger),                               // the logging backend log statements are written to
      typeof(Internals.SystemCE.IStaticInstancePropertySingleton<>),  // a type reached through a static Instance property
      typeof(InterprocessObject.IInterprocessObjectSerializer<>)      // how an interprocess object crosses the process boundary
   ];

   static IEnumerable<string> SubstitutedComponentsOf(Type testType, Dictionary<Type, Type> abstractionsCompzeImplements)
   {
      var implementedAbstractions = CompzeAbstractionsImplementedBy(testType).ToList();
      if(implementedAbstractions.Intersect(ExtensionPointsConsumersImplement).Any()) return [];

      return MostDerived(implementedAbstractions)
            .Where(abstractionsCompzeImplements.ContainsKey)
            .Select(abstraction => $"{testType.FullName} implements {abstraction.FullName}, which Compze implements: {abstractionsCompzeImplements[abstraction].FullName}");
   }

   ///<summary>The abstractions in <paramref name="abstractions"/> that no other abstraction in the set already derives from — the
   /// ones that actually say what the type is. Reporting the whole inheritance closure would bury each violation under the dozen
   /// base interfaces it drags along.</summary>
   static IEnumerable<Type> MostDerived(IReadOnlyList<Type> abstractions) =>
      abstractions.Where(abstraction => !abstractions.Any(other => other != abstraction && CompzeAbstractionsImplementedBy(other).Contains(abstraction)));

   ///<summary>The loaded assemblies built from a <c>test/</c> project — the ones this policy governs. Everything else is either a
   /// shipped library or a sample.</summary>
   static IEnumerable<Assembly> TestAssemblies() =>
      AppDomain.CurrentDomain.AllCompzeAssemblies().Where(assembly => CompzeRepository.IsTestProject(assembly.GetName().Name!));

   ///<summary>Every abstraction declared by a Compze library that a Compze library also implements, mapped to one such
   /// implementation so a violation can name the component the test is standing in for.</summary>
   static Dictionary<Type, Type> AbstractionsCompzeImplements()
   {
      var abstractionsCompzeImplements = new Dictionary<Type, Type>();
      foreach(var implementation in AppDomain.CurrentDomain.AllCompzeLibraryTypes().Where(type => type is { IsAbstract: false, IsInterface: false }))
         foreach(var abstraction in CompzeAbstractionsImplementedBy(implementation))
            abstractionsCompzeImplements.TryAdd(abstraction, implementation);

      return abstractionsCompzeImplements;
   }

   ///<summary>Every interface and base class of <paramref name="type"/> that a Compze library declares, generic types reduced to
   /// their generic type definition so an implementation of <c>ISomething&lt;TThing&gt;</c> is recognised as an implementation of
   /// <c>ISomething&lt;&gt;</c>.</summary>
   static IEnumerable<Type> CompzeAbstractionsImplementedBy(Type type) =>
      type.GetInterfaces()
          .Concat(BaseClassesOf(type))
          .Where(IsDeclaredByACompzeLibrary)
          .Select(abstraction => abstraction.IsGenericType ? abstraction.GetGenericTypeDefinition() : abstraction);

   static IEnumerable<Type> BaseClassesOf(Type type)
   {
      for(var baseClass = type.BaseType; baseClass is not null && baseClass != typeof(object); baseClass = baseClass.BaseType)
         yield return baseClass;
   }

   static bool IsDeclaredByACompzeLibrary(Type type)
   {
      var declaringAssembly = type.Assembly.GetName().Name!;
      return declaringAssembly.StartsWithOrdinal("Compze.") && !CompzeRepository.IsTestProject(declaringAssembly);
   }
}
