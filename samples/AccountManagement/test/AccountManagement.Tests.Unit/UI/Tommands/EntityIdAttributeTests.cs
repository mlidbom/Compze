using AccountManagement.API.ValidationAttributes;
using AccountManagement.Tests.Unit.UI.Tommands.UserTommands;
using Compze.Tests.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;
using Compze.xUnitBDD;
using JetBrains.Annotations;


namespace AccountManagement.Tests.Unit.UI.Tommands;


public class EntityIdAttributeTests: UniversalTestBase
{
   [XF]
   public void IsValidIfIdIsNull()
   {
      TommandValidator.ValidationFailures(new ATommand {AnId = null})
                      .Must().NotBeEmpty();
   }

   [XF]
   public void IsNotValidIfIdIsEmpty()
   {
      TommandValidator.ValidationFailures(new ATommand {AnId = Guid.Empty})
                      .Must().NotBeEmpty();
   }

   class ATommand
   {
      [EntityId]
      public Guid? AnId { [UsedImplicitly] get; set; }
   }
}