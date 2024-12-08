using System;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain;
using FluentAssertions;

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId;

public static partial class NestedEntities_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public partial class The_component
      {
         public class After_calling_AddEntity_with_name_entity1_and_a_newGuid : The_component
         {
            readonly Guid _entity1Id;
            readonly Component.Entity _entity1;

            public After_calling_AddEntity_with_name_entity1_and_a_newGuid()
            {
               _entity1Id = Guid.NewGuid();
               _entity1 = _component.AddEntity("entity1", _entity1Id);
            }
         }

         [XFact] public void Aggregate_Component_Entity_tests()
         {
            var _component = Aggregate.Component;
            var _qmComponent = QueryModel.Component;

            var _entity1Id = Guid.NewGuid();
            var _entity1 = _component.AddEntity("entity1", _entity1Id);
            _component.Invoking(it => it.AddEntity("entity2", _entity1Id)).Should().Throw<Exception>();

            var qmEntity1 = _qmComponent.Entities.InCreationOrder[0];

            qmEntity1.Id.Should().Be(_entity1.Id).And.Be(_entity1Id);
            _entity1.Name.Should().Be("entity1");
            qmEntity1.Name.Should().Be("entity1");
            _component.Entities.InCreationOrder.Count.Should().Be(1);
            _qmComponent.Entities.InCreationOrder.Count.Should().Be(1);
            _component.Entities.Contains(_entity1.Id).Should().Be(true);
            _qmComponent.Entities.Contains(_entity1.Id).Should().Be(true);
            _component.Entities.Get(_entity1.Id).Should().Be(_entity1);
            _qmComponent.Entities.Get(_entity1.Id).Should().Be(qmEntity1);
            _component.Entities[_entity1.Id].Should().Be(_entity1);
            _qmComponent.Entities[_entity1.Id].Should().Be(qmEntity1);

            var entity2Id = Guid.NewGuid();
            var agEntity2 = _component.AddEntity("entity2", entity2Id);
            _component.Invoking(it => it.AddEntity("entity3", entity2Id)).Should().Throw<Exception>();

            var qmEntity2 = _qmComponent.Entities.InCreationOrder[1];
            agEntity2.Name.Should().Be("entity2");
            qmEntity2.Name.Should().Be("entity2");
            _component.Entities.InCreationOrder.Count.Should().Be(2);
            _qmComponent.Entities.InCreationOrder.Count.Should().Be(2);
            _component.Entities.Contains(agEntity2.Id).Should().Be(true);
            _qmComponent.Entities.Contains(agEntity2.Id).Should().Be(true);
            _component.Entities[agEntity2.Id].Should().Be(agEntity2);
            _qmComponent.Entities[agEntity2.Id].Should().Be(qmEntity2);

            _entity1.Rename("newName");
            _entity1.Name.Should().Be("newName");
            qmEntity1.Name.Should().Be("newName");
            agEntity2.Name.Should().Be("entity2");
            qmEntity2.Name.Should().Be("entity2");

            agEntity2.Rename("newName2");
            agEntity2.Name.Should().Be("newName2");
            qmEntity2.Name.Should().Be("newName2");
            _entity1.Name.Should().Be("newName");
            qmEntity1.Name.Should().Be("newName");

            _component.Entities.InCreationOrder.Count.Should().Be(2);
            _qmComponent.Entities.InCreationOrder.Count.Should().Be(2);

            agEntity2.Remove();
            _component.Entities.Contains(agEntity2.Id).Should().Be(false);
            _qmComponent.Entities.Contains(agEntity2.Id).Should().Be(false);
            _component.Entities.InCreationOrder.Count.Should().Be(1);
            _qmComponent.Entities.InCreationOrder.Count.Should().Be(1);
            _component.Invoking(it => it.Entities.Get(agEntity2.Id)).Should().Throw<Exception>();
            _qmComponent.Invoking(it => it.Entities.Get(agEntity2.Id)).Should().Throw<Exception>();
            _component.Invoking(it => { _ = it.Entities[agEntity2.Id]; }).Should().Throw<Exception>();
            _qmComponent.Invoking(it => { _ = it.Entities[agEntity2.Id]; }).Should().Throw<Exception>();

            _entity1.Remove();
            _component.Entities.Contains(_entity1.Id).Should().Be(false);
            _qmComponent.Entities.Contains(_entity1.Id).Should().Be(false);
            _component.Entities.InCreationOrder.Count.Should().Be(0);
            _qmComponent.Entities.InCreationOrder.Count.Should().Be(0);
            _component.Invoking(it => it.Entities.Get(_entity1.Id)).Should().Throw<Exception>();
            _qmComponent.Invoking(it => it.Entities.Get(_entity1.Id)).Should().Throw<Exception>();
            _component.Invoking(it => { _ = it.Entities[_entity1.Id]; }).Should().Throw<Exception>();
            _qmComponent.Invoking(it => { _ = it.Entities[_entity1.Id]; }).Should().Throw<Exception>();
         }
      }
   }
}
