 // ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Events;

static partial class CompositeAggregateEvent
{
   public static partial class Component
   {
      public interface IRoot : CompositeAggregateEvent.ICompositeAggregateEvent;

      interface Renamed : PropertyUpdated.Name;

      public static class PropertyUpdated
      {
         public interface Name : IRoot
         {
            string Name { get; }
         }
      }

      internal static class Implementation
      {
         public abstract class Root : CompositeAggregateEvent.Implementation.Root, Component.IRoot;

         public class Renamed(string name) : Root, Component.Renamed
         {
            public string Name { get; } = name;
         }
      }
   }
}