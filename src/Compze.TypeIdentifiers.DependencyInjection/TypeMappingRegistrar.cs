using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.TypeIdentifiers.DependencyInjection;

/// <summary>
/// How a component declares the type identity it depends on. A component that persists or transmits types from some
/// assembly says so here — <c>registrar.RequireMappedTypesFromAssemblyContaining&lt;ITessage&gt;()</c> — and the
/// container composes one <see cref="ITypeMap"/> covering every such declaration.
/// </summary>
/// <remarks>
/// This exists because "which assemblies must be mapped" is a dependency like any other, and belongs to the component
/// that has it. Threading a mutable mapper through composition roots put the knowledge in the wrong place: every
/// composition root had to know, and repeat, what its components needed, and the resulting map depended on who
/// registered first.
/// </remarks>
/// <remarks>
/// Declaring the same assembly from several components is expected and costs nothing. Declaring it two <em>different</em>
/// ways — mapped by one component, stable-named by another — throws when the map is built: the two disagree about what
/// that assembly's persisted type identity is.
/// </remarks>
public static class TypeMappingRegistrar
{
   extension(IComponentRegistrar @this)
   {
      /// <summary>
      /// Declares that this container's <see cref="ITypeMap"/> must cover the types of the assembly containing
      /// <typeparamref name="T"/>, mapped to the GUIDs that assembly declares through its <see cref="IAssemblyTypeMapper"/>.
      /// </summary>
      /// <typeparam name="T">Any type from the required assembly. Pick one that will not move: the declaration follows the
      /// type, so a <typeparamref name="T"/> that later moves to another assembly silently repoints this requirement.</typeparam>
      public IComponentRegistrar RequireMappedTypesFromAssemblyContaining<T>() =>
         @this.Require(TypeMappingRequirement.MappedTypesFromAssemblyContaining<T>());

      /// <summary>
      /// Declares that this container's <see cref="ITypeMap"/> must treat the assembly containing <typeparamref name="T"/>
      /// as stable: its type names are persisted unchanged instead of being replaced by GUIDs. Suitable only for assemblies
      /// whose type names will not be renamed or moved, because a persisted name that later changes no longer resolves.
      /// </summary>
      /// <typeparam name="T">Any type from the required assembly — see <see cref="RequireMappedTypesFromAssemblyContaining{T}"/>.</typeparam>
      public IComponentRegistrar RequireStableTypeNamesFromAssemblyContaining<T>() =>
         @this.Require(TypeMappingRequirement.StableTypeNamesFromAssemblyContaining<T>());

      IComponentRegistrar Require(TypeMappingRequirement requirement)
      {
         @this.RegisterTypeMap();
         return @this.Register(Singleton.ForSet<TypeMappingRequirement>().Instance(requirement));
      }

      /// <summary>
      /// Installs the container's one <see cref="ITypeMap"/> composition, on whichever requirement happens to be declared
      /// first.
      /// </summary>
      /// <remarks>
      /// Which requirement wins the race is irrelevant, and that is the point: what gets registered is not a map but a
      /// <em>recipe</em> that reads the whole finished requirement set when the container builds the map. Every declaration
      /// made before then is included no matter what order they arrived in. (A container that already carries an
      /// <see cref="ITypeMap"/> — a clone of one that composed its own, or an application that registered a hand-built map —
      /// keeps it.)
      /// </remarks>
      public IComponentRegistrar RegisterTypeMap()
      {
         if(@this.IsRegistered<ITypeMap>()) return @this;

         return @this.Register(Singleton.For<ITypeMap>()
                                        .CreatedBy((IComponentSet<TypeMappingRequirement> requirements) => BuildTypeMapFrom(requirements)));
      }
   }

   static ITypeMap BuildTypeMapFrom(IComponentSet<TypeMappingRequirement> requirements)
   {
      var builder = new TypeMapBuilder();
      foreach(var requirement in requirements)
         requirement.DeclareInto(builder);

      return builder.Build();
   }
}
