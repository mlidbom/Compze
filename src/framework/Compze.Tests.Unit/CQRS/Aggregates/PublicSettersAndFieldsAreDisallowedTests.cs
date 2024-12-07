using System;
using Compze.GenericAbstractions.Time;
using Compze.Persistence.EventStore;
using Compze.Persistence.EventStore.Aggregates;
using Compze.Testing;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable UnusedMember.Local
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable MemberCanBeInternal
#pragma warning disable CA1806 // Do not ignore method results

namespace Compze.Tests.Unit.CQRS.Aggregates;

public class PublicSettersAndFieldsAreDisallowedTests : UniversalTestBase
{
   public static class RootEvent
   {
      public interface IRoot : IAggregateEvent { string Public1 { get; set; } }

      public class Root : AggregateEvent, IRoot { public string Public1 { get; set; } = string.Empty; }

      [AllowPublicSetters]
      public class Ignored : Root { public string IgnoredMember { get; set; } = string.Empty; }

      public static class Component
      {
         public interface IRoot : RootEvent.IRoot { string Public2 { get; set; }  }
         internal class Root : RootEvent.Root, IRoot { public string Public2 { get; set; } = string.Empty;}

         public static class NestedComponent
         {
            public interface IRoot : Component.IRoot{ string           Public3 { get; set; }  }
            internal class Root : Component.Root, IRoot { public string Public3 { get; set; } = string.Empty; }
         }
      }

      public static class Entity
      {
         public interface IRoot : RootEvent.IRoot{ string            Public4 { get; set; }  }
         internal class Root : RootEvent.Root, IRoot { public string Public4 { get; set; } = string.Empty;
            [UsedImplicitly] public class GetterSetter : IGetSetAggregateEntityEventEntityId<Guid, Root, IRoot>
            {
               public Guid GetId(IRoot @event) => throw new Exception();
               public void SetEntityId(Root @event, Guid id) => throw new Exception();
            }
         }

         public static class Component
         {
            public interface IRoot : Entity.IRoot { string           Public2 { get; set; }  }
            internal class Root : Entity.Root, IRoot { public string Public2 { get; set; } = string.Empty; }

            public static class NestedComponent
            {
               public interface IRoot : Component.IRoot{ string            Public3 { get; set; }  }
               internal class Root : Component.Root, IRoot { public string Public3 { get; set; } = string.Empty;}
            }
         }
      }

   }

   class Root(IUtcTimeTimeSource timeSource) : Aggregate<Root, RootEvent.Root, RootEvent.IRoot>(timeSource)
   {
      public class AggComponent(Root aggregate) : Root.Component<AggComponent, RootEvent.Component.Root, RootEvent.Component.IRoot>(aggregate)
      {
         public string Public { get; set; } = string.Empty;

         public class NestedAggComponent(AggComponent parent) : AggComponent.NestedComponent<NestedAggComponent, RootEvent.Component.NestedComponent.Root, RootEvent.Component.NestedComponent.IRoot>(parent)
         {
            public string Public { get; set; } = string.Empty;
         }
      }

      public class AggEntity(Root aggregate) : Root.Entity<AggEntity, Guid, RootEvent.Entity.Root, RootEvent.Entity.IRoot, RootEvent.Entity.IRoot, RootEvent.Entity.Root.GetterSetter>(aggregate)
      {
         public string Public { get; set; }  = string.Empty;

         public class EntNestedComp(AggEntity parent) : AggEntity.NestedComponent<EntNestedComp, RootEvent.Entity.Component.Root, RootEvent.Entity.Component.IRoot>(parent)
         {
            public string Public2 { get; set; } = string.Empty;
         }
      }
   }


   [Test]public void Trying_to_create_instance_of_aggregate_throws_and_lists_all_broken_types_in_exception_except_ignored()
   {
      FluentActions.Invoking(() => new Root(null!))
                   .Should().Throw<Exception>()
                   .Which.InnerException!
                   .Message
                   .Should().Contain(typeof(Root).FullName)
                   .And.Contain(typeof(RootEvent.IRoot).FullName)
                   .And.Contain(typeof(RootEvent.Root).FullName)
                   .And.NotContain(typeof(RootEvent.Ignored).FullName);
   }

   [Test] public void Trying_to_create_instance_of_component_throws_and_lists_all_broken_types_in_exception()
   {
      FluentActions.Invoking(() => new Root.AggComponent(null!))
                   .Should().Throw<Exception>()
                   .Which.InnerException!
                   .Message
                   .Should().Contain(typeof(Root.AggComponent).FullName).And
                   .Contain(typeof(RootEvent.Component.IRoot).FullName)
                   .And.Contain(typeof(RootEvent.Component.Root).FullName);
   }


   [Test] public void Trying_to_create_instance_of_nested_nested_component_throws_and_lists_all_broken_types_in_exception()
   {
      FluentActions.Invoking(() => new Root.AggComponent.NestedAggComponent(null!))
                   .Should().Throw<Exception>()
                   .Which.InnerException!
                   .Message
                   .Should().Contain(typeof(Root.AggComponent.NestedAggComponent).FullName).And
                   .Contain(typeof(RootEvent.Component.NestedComponent.IRoot).FullName)
                   .And.Contain(typeof(RootEvent.Component.NestedComponent.Root).FullName);
   }

   [Test] public void Trying_to_create_instance_of_entity_throws_and_lists_all_broken_types_in_exception()
   {
      FluentActions.Invoking(() => new Root.AggEntity(null!))
                   .Should().Throw<Exception>()
                   .Which.InnerException!
                   .Message
                   .Should().Contain(typeof(Root.AggEntity).FullName).And
                   .Contain(typeof(RootEvent.Entity.IRoot).FullName)
                   .And.Contain(typeof(RootEvent.Entity.Root).FullName);
   }

   [Test] public void Trying_to_create_instance_of_entity_nested_component_throws_and_lists_all_broken_types_in_exception()
   {
      FluentActions.Invoking(() => new Root.AggEntity.EntNestedComp(null!))
                   .Should().Throw<Exception>()
                   .Which.InnerException!
                   .Message
                   .Should().Contain(typeof(Root.AggEntity.EntNestedComp).FullName).And
                   .Contain(typeof(RootEvent.Entity.Component.IRoot).FullName)
                   .And.Contain(typeof(RootEvent.Entity.Component.Root).FullName);
   }
}