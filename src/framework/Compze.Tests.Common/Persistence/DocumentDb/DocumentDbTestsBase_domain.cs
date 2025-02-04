using System;
using System.Collections.Generic;
using Compze.DDD;
using JetBrains.Annotations;

namespace Compze.Tests.Persistence.DocumentDb;

class Dog : IPersistentEntity<Guid>
{
   public Guid Id { get; set; }
   public string Name { get; [UsedImplicitly] set; } = "John Doe Doggy";
}

class Person : Entity<Person>
{
   public Person() {}
   public Person(Guid id): base(id) {}
}

class User : Person
{
   public User(){}
   public User(Guid id): base(id) {}
   public string Email { get; set; } = "some.email@nodomain.not";
   public string Password { get; set; } = "default";

   public Address Address { get; set; } = new();

   public HashSet<User> People { get; set; } = [];
}

record Address
{
   public string Street { [UsedImplicitly] get; set; } = "SomeStreet";
   public int Streetnumber { [UsedImplicitly] get; set; } = 12;
   public string City { [UsedImplicitly] get; set; } = "SomeCity";
}

record Email(string TheEmail)
{
   public string TheEmail { get; private set; } = TheEmail;
}