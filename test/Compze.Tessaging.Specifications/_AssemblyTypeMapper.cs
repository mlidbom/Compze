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
            .Map<NumberedExactlyOnceTommand>("5c9e1f74-3a28-4b6d-9e05-b7d41a26c83f")
            .Map<TessageBus.TommandWhoseHandlerWaitsForItsCancellation>("6560d06b-b249-4f51-916b-ec0386e77808")
            .Map<TessageBus.TommandNoEndpointHandles>("d9bed99c-017b-4b48-a973-755a614a4f92")
            .Map<TessageBus.DeferredStep>("3b1f6c2e-8d47-4a95-b0e3-6f24c9a81d57")
            .Map<TessageBus.StepGivingUpItsPlace>("085b5d1b-3d83-4072-8517-66c8d44bdd0a")
            .Map<TessageBus.StepKeepingItsPlace>("cb47dbf4-2fab-4f73-80e8-a098b6065f83")
            .Map<TessageBus.TommandGivingUpItsPlace>("fae88462-9233-4f6a-b448-b106d49f7d75")
            .Map<TessageBus.ITeventWhoseHandlersAllGiveUpTheirPlace>("15f99cbf-77d1-4234-9d2f-edff852829d1")
            .Map<TessageBus.TeventWhoseHandlersAllGiveUpTheirPlace>("5e55310c-b25a-4214-bce1-5a9fcabea642")
            .Map<TessageBus.ITeventOneOfWhoseHandlersKeepsItsPlace>("f9118d8f-1274-431f-8719-a60e5f2378a3")
            .Map<TessageBus.TeventOneOfWhoseHandlersKeepsItsPlace>("b7c62b65-2d64-4309-ae6e-0be750bdaf6f")
            .Map<TessageBus.StepWithATransactionTimeoutOfASecond>("f4d27090-4584-4f44-8d06-f4d81957268c")
            .Map<TessageBus.TommandWithATransactionTimeoutOfASecond>("77a6f7a8-bc82-4218-82ea-56cd628848f0")
            .Map<TessageBus.ITeventWhoseHandlersAllHaveATransactionTimeoutOfASecond>("1dbab4ad-b678-417d-bab3-9f7146bf4b49")
            .Map<TessageBus.TeventWhoseHandlersAllHaveATransactionTimeoutOfASecond>("22ab50b9-8f43-456e-9157-c8018fae3801")
            .Map<TessageBus.ITeventOneOfWhoseHandlersHasATransactionTimeoutOfASecond>("066e0844-517e-4d5a-949b-240b04b5ff22")
            .Map<TessageBus.TeventOneOfWhoseHandlersHasATransactionTimeoutOfASecond>("190892b5-f650-4d7e-9529-440585254f99");
}
#pragma warning restore CA1812

public static class TessagingSpecificationTypeMappings
{
   /// <summary>Requires the type identity of this specification assembly — its test message types.</summary>
   public static IComponentRegistrar RequireTessagingSpecificationTypeMappings(this IComponentRegistrar @this)
      => @this.RequireMappedTypesFromAssemblyContaining<AssemblyTypeMapper>();
}
