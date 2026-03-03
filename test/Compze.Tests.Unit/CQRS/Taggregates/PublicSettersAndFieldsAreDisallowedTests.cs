using System;
using Compze.Core.Tessaging.Teventive.Internal.Implementation;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using JetBrains.Annotations;
using static Compze.Utilities.Testing.Must.MustActions;
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable PossibleInterfaceMemberAmbiguity
// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedMember.Global
#pragma warning disable IDE0051

// ReSharper disable InconsistentNaming
#pragma warning disable CA1715 //Interfaces without I prefix
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable UnusedMember.Local
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable MemberCanBeInternal
#pragma warning disable CS0108 //Hides inherited member.
#pragma warning disable CA1806 // Do not ignore method results
#pragma warning disable CA1812 // These types are instantiated via reflection in taggregate infrastructure

namespace Compze.Tests.Unit.CQRS.Taggregates;

public class PublicSettersAndFieldsAreDisallowedTests : UniversalTestBase
{
   interface IRootTevent<out T> : ITaggregateIdentifyingTevent<T> where T : IRootTevent;
   public interface IRootTevent : ITaggregateTevent
   {
      string Public1 { get; set; }

      public interface Component : IRootTevent
      {
         string Public2 { get; set; }

         public interface NestedComponent : Component
         {
            string Public3 { get; set; }
         }
      }

      public interface Entity : IRootTevent
      {
         string Public4 { get; set; }

         public interface Component : Entity
         {
            string Public2 { get; set; }
         }
      }
   }

   class RootTevent<T>(T tevent) : TaggregateIdentifyingTevent<T>(tevent), IRootTevent<T> where T : IRootTevent;

   abstract class RootTevent : TaggregateTevent, IRootTevent
   {
      public string Public1 { get; set; } = string.Empty;

      [AllowPublicSetters]
      public class Ignored : RootTevent
      {
         public string IgnoredMember { get; set; } = string.Empty;
      }

      public class Component : RootTevent, IRootTevent.Component
      {
         public string Public2 { get; set; } = string.Empty;

         public class NestedComponent : Component, IRootTevent.Component.NestedComponent
         {
            public string Public3 { get; set; } = string.Empty;
         }
      }

      public class Entity : RootTevent, IRootTevent.Entity
      {
         public string Public4 { get; set; } = string.Empty;

            [UsedImplicitly]
            public class GetterSetter : IGetSetTaggregateEntityTeventEntityId<Guid, RootTevent, IRootTevent>
            {
                public Guid GetId(IRootTevent tevent) => throw new Exception();
                public void SetEntityId(RootTevent tevent, Guid id) => throw new Exception();
            }

            public class Component : Entity, IRootTevent.Entity.Component
         {
            public string Public2 { get; set; } = string.Empty;
         }
      }
   }

   class Root : Taggregate<Root, IRootTevent, RootTevent, IRootTevent<IRootTevent>, RootTevent<RootTevent>>
   {
      public class AggComponent(Root parent) : Root.Component<AggComponent, IRootTevent.Component, RootTevent.Component>(parent)
      {
         public string Public { get; set; } = string.Empty;

         public class NestedAggComponent(AggComponent parent) : AggComponent.Component<NestedAggComponent, RootTevent.Component.NestedComponent, IRootTevent.Component.NestedComponent>(parent)
         {
            public string Public { get; set; } = string.Empty;
         }
      }

      public class AggEntity(Root taggregate) : Root.Entity<AggEntity, Guid, IRootTevent.Entity, RootTevent.Entity, IRootTevent.Entity, RootTevent.Entity.GetterSetter>(taggregate)
      {
         public string Public { get; set; } = string.Empty;

         public class EntNestedComp(AggEntity parent) : AggEntity.Component<EntNestedComp, RootTevent.Entity.Component, IRootTevent.Entity.Component>(parent)
         {
            public string Public2 { get; set; } = string.Empty;
         }
      }
   }

   [XF] public void Trying_to_create_instance_of_taggregate_throws_and_lists_all_broken_types_in_exception_except_ignored()
   {
      Invoking(() => new Root())
        .Must().Throw<Exception>()
        .Which.InnerException!
        .Message.Must()
        .Contain(typeof(Root).FullName!)
        .Contain(typeof(IRootTevent).FullName!)
        .NotContain(typeof(RootTevent.Ignored).FullName!);
   }

   [XF] public void Trying_to_create_instance_of_component_throws_and_lists_all_broken_types_in_exception()
   {
      Invoking(() => new Root.AggComponent(null!))
        .Must().Throw<Exception>()
        .Which.InnerException!
        .Message.Must()
        .Contain(typeof(Root.AggComponent).FullName!)
        .Contain(typeof(IRootTevent.Component).FullName!)
        .Contain(typeof(RootTevent.Component).FullName!);
   }

   [XF] public void Trying_to_create_instance_of_nested_nested_component_throws_and_lists_all_broken_types_in_exception()
   {
      Invoking(() => new Root.AggComponent.NestedAggComponent(null!))
        .Must().Throw<Exception>()
        .Which.InnerException!
        .Message.Must()
        .Contain(typeof(Root.AggComponent.NestedAggComponent).FullName!)
        .Contain(typeof(IRootTevent.Component.NestedComponent).FullName!)
        .Contain(typeof(RootTevent.Component.NestedComponent).FullName!);
   }

   [XF] public void Trying_to_create_instance_of_entity_throws_and_lists_all_broken_types_in_exception()
   {
      Invoking(() => new Root.AggEntity(null!))
        .Must().Throw<Exception>()
        .Which.InnerException!
        .Message.Must()
        .Contain(typeof(Root.AggEntity).FullName!)
        .Contain(typeof(IRootTevent.Entity).FullName!)
        .Contain(typeof(RootTevent.Entity).FullName!);
   }

   [XF] public void Trying_to_create_instance_of_entity_nested_component_throws_and_lists_all_broken_types_in_exception()
   {
      Invoking(() => new Root.AggEntity.EntNestedComp(null!))
        .Must().Throw<Exception>()
        .Which.InnerException!
        .Message.Must()
        .Contain(typeof(Root.AggEntity.EntNestedComp).FullName!)
        .Contain(typeof(IRootTevent.Entity.Component).FullName!)
        .Contain(typeof(RootTevent.Entity.Component).FullName!);
   }
}
