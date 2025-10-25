 // ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;

static partial class CompositeTaggregateTevent
{
   public static partial class Component
   {
      public interface IRoot : CompositeTaggregateTevent.ICompositeTaggregateTevent;

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
         public abstract class Root : CompositeTaggregateTevent.Implementation.Root, Component.IRoot;

         public class Renamed(string name) : Root, Component.Renamed
         {
            public string Name { get; } = name;
         }
      }
   }
}