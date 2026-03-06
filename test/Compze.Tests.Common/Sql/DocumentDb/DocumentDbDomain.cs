using Compze.Abstractions.Public;
using JetBrains.Annotations;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Compze.Tests.Common.Sql.DocumentDb;

public class Dog : IEntity
{
   public EntityId Id { get; set; } = new();
   public string Name { get; [UsedImplicitly] set; } = "John Doe Doggy";
}

public class Person : Entity<Person>
{
   public Person() {}
   protected Person(Guid id): base(id) {}
}

public class User : Person
{
   public User(){}
   public User(Guid id): base(id) {}
   public string Email { get; set; } = "some.email@nodomain.not";
   public string Password { get; set; } = "default";

   public Address Address { get; set; } = new();

#pragma warning disable CA2227
   public HashSet<User> People { get; set; } = [];
#pragma warning restore CA2227
}

public record Address
{
   public string Street { [UsedImplicitly] get; set; } = "SomeStreet";
   public int StreetNumber { [UsedImplicitly] get; set; } = 12;
   public string City { [UsedImplicitly] get; set; } = "SomeCity";
}

public record Email(string TheEmail)
{
   public string TheEmail { get; private set; } = TheEmail;
}
