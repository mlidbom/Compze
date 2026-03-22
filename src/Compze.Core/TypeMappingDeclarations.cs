using Compze.TypeIdentifiers;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

[assembly: AssemblyTypeMapper(typeof(Compze.Core.AssemblyTypeMapper))]

namespace Compze.Core;

class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(ITypeMappingRegistrar map)
   {
      map.Map<TaggregateTevent>("32bbb393-64ab-42af-8edd-630d73d697a5")
         .Map<ITaggregateCreatedTevent>("af07f49c-12c6-4ea9-abf1-45fa2088515b")
         .Map<ITaggregateDeletedTevent>("bc662519-21b4-41b8-bce6-3714da82b1cc")
         .Map<ITaggregateTevent>("a1503d7d-51c0-4fff-ad3b-c7090f1e4905")
         .Map<IMutableTaggregateTevent>("befc4021-9e9c-4d40-842c-9878ce2c9ee3")
         .MapOpenGeneric(typeof(Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public.Taggregate<,,,,>), "e1f2a3b4-c5d6-4e7f-8091-a2b3c4d5e6f7")
         .Map<Compze.Core.Tessaging.Transport.Internal.EndPointAddress>("f2a3b4c5-d6e7-4f80-91a2-b3c4d5e6f7a8");
   }
}
