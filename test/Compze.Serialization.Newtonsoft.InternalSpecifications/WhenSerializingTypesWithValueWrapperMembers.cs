using Compze.Abstractions;

using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable UnusedAutoPropertyAccessor.Local

// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMemberHierarchy.Global
// ReSharper disable MemberCanBeProtected.Global

namespace Compze.Serialization.Newtonsoft.InternalSpecifications
{
   public class When_serializing_types_with_ValueWrapper_members : SerializerTest
   {
      [PCTSerializer] public void Serialized_data_contains_type_information_only_when_the_type_differs_from_the_declared() =>
         Root.Create()
             ._(DocumentSerializer.Serialize)
             .Must()
             .Be("""
                 {
                   "PersonHoldingProperty": {
                     "Id": {
                       "Value": "312bdec4-3a0a-4186-b0e7-49efbcbe5ffc"
                     }
                   },
                   "UserHolderProperty": {
                     "$type": "fc967965-cab8-466c-b090-b0d9b3555708, 0",
                     "Id": {
                       "Value": "cf99c283-f5ee-48dd-9c19-98221be5fe9a"
                     }
                   },
                   "ListOfPersons": [
                     {
                       "Id": {
                         "Value": "6aa91672-8378-4a02-8d3f-1321989291db"
                       }
                     },
                     {
                       "$type": "fc967965-cab8-466c-b090-b0d9b3555708, 0",
                       "Id": {
                         "Value": "4c05d71d-41df-45ca-8a50-26e6456bc62f"
                       }
                     }
                   ]
                 }
                 """);

      [PCTSerializer] public void RoundTrippedObjectIsIdenticalToOriginal()
      {
         var original = Root.Create();

         var json = DocumentSerializer.Serialize(original);

         var roundTripped = DocumentSerializer.Deserialize<Root>(json);

         roundTripped.Must().DeepEqual(original);
      }

      ///<summary>An id type of a consumer's own, shaped as Compze's own are: no constructor that generates an id, and the one constructor's<br/>
      /// parameter named as the <see cref="Abstractions.Infrastructure.ValueWrapper{TValue}.Value"/> it sets.</summary>
      internal class PersonId(Guid value) : TentityId(value);

      internal class UserId(Guid value) : PersonId(value);

      internal class User(UserId id) : Person(id)
      {
         public override UserId Id => (UserId)base.Id;
      }

      internal class Person(PersonId id)
      {
         [Obsolete("for serializer")]
         public Person():this(new PersonId(Guid.NewGuid())) {}

         public virtual PersonId Id { get; private set; } = id;
      }

      class Root
      {
         internal static Root Create() => new()
                                          {
                                             PersonHoldingProperty = new Person(new PersonId(Guid.Parse("312bdec4-3a0a-4186-b0e7-49efbcbe5ffc"))),
                                             UserHolderProperty = new User(new UserId(Guid.Parse("cf99c283-f5ee-48dd-9c19-98221be5fe9a"))),
                                             ListOfPersons =
                                             [
                                                new Person(new PersonId(Guid.Parse("6aa91672-8378-4a02-8d3f-1321989291db"))),
                                                new User(new UserId(Guid.Parse("4c05d71d-41df-45ca-8a50-26e6456bc62f")))
                                             ]
                                          };

         public Person? PersonHoldingProperty { get; set; }
         public Person? UserHolderProperty { get; set; }

         public List<Person> ListOfPersons { get; set; } = [];
      }
   }
}
