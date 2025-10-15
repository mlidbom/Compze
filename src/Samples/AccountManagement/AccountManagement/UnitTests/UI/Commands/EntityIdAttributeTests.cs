using System;
using AccountManagement.API.ValidationAttributes;
using AccountManagement.Tests.Unit.UI.Commands.UserCommands;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;

namespace AccountManagement.Tests.Unit.UI.Commands;

[TestFixture]
public class EntityIdAttributeTests: NUnitTestBase
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