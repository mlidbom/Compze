using System;
using Compze.Core.Tessaging.Teventive.Internal.Implementation;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using JetBrains.Annotations;
using static Compze.Utilities.Testing.Must.MustActions;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable UnusedMember.Local
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable MemberCanBeInternal
#pragma warning disable CA1806 // Do not ignore method results

namespace Compze.Tests.Unit.CQRS.Taggregates;

public class PublicSettersAndFieldsAreDisallowedTests : UniversalTestBase
{
   public static class RootTevent
   {
      public interface IRoot : ITaggregateTevent { string Public1 { get; set; } }

      public class Root : TaggregateTevent, IRoot { public string Public1 { get; set; } = string.Empty; }

      [AllowPublicSetters]
      public class Ignored : Root { public string IgnoredMember { get; set; } = string.Empty; }

      public static class Component
      {
         public interface IRoot : RootTevent.IRoot { string Public2 { get; set; }  }
         internal class Root : RootTevent.Root, IRoot { public string Public2 { get; set; } = string.Empty;}

         public static class NestedComponent
         {
            public interface IRoot : Component.IRoot{ string           Public3 { get; set; }  }
#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
            internal class Root : Component.Root, IRoot { public string Public3 { get; set; } = string.Empty; }
#pragma warning restore CA1812
         }
      }

      public static class Entity
      {
         public interface IRoot : RootTevent.IRoot{ string            Public4 { get; set; }  }
         internal class Root : RootTevent.Root, IRoot { public string Public4 { get; set; } = string.Empty;
#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
            [UsedImplicitly] public class GetterSetter : IGetSetTaggregateEntityTeventEntityId<Guid, Root, IRoot>
#pragma warning restore CA1812
            {
               public Guid GetId(IRoot tevent) => throw new Exception();
               public void SetEntityId(Root tevent, Guid id) => throw new Exception();
            }
         }

         public static class Component
         {
            public interface IRoot : Entity.IRoot { string           Public2 { get; set; }  }
            internal class Root : Entity.Root, IRoot { public string Public2 { get; set; } = string.Empty; }

            public static class NestedComponent
            {
               public interface IRoot : Component.IRoot{ string            Public3 { get; set; }  }
#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
               internal class Root : Component.Root, IRoot { public string Public3 { get; set; } = string.Empty;}
#pragma warning restore CA1812
            }
         }
      }

   }

   class Root() : Taggregate<Root, RootTevent.IRoot, RootTevent.Root>()
   {
      public class AggComponent(Root parent): Root.Component<AggComponent, RootTevent.Component.Root, RootTevent.Component.IRoot>(parent)
      {
         public string Public { get; set; } = string.Empty;

         public class NestedAggComponent(AggComponent parent) : AggComponent.Component<NestedAggComponent, RootTevent.Component.NestedComponent.Root, RootTevent.Component.NestedComponent.IRoot>(parent)
         {
            public string Public { get; set; } = string.Empty;
         }
      }

      public class AggEntity(Root taggregate) : Root.Entity<AggEntity, Guid, RootTevent.Entity.Root, RootTevent.Entity.IRoot, RootTevent.Entity.IRoot, RootTevent.Entity.Root.GetterSetter>(taggregate)
      {
         public string Public { get; set; }  = string.Empty;

         public class EntNestedComp(AggEntity parent) : AggEntity.Component<EntNestedComp, RootTevent.Entity.Component.Root, RootTevent.Entity.Component.IRoot>(parent)
         {
            public string Public2 { get; set; } = string.Empty;
         }
      }
   }


   [XF]public void Trying_to_create_instance_of_taggregate_throws_and_lists_all_broken_types_in_exception_except_ignored()
   {
      Invoking(() => new Root())
                   .Must().Throw<Exception>()
                   .Which.InnerException!
                   .Message.Must()
                   .Contain(typeof(Root).FullName!)
                   .Contain(typeof(RootTevent.IRoot).FullName!)
                   .Contain(typeof(RootTevent.Root).FullName!)
                   .NotContain(typeof(RootTevent.Ignored).FullName!);
   }

   [XF] public void Trying_to_create_instance_of_component_throws_and_lists_all_broken_types_in_exception()
   {
      Invoking(() => new Root.AggComponent(null!))
                   .Must().Throw<Exception>()
                   .Which.InnerException!
                   .Message.Must()
                   .Contain(typeof(Root.AggComponent).FullName!)
                   .Contain(typeof(RootTevent.Component.IRoot).FullName!)
                   .Contain(typeof(RootTevent.Component.Root).FullName!);
   }


   [XF] public void Trying_to_create_instance_of_nested_nested_component_throws_and_lists_all_broken_types_in_exception()
   {
      Invoking(() => new Root.AggComponent.NestedAggComponent(null!))
                   .Must().Throw<Exception>()
                   .Which.InnerException!
                   .Message.Must()
                   .Contain(typeof(Root.AggComponent.NestedAggComponent).FullName!)
                   .Contain(typeof(RootTevent.Component.NestedComponent.IRoot).FullName!)
                   .Contain(typeof(RootTevent.Component.NestedComponent.Root).FullName!);
   }

   [XF] public void Trying_to_create_instance_of_entity_throws_and_lists_all_broken_types_in_exception()
   {
      Invoking(() => new Root.AggEntity(null!))
                   .Must().Throw<Exception>()
                   .Which.InnerException!
                   .Message.Must()
                   .Contain(typeof(Root.AggEntity).FullName!)
                   .Contain(typeof(RootTevent.Entity.IRoot).FullName!)
                   .Contain(typeof(RootTevent.Entity.Root).FullName!);
   }

   [XF] public void Trying_to_create_instance_of_entity_nested_component_throws_and_lists_all_broken_types_in_exception()
   {
      Invoking(() => new Root.AggEntity.EntNestedComp(null!))
                   .Must().Throw<Exception>()
                   .Which.InnerException!
                   .Message.Must()
                   .Contain(typeof(Root.AggEntity.EntNestedComp).FullName!)
                   .Contain(typeof(RootTevent.Entity.Component.IRoot).FullName!)
                   .Contain(typeof(RootTevent.Entity.Component.Root).FullName!);
   }
}
