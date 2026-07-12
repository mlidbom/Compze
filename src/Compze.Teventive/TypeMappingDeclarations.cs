using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.Tevents.Public;
using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(AssemblyTypeMapper))]

namespace Compze.Teventive;

#pragma warning disable CA1812 // Instantiated via reflection through the [assembly: AssemblyTypeMapper(typeof(...))] attribute.
class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
   {
      map.Map<TaggregateTevent>("32bbb393-64ab-42af-8edd-630d73d697a5")
         .Map<ITaggregateCreatedTevent>("af07f49c-12c6-4ea9-abf1-45fa2088515b")
         .Map<ITaggregateDeletedTevent>("bc662519-21b4-41b8-bce6-3714da82b1cc")
         .Map<ITaggregateTevent>("a1503d7d-51c0-4fff-ad3b-c7090f1e4905")
         .Map<IMutableTaggregateTevent>("befc4021-9e9c-4d40-842c-9878ce2c9ee3")
         .MapOpenGeneric(typeof(Taggregate<,,,,>), "e1f2a3b4-c5d6-4e7f-8091-a2b3c4d5e6f7")
         .MapOpenGeneric(typeof(PublisherIdentifyingTevent<>), "9c3ad661-f59c-42b6-b416-c38375eefc56")
         .MapOpenGeneric(typeof(TaggregateIdentifyingTevent<>), "063c6df4-48ec-4860-8c74-d0466a7858c3")
         .MapOpenGeneric(typeof(ITaggregateIdentifyingTevent<>), "9c783976-357f-466b-adaa-42812c3e0a65");
   }
}
