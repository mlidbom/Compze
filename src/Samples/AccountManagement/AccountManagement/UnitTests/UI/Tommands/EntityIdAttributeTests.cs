using System;
using AccountManagement.API.ValidationAttributes;
using AccountManagement.Tests.Unit.UI.Tommands.UserTommands;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using Compze.Tests.Infrastructure.Fluent;
using static Compze.Tests.Infrastructure.Fluent.MustActions;
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