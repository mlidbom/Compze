using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;

static partial class CompositeTaggregateTevent
{
   public interface ICompositeTaggregateTevent : ITaggregateTevent;

   interface Created : ITaggregateCreatedTevent, PropertyUpdated.Name;

   public static class PropertyUpdated
   {
      public interface Name : CompositeTaggregateTevent.ICompositeTaggregateTevent
      {
         string Name { get; }
      }
   }

   internal static class Implementation
   {
      public abstract class Root : TaggregateTevent, ICompositeTaggregateTevent
      {
         protected Root() { }
         protected Root(TaggregateId taggregateId) : base(taggregateId) { }
      }

      public class Created(TaggregateId id, string name) : Root(id), CompositeTaggregateTevent.Created
      {
         public string Name { get; } = name;
      }
   }
}