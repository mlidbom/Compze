using Compze.Abstractions.Tessaging.Public;
using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(Compze.Abstractions.AssemblyTypeMapper))]

namespace Compze.Abstractions;

class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
   {
      map.Map<IExactlyOnceTevent>("0d68a831-87c0-4d05-8e52-bf063d51b56d")
         .Map<IRemotableTevent>("887aad71-52f3-46f8-a26a-e2886941758d")
         .Map<TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand>("f1581159-4ff1-4df5-b10b-0680552111b1")
         .Map<TessageTypes.Remotable.ExactlyOnce.Tommand>("fcf83cc4-de51-4b17-ab44-741c2d34feb2")
         .Map<Public.TentityId>("a1d63763-f934-493b-ae92-aeb2f15368b7")
         .Map<Public.TaggregateId>("5d87bfa3-5f88-4d3b-8971-c994757286ce")
         .Map<Public.TessageId>("0469d68b-776e-4844-8766-1cec0a563e9c")
         .MapOpenGeneric(typeof(TessageTypes.Remotable.NonTransactional.Tueries.TaggregateLink<>), "e0f1a2b3-c4d5-4e6f-7a8b-9c0d1e2f3a4b")
         .MapOpenGeneric(typeof(TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand<>), "f1a2b3c4-d5e6-4f7a-8b9c-0d1e2f3a4b5c")
         .Map<Hosting.Public.EndpointAddress>("f2a3b4c5-d6e7-4f80-91a2-b3c4d5e6f7a8");
   }
}
