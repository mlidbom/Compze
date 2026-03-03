using Compze.Core.Public;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable InconsistentNaming
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

#pragma warning disable CA1052 //I should make an inherited class static? Really?

namespace Compze.Tests.Unit.Core.Public;

public class EntityId_specification
{
   static readonly Guid ExpectedGuidValue = Guid.Parse("10000000-0000-0000-0000-000000000000");
   static readonly Guid DifferentGuidValue = Guid.Parse("20000000-0000-0000-0000-000000000000");

   public class given_a_TentityId : EntityId_specification
   {
      readonly TentityId _tentityId = new(ExpectedGuidValue);

      public class and_another_TentityId : given_a_TentityId
      {
         public class with_the_same_value : and_a_taggregate_id_which_inherits_from_EntityId
         {
            readonly TentityId _sameTentityId = new(ExpectedGuidValue);
            [XF] public void IEquatable_equals_returns_true() => _tentityId.Equals(_sameTentityId).Must().BeTrue();
            [XF] public void Object_equals_returns_true() => Equals(_tentityId, _sameTentityId).Must().BeTrue();
            [XF] public void equals_operator_returns_true() => (_tentityId == _sameTentityId).Must().BeTrue();
            [XF] public void not_equals_operator_returns_false() => (_tentityId != _sameTentityId).Must().BeFalse();
         }

         public class with_a_different_value : and_another_TentityId
         {
            readonly TentityId _differentTentityId = new(DifferentGuidValue);
            [XF] public void IEquatable_equals_returns_false() => _tentityId.Equals(_differentTentityId).Must().BeFalse();
            [XF] public void Object_equals_returns_false() => Equals(_tentityId, _differentTentityId).Must().BeFalse();
            [XF] public void equals_operator_returns_false() => (_tentityId == _differentTentityId).Must().BeFalse();
            [XF] public void not_equals_operator_returns_true() => (_tentityId != _differentTentityId).Must().BeTrue();
         }
      }

      public class and_a_taggregate_id_which_inherits_from_EntityId : given_a_TentityId
      {
         public class with_the_same_value : and_a_taggregate_id_which_inherits_from_EntityId
         {
            readonly TaggregateId _sameTaggregateId = new(ExpectedGuidValue);
            [XF] public void IEquatable_equals_returns_true() => _tentityId.Equals(_sameTaggregateId).Must().BeTrue();
            [XF] public void Object_equals_returns_true() => Equals(_tentityId, _sameTaggregateId).Must().BeTrue();
            [XF] public void equals_operator_returns_true() => (_tentityId == _sameTaggregateId).Must().BeTrue();
            [XF] public void not_equals_operator_returns_false() => (_tentityId != _sameTaggregateId).Must().BeFalse();
         }
      }

      public class and_a_tessage_id_which_does_not_inherits_from_TEntityId : given_a_TentityId
      {
         public class with_the_same_value : and_a_tessage_id_which_does_not_inherits_from_TEntityId
         {
            readonly TessageId _tessageId = new(ExpectedGuidValue);
            [XF] public void IEquatable_equals_returns_false() => _tentityId.Equals(_tessageId).Must().BeFalse();
            [XF] public void Object_equals_returns_false() => Equals(_tentityId, _tessageId).Must().BeFalse();
            [XF] public void equals_operator_returns_false() => (_tentityId == _tessageId).Must().BeFalse();
            [XF] public void not_equals_operator_returns_true() => (_tentityId != _tessageId).Must().BeTrue();
         }
      }

      public class when_passing_null_as_the_other_value : given_a_TentityId
      {
         [XF] public void IEquatable_equals_returns_false() => _tentityId.Equals(null).Must().BeFalse();
         [XF] public void Object_equals_returns_false() => Equals(_tentityId, null).Must().BeFalse();
         [XF] public void equals_operator_returns_false() => (_tentityId == null).Must().BeFalse();
         [XF] public void not_equals_operator_returns_true() => (_tentityId != null).Must().BeTrue();
      }
   }
}
