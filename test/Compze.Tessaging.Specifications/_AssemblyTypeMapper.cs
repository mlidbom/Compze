using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Specifications.ExactlyOnce;
using Compze.Tessaging.Specifications.Typermedia;
using Compze.TypeIdentifiers;
using Compze.TypeIdentifiers.DependencyInjection;

[assembly: AssemblyTypeMapper(typeof(Compze.Tessaging.Specifications.AssemblyTypeMapper))]

namespace Compze.Tessaging.Specifications;

///<summary>The type identity of this specification assembly's tessage types: an assembly declares exactly one mapper.</summary>
#pragma warning disable CA1812 // Instantiated via reflection through the [assembly: AssemblyTypeMapper(typeof(...))] attribute.
class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
      => map.Map<GreetingTuery>("7e8158a9-4972-4bd3-a0a5-9b261e7e5e64")
            .Map<RegisterGreeterTommand>("8b7a82f3-9c2e-4f24-9a96-2f0d2af56a01")
            .Map<TueryBothEndpointsHandle>("c93a55b7-30a1-4a35-b7c8-01c9d61d31e6")
            .Map<TueryOnlyTheSecondEndpointHandles>("5f2f0f68-8a4e-4a2e-b7a2-38d8bd7fca10")
            .Map<TueryServedByTheLateEndpoint>("2a6de5cf-40b8-4f11-9c76-8f0e2f5f13b2")
            .Map<TueryNothingServes>("9d41c2b7-6a35-4a90-8f3e-5b7c1d20ae44")
            .Map<TheAnswerTuery>("6c15f9a3-2d70-4e81-9b4c-73a0e5182fd6")
            .Map<AddTuery>("0b83d4e6-51c9-4a27-8e60-2f9d71c34b58")
            .Map<RememberNumberTommand>("f27a061b-9e34-4c85-b1d0-58e63a2790cf")
            .Map<NumberedExactlyOnceTommand>("5c9e1f74-3a28-4b6d-9e05-b7d41a26c83f");
}
#pragma warning restore CA1812

public static class TessagingSpecificationTypeMappings
{
   /// <summary>Requires the type identity of this specification assembly — its test message types.</summary>
   public static IComponentRegistrar RequireTessagingSpecificationTypeMappings(this IComponentRegistrar @this)
      => @this.RequireMappedTypesFromAssemblyContaining<AssemblyTypeMapper>();
}
