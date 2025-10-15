using AccountManagement.API.ValidationAttributes;
using AccountManagement.Tests.Unit.UI.Commands.UserCommands;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;

namespace AccountManagement.Tests.Unit.UI.Commands;

[TestFixture]
public class EmailAttributeTests : NUnitTestBase
{
   [Test]
   public void IsNotValidIfEmailIsNull()
   {
      CommandValidator.ValidationFailures(new ACommand {Email = null})
                      .Should().NotBeEmpty();
   }

   [Test]
   public void IsValidIfEmailIsEmpty()
   {
      CommandValidator.ValidationFailures(new ACommand {Email = ""})
                      .Should().BeEmpty();
   }

   [Test]
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