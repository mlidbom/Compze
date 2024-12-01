using System;
using AccountManagement.API.ValidationAttributes;
using AccountManagement.UnitTests.UI.Commands.UserCommands;
using Compze.Testing;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

namespace AccountManagement.UnitTests.UI.Commands;

[TestFixture]
public class EntityIdAttributeTests: UniversalTestBase
{
   [Test]
   public void IsValidIfIdIsNull()
   {
      CommandValidator.ValidationFailures(new ACommand {AnId = null})
                      .Should().NotBeEmpty();
   }

   [Test]
   public void IsNotValidIfIdIsEmpty()
   {
      CommandValidator.ValidationFailures(new ACommand {AnId = Guid.Empty})
                      .Should().NotBeEmpty();
   }

   class ACommand
   {
      [EntityId]
      public Guid? AnId { [UsedImplicitly] get; set; }
   }
}