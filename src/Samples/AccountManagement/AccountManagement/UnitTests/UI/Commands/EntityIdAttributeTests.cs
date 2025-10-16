using System;
using AccountManagement.API.ValidationAttributes;
using AccountManagement.Tests.Unit.UI.Commands.UserCommands;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
using FluentAssertions;
using JetBrains.Annotations;


namespace AccountManagement.Tests.Unit.UI.Commands;


public class EntityIdAttributeTests: UniversalTestBase
{
   [XF]
   public void IsValidIfIdIsNull()
   {
      CommandValidator.ValidationFailures(new ACommand {AnId = null})
                      .Should().NotBeEmpty();
   }

   [XF]
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