using System;
using Compze.DDD;
using JetBrains.Annotations;

namespace Compze.Tests.Persistence.DocumentDb;

class Dog : IPersistentEntity<Guid>
{
   public Guid Id { get; set; }
   public string Name { get; [UsedImplicitly] set; } = "John Doe Doggy";
}