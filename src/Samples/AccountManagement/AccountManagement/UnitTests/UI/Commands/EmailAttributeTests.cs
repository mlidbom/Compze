using AccountManagement.API.ValidationAttributes;
using AccountManagement.Tests.Unit.UI.Commands.UserCommands;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using FluentAssertions;
using JetBrains.Annotations;

namespace AccountManagement.Tests.Unit.UI.Commands;

public class EmailAttributeTests : UniversalTestBase
{
   [XF]
   public void IsNotValidIfEmailIsNull()
   {
      CommandValidator.ValidationFailures(new ACommand {Email = null})
                      .Should().NotBeEmpty();
   }

   [XF]
   public void IsValidIfEmailIsEmpty()
   {
      CommandValidator.ValidationFailures(new ACommand {Email = ""})
                      .Should().BeEmpty();
   }

   [XF]
   public void IsNotValidIfEmailIsInvalid()
   {
      CommandValidator.ValidationFailures(new ACommand {Email = "InvalidEmail"})
                      .Should().NotBeEmpty();
   }

   class ACommand
   {
      [Email]
      public string? Email { [UsedImplicitly] get; set; }
   }
}