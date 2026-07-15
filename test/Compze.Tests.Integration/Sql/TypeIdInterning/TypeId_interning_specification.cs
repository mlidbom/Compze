using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Tests.Common.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.TypeIdentifiers;
using Compze.TypeIdentifiers.Interning;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Must;

using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Integration.Sql.TypeIdInterning;

/// <summary>
/// The interner persists a database-local <c>int</c> per conceptual type and links every persisted spelling of
/// that type to the one id. These specs resolve the engine-specific <see cref="ITypeIdInternerPersistence"/> for
/// the current matrix combo and layer separate interners over it, each with its own <see cref="ITypeMap"/>, to
/// simulate successive deployments that classify the same type differently against one shared database.
/// <para>The reclassification subject is a real mapped domain type. In one deployment its assembly is treated as
/// stable (it serialises as its assembly-qualified name); in another it keeps its declared GUID mapping (it
/// serialises as <c>"GUID, 0"</c>). The stable spelling resolves by reflection in any deployment, which is what
/// lets the two spellings be linked to one id.</para>
/// </summary>
public class TypeId_interning_specification : UniversalTestBase
{
   readonly IDependencyInjectionContainer _container = TestEnv.DIContainer.SetupTestingContainer(mapper => mapper.RegisterIntegrationTestTypeMappings());

   ITypeIdInternerPersistence Persistence => _container.Resolve<ITypeIdInternerPersistence>();

   protected override void DisposeInternal() => _container.Dispose();

   static ITypeMap StableDeployment()
   {
      var mapper = new TypeMapper();
      mapper.UseStableNameStrategyForAssemblyContaining<CQRS.UserRegistered>();
      return mapper;
   }

   static ITypeMap MappedDeployment()
   {
      var mapper = new TypeMapper();
      mapper.MapTypesFromAssemblyContaining<AssemblyTypeMapper>();
      return mapper;
   }

   ITypeIdInterner InternerOver(ITypeMap typeMap) => TypeIdInterner.For(Persistence, typeMap);

   static TypeId IdOf(ITypeMap map) => map.GetId(typeof(CQRS.UserRegistered));

   public class When_a_type_is_reclassified_from_stable_to_mapped : TypeId_interning_specification
   {
      [PCT] public void its_interned_id_is_unchanged()
      {
         var stableMap = StableDeployment();
         var idUnderStable = InternerOver(stableMap).GetOrInternId(IdOf(stableMap));

         var mappedMap = MappedDeployment();
         var idUnderMapped = InternerOver(mappedMap).GetOrInternId(IdOf(mappedMap));

         idUnderMapped.Must().Be(idUnderStable);
      }

      [PCT] public void data_written_under_the_stable_classification_still_resolves_after_the_change()
      {
         var stableMap = StableDeployment();
         var oldId = InternerOver(stableMap).GetOrInternId(IdOf(stableMap));

         // A fresh deployment with the type now mapped loads, links the new spelling, and resolves the id
         // stored under the old stable spelling back to the same .NET type.
         InternerOver(MappedDeployment()).GetTypeId(oldId).Type.Must().Be(typeof(CQRS.UserRegistered));
      }
   }

   public class When_a_stored_spelling_does_not_resolve_in_the_current_deployment : TypeId_interning_specification
   {
      // A type mapped to a GUID in one deployment, then read by a deployment that does not know that GUID
      // mapping (the signature of a mapping deployed together with — or removed before — the data that needs it).
      [PCT] public void resolving_its_id_throws_an_actionable_error()
      {
         var mappedMap = MappedDeployment();
         var id = InternerOver(mappedMap).GetOrInternId(IdOf(mappedMap));

         Invoking(() => InternerOver(StableDeployment()).GetTypeId(id)).Must().Throw<InvalidOperationException>();
      }
   }

   public class When_an_id_was_never_interned : TypeId_interning_specification
   {
      [PCT] public void resolving_it_throws() =>
         Invoking(() => InternerOver(StableDeployment()).GetTypeId(999999)).Must().Throw<InvalidOperationException>();
   }
}
