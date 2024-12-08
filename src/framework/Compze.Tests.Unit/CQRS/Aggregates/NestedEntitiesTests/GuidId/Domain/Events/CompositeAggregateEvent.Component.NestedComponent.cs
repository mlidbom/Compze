 // ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable IDE1006 //Review OK: Test Naming Styles
namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;

static partial class CompositeAggregateEvent
{
   public static partial class Component
   {
      internal static class NestedComponent
      {
         internal interface IRoot : Component.IRoot;
         internal interface Renamed : PropertyUpdated.Name;

         internal static class PropertyUpdated
         {
            public interface Name : NestedComponent.IRoot
            {
               string Name { get; }
            }
         }

         internal static class Implementation
         {
            public abstract class Root : Component.Implementation.Root, NestedComponent.IRoot;
            public class Renamed(string name) : Root, NestedComponent.Renamed
            {
               public string Name { get; } = name;
            }
         }
      }
   }
}