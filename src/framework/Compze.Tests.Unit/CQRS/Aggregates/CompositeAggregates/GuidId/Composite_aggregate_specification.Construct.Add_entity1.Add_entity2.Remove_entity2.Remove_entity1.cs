﻿using System;
using System.Linq;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using FluentAssertions;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId;

public static partial class Composite_aggregate_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public partial class After_adding_entity_named_entity1
      {
         public partial class After_adding_entity_named_entity2
         {
            public partial class After_calling_entity2_Remove
            {
               public class After_calling_entity1_Remove : After_calling_entity2_Remove
               {
                  public After_calling_entity1_Remove() => _entity1.Remove();

                  public class The_aggregates_Entities_collection___ : After_calling_entity1_Remove
                  {
                     [XFact] public void Single_throws() => Invoking(()  => _aggregate.Entities.Single()).Should().Throw<Exception>();
                     [XFact] public void InCreationOrder_0_throws() => Invoking(() => _aggregate.Entities.InCreationOrder[0]).Should().Throw<Exception>();
                     [XFact] public void InCreationOrder_Count_is_0() => _aggregate.Entities.InCreationOrder.Count.Should().Be(0);

                     public class Passing_the_entity1_id_to : The_aggregates_Entities_collection___
                     {
                        [XFact] public void Contains_returns_false() => _aggregate.Entities.Contains(_entity1.Id).Should().Be(false);
                        [XFact] public void Get_throws() => Invoking(() => _aggregate.Entities.Get(_entity1.Id)).Should().Throw<Exception>();
                        [XFact] public void Indexer_throws() => Invoking(() => _aggregate.Entities[_entity1.Id]).Should().Throw<Exception>();

                        [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                        {
                           _aggregate.Entities.TryGet(_entity1.Id, out var agEntity1Fetched).Should().BeFalse();
                           agEntity1Fetched.Should().Be(null);
                        }
                     }
                  }

                  public class The_QueryModel_Entities_collection_ : After_calling_entity1_Remove
                  {
                     [XFact] public void Single_throws() => Invoking(() => _queryModel.Entities.Single()).Should().Throw<Exception>();
                     [XFact] public void InCreationOrder_0_throws() => Invoking(() => _queryModel.Entities.InCreationOrder[0]).Should().Throw<Exception>();
                     [XFact] public void InCreationOrder_Count_is_0() => _queryModel.Entities.InCreationOrder.Count.Should().Be(0);

                     public class Passing_the_entity1_id_to : The_QueryModel_Entities_collection_
                     {
                        [XFact] public void Contains_returns_false() => _queryModel.Entities.Contains(_entity1.Id).Should().Be(false);
                        [XFact] public void Get_throws() => Invoking(() => _queryModel.Entities.Get(_entity1.Id)).Should().Throw<Exception>();
                        [XFact] public void Indexer_throws() => Invoking(() => _queryModel.Entities[_entity1.Id]).Should().Throw<Exception>();

                        [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                        {
                           _queryModel.Entities.TryGet(_entity1.Id, out var qmEntity1Fetched).Should().BeFalse();
                           qmEntity1Fetched.Should().Be(null);
                        }
                     }
                  }
               }
            }
         }
      }
   }
}
