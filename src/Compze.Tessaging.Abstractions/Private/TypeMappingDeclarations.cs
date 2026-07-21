using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(Compze.Tessaging.Private.AssemblyTypeMapper))]

namespace Compze.Tessaging.Private;

#pragma warning disable CA1812 // Instantiated via reflection through the [assembly: AssemblyTypeMapper(typeof(...))] attribute.
class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
   {
      map.Map<IExactlyOnceTevent>("0d68a831-87c0-4d05-8e52-bf063d51b56d")
         .Map<IRemotableTevent>("887aad71-52f3-46f8-a26a-e2886941758d")
         .Map<Remotable.AtMostOnce.AtMostOnceTypermediaTommand>("f1581159-4ff1-4df5-b10b-0680552111b1")
         .Map<Remotable.ExactlyOnce.Tommand>("fcf83cc4-de51-4b17-ab44-741c2d34feb2")
         .Map<TessageId>("0469d68b-776e-4844-8766-1cec0a563e9c")
         .MapOpenGeneric(typeof(Remotable.NonTransactional.Tueries.TaggregateLink<>), "e0f1a2b3-c4d5-4e6f-7a8b-9c0d1e2f3a4b")
         .MapOpenGeneric(typeof(Remotable.AtMostOnce.AtMostOnceTypermediaTommand<>), "f1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5c")
         .MapOpenGeneric(typeof(IPublisherTevent<>), "a6907e78-2e6c-4674-a426-78008791b0a0")
         .MapOpenGeneric(typeof(PublisherTevent<>), "9c3ad661-f59c-42b6-b416-c38375eefc56");
   }
}
