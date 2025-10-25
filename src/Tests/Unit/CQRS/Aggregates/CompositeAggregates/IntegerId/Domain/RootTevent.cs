using System;
using Compze.Abstractions.Tessaging.Teventive.Public;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.IntegerId.Domain;

static partial class RootTevent
{
   public interface IRoot : ITaggregateTevent;

   interface Created : ITaggregateCreatedTevent, PropertyUpdated.Name;

   public static class PropertyUpdated
   {
      public interface Name : RootTevent.IRoot
      {
         string Name { get; }
      }
   }

   internal static class Implementation
   {
      public abstract class Root : TaggregateTevent, IRoot
      {
         protected Root() { }
         protected Root(Guid taggregateId) : base(taggregateId) { }
      }

      public class Created(Guid id, string name) : Root(id), RootTevent.Created
      {
         public string Name { get; } = name;
      }
   }
}