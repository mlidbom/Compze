using System;
using System.Collections.Generic;
using Compze.Abstractions;
using JetBrains.Annotations;

namespace Compze.Tests.Common.Persistence.DocumentDb;

public class Dog : IPersistentEntity
{
   public Guid Id { get; set; }
   public string Name { get; [UsedImplicitly] set; } = "John Doe Doggy";
}

public class Person : PersistentEntity<Person>
{
   public Person() {}
   public Person(Guid id): base(id) {}
}

public class User : Person
{
   public User(){}
   public User(Guid id): base(id) {}
   public string Email { get; set; } = "some.email@nodomain.not";
   public string Password { get; set; } = "default";

   public Address Address { get; set; } = new();

   public HashSet<User> People { get; set; } = [];
}

public record Address
{
   public string Street { [UsedImplicitly] get; set; } = "SomeStreet";
   public int Streetnumber { [UsedImplicitly] get; set; } = 12;
   public string City { [UsedImplicitly] get; set; } = "SomeCity";
}

public record Email(string TheEmail)
{
   public string TheEmail { get; private set; } = TheEmail;
}
