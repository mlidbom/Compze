using System;
using Compze.Core.Public;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable InconsistentNaming
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Compze.Tests.Unit.Core.Public;

public class EntityId_specification
{
   static readonly Guid ExpectedGuidValue = Guid.Parse("10000000-0000-0000-0000-000000000000");
   static readonly Guid DifferentGuidValue = Guid.Parse("20000000-0000-0000-0000-000000000000");

   public class given_an_entity_id : EntityId_specification
   {
      readonly TentityId _tentityId = new(ExpectedGuidValue);

      public class and_another_TentityId : given_an_entity_id
      {
         public class with_the_same_value : and_a_taggregate_id_which_inherits_from_EntityId
         {
            readonly TentityId _taggregateId = new(ExpectedGuidValue);
            [XF] public void IEquatable_equals_returns_true() => _tentityId.Equals(_taggregateId).Must().BeTrue();
            [XF] public void Object_equals_returns_true() => Equals(_tentityId, _taggregateId).Must().BeTrue();
            [XF] public void Equals_operator_returns_true() => (_tentityId == _taggregateId).Must().BeTrue();
            [XF] public void Not_equals_operator_returns_false() => (_tentityId != _taggregateId).Must().BeFalse();
         }

         public class with_a_different_value : and_another_TentityId
         {
            readonly TentityId _differentTentityId = new(DifferentGuidValue);
            [XF] public void IEquatable_equals_returns_false() => _tentityId.Equals(_differentTentityId).Must().BeFalse();
            [XF] public void Object_equals_returns_false() => Equals(_tentityId, _differentTentityId).Must().BeFalse();
            [XF] public void Equals_operator_returns_false() => (_tentityId == _differentTentityId).Must().BeFalse();
            [XF] public void Not_equals_operator_returns_true() => (_tentityId != _differentTentityId).Must().BeTrue();
         }
      }

      public class and_a_taggregate_id_which_inherits_from_EntityId : given_an_entity_id
      {
         public class with_the_same_value : and_a_taggregate_id_which_inherits_from_EntityId
         {
            readonly TaggregateId _taggregateId = new(ExpectedGuidValue);
            [XF] public void IEquatable_equals_returns_true() => _tentityId.Equals(_taggregateId).Must().BeTrue();
            [XF] public void Object_equals_returns_true() => Equals(_tentityId, _taggregateId).Must().BeTrue();
            [XF] public void Equals_operator_returns_true() => (_tentityId == _taggregateId).Must().BeTrue();
            [XF] public void Not_equals_operator_returns_false() => (_tentityId != _taggregateId).Must().BeFalse();
         }
      }

      public class and_a_tessage_id_which_does_not_inherits_from_TEntityId : given_an_entity_id
      {
         public class with_the_same_value : and_a_tessage_id_which_does_not_inherits_from_TEntityId
         {
            readonly TessageId _tessageId = new(ExpectedGuidValue);
            [XF] public void IEquatable_equals_returns_false() => _tentityId.Equals(_tessageId).Must().BeFalse();
            [XF] public void Object_equals_returns_false() => Equals(_tentityId, _tessageId).Must().BeFalse();
            [XF] public void Equals_operator_returns_false() => (_tentityId == _tessageId).Must().BeFalse();
            [XF] public void Not_equals_operator_returns_true() => (_tentityId != _tessageId).Must().BeTrue();
         }
      }

      public class when_passing_null_as_the_other_value : given_an_entity_id
      {
         [XF] public void IEquatable_equals_returns_false() => _tentityId.Equals(null).Must().BeFalse();
         [XF] public void Object_equals_returns_false() => Equals(_tentityId, null).Must().BeFalse();
         [XF] public void Equals_operator_returns_false() => (_tentityId == null).Must().BeFalse();
         [XF] public void Not_equals_operator_returns_true() => (_tentityId != null).Must().BeTrue();
      }
   }
}
