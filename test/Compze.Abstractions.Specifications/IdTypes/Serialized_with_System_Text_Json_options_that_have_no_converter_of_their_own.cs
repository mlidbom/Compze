using System.Text.Json;
using System.Text.Json.Serialization;
using Compze.Contracts.Exceptions;
using Compze.Must;
using Compze.Tessaging;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

#pragma warning disable CA1052 //I should make an inherited class static? Really?

namespace Compze.Abstractions.Specifications.IdTypes;

public class Serialized_with_System_Text_Json_options_that_have_no_converter_of_their_own
{
   static readonly JsonSerializerOptions Options = new();

   static readonly Guid FirstGuid = Guid.Parse("10000000-0000-0000-0000-000000000000");
   static readonly Guid SecondGuid = Guid.Parse("20000000-0000-0000-0000-000000000000");
   static readonly Guid ThirdGuid = Guid.Parse("30000000-0000-0000-0000-000000000000");
   static readonly Guid FourthGuid = Guid.Parse("40000000-0000-0000-0000-000000000000");
   static readonly Guid FifthGuid = Guid.Parse("50000000-0000-0000-0000-000000000000");

   public class an_object_holding_one_of_each_of_compzes_id_types : Serialized_with_System_Text_Json_options_that_have_no_converter_of_their_own
   {
      readonly OneOfEachOfCompzesIdTypes _original = new(new EntityId<Guid>(FirstGuid), new EntityId(SecondGuid), new TentityId(ThirdGuid), new TaggregateId(FourthGuid), new TessageId(FifthGuid));
      readonly string _json;

      public an_object_holding_one_of_each_of_compzes_id_types() => _json = JsonSerializer.Serialize(_original, Options);

      [XF] public void is_written_with_each_id_as_the_string_of_its_guid() =>
         _json.Must().Be("""{"EntityIdOfAGuid":"10000000-0000-0000-0000-000000000000","EntityId":"20000000-0000-0000-0000-000000000000","TentityId":"30000000-0000-0000-0000-000000000000","TaggregateId":"40000000-0000-0000-0000-000000000000","TessageId":"50000000-0000-0000-0000-000000000000"}""");

      public class and_read_back : an_object_holding_one_of_each_of_compzes_id_types
      {
         readonly OneOfEachOfCompzesIdTypes _readBack;

         public and_read_back() => _readBack = JsonSerializer.Deserialize<OneOfEachOfCompzesIdTypes>(_json, Options)!;

         [XF] public void holds_each_id_it_was_written_with() => _readBack.Ids.Must().SequenceEqual(_original.Ids);
      }
   }

   public class a_dictionary_keyed_by_TaggregateId : Serialized_with_System_Text_Json_options_that_have_no_converter_of_their_own
   {
      readonly Dictionary<TaggregateId, string> _original = new() { [new TaggregateId(FirstGuid)] = "taberu" };
      readonly string _json;

      public a_dictionary_keyed_by_TaggregateId() => _json = JsonSerializer.Serialize(_original, Options);

      [XF] public void is_written_with_each_key_as_the_string_of_its_guid() => _json.Must().Be("""{"10000000-0000-0000-0000-000000000000":"taberu"}""");

      public class and_read_back : a_dictionary_keyed_by_TaggregateId
      {
         readonly Dictionary<TaggregateId, string> _readBack;

         public and_read_back() => _readBack = JsonSerializer.Deserialize<Dictionary<TaggregateId, string>>(_json, Options)!;

         [XF] public void holds_the_key_and_value_it_was_written_with() => _readBack.Single().Must().Be(_original.Single());
      }
   }

   public class an_id_type_of_a_consumers_own_that_carries_the_EntityIdJsonConverter : Serialized_with_System_Text_Json_options_that_have_no_converter_of_their_own
   {
      public class derived_from_TaggregateId : an_id_type_of_a_consumers_own_that_carries_the_EntityIdJsonConverter
      {
         readonly NoteId _original = new(FirstGuid);
         readonly string _json;

         public derived_from_TaggregateId() => _json = JsonSerializer.Serialize(_original, Options);

         [XF] public void is_written_as_the_string_of_its_guid() => _json.Must().Be("\"10000000-0000-0000-0000-000000000000\"");

         [XF] public void is_read_back_as_the_id_it_was_written_from() => JsonSerializer.Deserialize<NoteId>(_json, Options).Must().Be(_original);
      }

      public class derived_from_EntityId_of_an_int : an_id_type_of_a_consumers_own_that_carries_the_EntityIdJsonConverter
      {
         readonly OrderNumber _original = new(42);
         readonly string _json;

         public derived_from_EntityId_of_an_int() => _json = JsonSerializer.Serialize(_original, Options);

         [XF] public void is_written_as_its_int() => _json.Must().Be("42");

         [XF] public void is_read_back_as_the_id_it_was_written_from() => JsonSerializer.Deserialize<OrderNumber>(_json, Options).Must().Be(_original);
      }

      public class that_has_no_constructor_taking_its_guid_alone : an_id_type_of_a_consumers_own_that_carries_the_EntityIdJsonConverter
      {
         [XF] public void serializing_it_throws_naming_the_id_type() =>
            Invoking(() => JsonSerializer.Serialize(new NoteIdParsedFromText(FirstGuid.ToString()), Options)).Must().Throw<ArgumentAssertionFailedException>()
                                                                                                      .Which.Message.Must().Contain(nameof(NoteIdParsedFromText));
      }
   }

   class OneOfEachOfCompzesIdTypes(EntityId<Guid> entityIdOfAGuid, EntityId entityId, TentityId tentityId, TaggregateId taggregateId, TessageId tessageId)
   {
      public EntityId<Guid> EntityIdOfAGuid { get; } = entityIdOfAGuid;
      public EntityId EntityId { get; } = entityId;
      public TentityId TentityId { get; } = tentityId;
      public TaggregateId TaggregateId { get; } = taggregateId;
      public TessageId TessageId { get; } = tessageId;

      [JsonIgnore] public IReadOnlyList<EntityId<Guid>> Ids => [EntityIdOfAGuid, EntityId, TentityId, TaggregateId, TessageId];
   }

   [JsonConverter(typeof(EntityIdJsonConverter))]
   class NoteId(Guid value) : TaggregateId(value);

   [JsonConverter(typeof(EntityIdJsonConverter))]
   class OrderNumber(int value) : EntityId<int>(value);

   [JsonConverter(typeof(EntityIdJsonConverter))]
   class NoteIdParsedFromText(string text) : TaggregateId(Guid.Parse(text));
}
