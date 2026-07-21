using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers;
using Compze.TypeIdentifiers.DependencyInjection;

[assembly: AssemblyTypeMapper(typeof(Compze.Tessaging.InternalSpecifications.AssemblyTypeMapper))]

// ReSharper disable ClassNeverInstantiated.Global
namespace Compze.Tessaging.InternalSpecifications;

public class TueryAnswer
{
   public string Message { get; set; } = "";
}

public class TueryOnlyADownPeerServes : Remotable.NonTransactional.Tueries.Tuery<TueryAnswer>;

#pragma warning disable CA1812 // Instantiated via reflection through the [assembly: AssemblyTypeMapper(typeof(...))] attribute.
class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
      => map.Map<TueryOnlyADownPeerServes>("4b0e7d92-5c1a-4e83-9f27-6d38a0b45c19");
}

static class TessagingInternalSpecificationTypeMappings
{
   extension(IComponentRegistrar @this)
   {
      ///<summary>Requires the type identity of this internal-specification assembly — its test message types.</summary>
      public IComponentRegistrar RequireTessagingInternalSpecificationTypeMappings()
         => @this.RequireMappedTypesFromAssemblyContaining<AssemblyTypeMapper>();
   }
}
