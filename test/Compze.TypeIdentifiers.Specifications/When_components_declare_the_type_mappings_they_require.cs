using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Microsoft;
using Compze.Must;
using Compze.TypeIdentifiers.DependencyInjection;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052

namespace Compze.TypeIdentifiers.Specifications;

/// <summary>
/// A component declares the assemblies whose type identity it needs where it is registered, and the container composes
/// one <see cref="ITypeMap"/> covering every declaration. These specifications pin what that buys: declaration order
/// cannot change the resulting map, several components may need the same assembly, two components may not disagree
/// about how an assembly's types get their identity, and a cloned container composes its own map from its own
/// declarations rather than inheriting the source container's.
/// </summary>
public class When_components_declare_the_type_mappings_they_require
{
   static IContainerBuilder NewContainerBuilder() => new MicrosoftContainerBuilder(new ComponentRegistrar());

   static bool IsStableTypeString(string persistedTypeString) => !persistedTypeString.Contains(", 0", StringComparison.Ordinal);

   public class One_component_requiring_one_assembly : When_components_declare_the_type_mappings_they_require
   {
      readonly ITypeMap _typeMap;

      public One_component_requiring_one_assembly()
      {
         var builder = NewContainerBuilder();
         builder.Registrar.RequireMappedTypesFromAssemblyContaining<RegistrationTestEntity>();
         _typeMap = builder.Build().RootResolver.Resolve<ITypeMap>();
      }

      [XF] public void the_containers_type_map_covers_that_assemblys_types()
         => _typeMap.GetId(typeof(RegistrationTestEntity)).CanonicalString.Must().Contain(", 0");
   }

   public class Two_components_requiring_the_same_assembly : When_components_declare_the_type_mappings_they_require
   {
      readonly ITypeMap _typeMap;

      public Two_components_requiring_the_same_assembly()
      {
         var builder = NewContainerBuilder();
         builder.Registrar.RequireMappedTypesFromAssemblyContaining<RegistrationTestEntity>()
                          .RequireMappedTypesFromAssemblyContaining<RegistrationTestEntity>();
         _typeMap = builder.Build().RootResolver.Resolve<ITypeMap>();
      }

      [XF] public void the_duplicate_declaration_is_not_an_error()
         => _typeMap.GetId(typeof(RegistrationTestEntity)).CanonicalString.Must().Contain(", 0");
   }

   public class Two_components_disagreeing_about_one_assembly : When_components_declare_the_type_mappings_they_require
   {
      static IDependencyInjectionContainer ContainerWhoseComponentsDisagree()
      {
         var builder = NewContainerBuilder();
         builder.Registrar.RequireMappedTypesFromAssemblyContaining<RegistrationTestEntity>()
                          .RequireStableTypeNamesFromAssemblyContaining<RegistrationTestEntity>();
         return builder.Build();
      }

      [XF] public void composing_the_type_map_throws()
         => Invoking(() => ContainerWhoseComponentsDisagree().RootResolver.Resolve<ITypeMap>())
           .Must().Throw<Exception>();

      [XF] public void the_thrown_exception_names_the_assembly_they_disagree_about()
         => Invoking(() => ContainerWhoseComponentsDisagree().RootResolver.Resolve<ITypeMap>())
           .Must().Throw<Exception>()
           .Which.ToString().Must().Contain(typeof(RegistrationTestEntity).Assembly.GetName().Name!);
   }

   /// <summary>
   /// The reason the container registers a recipe for the map rather than a finished map: a clone re-runs the recipe
   /// against its own declarations. Were the source container to register the built map itself, every clone would
   /// silently share it and any declaration a clone added would be ignored.
   /// </summary>
   public class A_clone_of_a_container_that_declared_its_own_requirements : When_components_declare_the_type_mappings_they_require
   {
      readonly ITypeMap _sourceTypeMap;
      readonly ITypeMap _cloneTypeMap;

      public A_clone_of_a_container_that_declared_its_own_requirements()
      {
         var sourceBuilder = NewContainerBuilder();
         sourceBuilder.Registrar.RequireMappedTypesFromAssemblyContaining<RegistrationTestEntity>();
         var sourceContainer = sourceBuilder.Build();

         //A type from an assembly the source container never declared, so it can only reach a map through the clone's
         //own declaration. Stable-named because that assembly declares no type↔GUID mappings of its own.
         var cloneBuilder = sourceContainer.CreateCloneContainerBuilder();
         cloneBuilder.Registrar.RequireStableTypeNamesFromAssemblyContaining<ComponentRegistrar>();

         _sourceTypeMap = sourceContainer.RootResolver.Resolve<ITypeMap>();
         _cloneTypeMap = cloneBuilder.Build().RootResolver.Resolve<ITypeMap>();
      }

      [XF] public void composes_its_own_type_map_rather_than_sharing_the_sources()
         => ReferenceEquals(_cloneTypeMap, _sourceTypeMap).Must().BeFalse();

      [XF] public void covers_the_requirement_the_clone_itself_declared()
         => IsStableTypeString(_cloneTypeMap.GetId(typeof(ComponentRegistrar)).CanonicalString).Must().BeTrue();

      [XF] public void still_covers_the_requirements_inherited_from_the_source()
         => _cloneTypeMap.GetId(typeof(RegistrationTestEntity)).CanonicalString.Must().Contain(", 0");

      [XF] public void leaves_the_sources_own_map_without_the_clones_requirement()
         => _sourceTypeMap.TryGetId(typeof(ComponentRegistrar), out _).Must().BeFalse();
   }
}

/// <summary>
/// A built map is finished. Declaring more into the builder afterwards must not reach into it: the map has caches
/// computed against the mappings it was built with, so a mapping leaking in later would make an already-answered
/// lookup permanently wrong — the corruption an immutable map exists to make impossible.
/// </summary>
public class When_a_builder_is_used_again_after_building_a_map
{
   readonly ITypeMap _mapBuiltBeforeTheSecondDeclaration;

   public When_a_builder_is_used_again_after_building_a_map()
   {
      var builder = new TypeMapBuilder();
      _mapBuiltBeforeTheSecondDeclaration = builder.Build();
      //Answer a lookup, so the built map has a cached identity for this type, and only then declare the assembly.
      _mapBuiltBeforeTheSecondDeclaration.TryGetId(typeof(RegistrationTestEntity), out _);
      builder.MapTypesFromAssemblyContaining<RegistrationTestEntity>();
   }

   [XF] public void the_already_built_map_does_not_gain_the_later_declaration()
      => _mapBuiltBeforeTheSecondDeclaration.TryGetId(typeof(RegistrationTestEntity), out _).Must().BeFalse();
}
