using System;
using System.Collections.Generic;
using Compze.Core.Public.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Tests.Infrastructure.FluentAssertionsExtensions;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.Functional;
using FluentAssertions;

// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Compze.Tests.Unit.Internals.Serialization
{
   public class WhenSerializingTypesWithValueWrapperMembers : SerializerTest
   {
      [PCTSerializer] public void SerializedDataIs() =>
         Root.Create()
             ._(DocumentSerializer.Serialize)
             .Must()
             .Equal("""
                    {
                      "PersonHoldingProperty": {
                        "Id": "312bdec4-3a0a-4186-b0e7-49efbcbe5ffc"
                      },
                      "UserHolderProperty": {
                        "$type": "fc967965-cab8-466c-b090-b0d9b3555708",
                        "Id": "cf99c283-f5ee-48dd-9c19-98221be5fe9a"
                      },
                      "ListOfPersons": [
                        {
                          "Id": "6aa91672-8378-4a02-8d3f-1321989291db"
                        },
                        {
                          "$type": "fc967965-cab8-466c-b090-b0d9b3555708",
                          "Id": "4c05d71d-41df-45ca-8a50-26e6456bc62f"
                        }
                      ]
                    }
                    """);

      [PCTSerializer] public void RoundTrippedObjectIsIdenticalToOriginal()
      {
         var original = Root.Create();

         var json = DocumentSerializer.Serialize(original);

         var roundTripped = DocumentSerializer.Deserialize<Root>(json);

         roundTripped.Should().BeStrictlyEquivalentTo(original);
      }

      internal class PersonId(Guid id) : TentityId(id);
      internal class Person(PersonId id)
      {
         public virtual PersonId Id { get; } = id;
      }

      internal class UserId(Guid id) : PersonId(id);
      internal class User(UserId id) : Person(id)
      {
         public override UserId Id => (UserId)base.Id;
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
