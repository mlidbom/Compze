using AccountManagement.API.ValidationAttributes;
using AccountManagement.Tests.Unit.UI.Tommands.UserTommands;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using Compze.Tests.Infrastructure.Fluent;
using JetBrains.Annotations;

namespace AccountManagement.Tests.Unit.UI.Tommands;

public class EmailAttributeTests : UniversalTestBase
{
   [XF]
   public void IsNotValidIfEmailIsNull()
   {
      TommandValidator.ValidationFailures(new ATommand {Email = null})
                      .Must().NotBeEmpty();
   }

   [XF]
   public void IsValidIfEmailIsEmpty()
   {
      TommandValidator.ValidationFailures(new ATommand {Email = ""})
                      .Must().BeEmpty();
   }

   [XF]
   public void IsNotValidIfEmailIsInvalid()
   {
      TommandValidator.ValidationFailures(new ATommand {Email = "InvalidEmail"})
                      .Must().NotBeEmpty();
   }

   class ATommand
   {
      [Email]
      public string? Email { [UsedImplicitly] get; set; }
   }
}