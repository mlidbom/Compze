using Compze.Abstractions;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses.Shared;
using Compze.Teventive.Taggregates.Tevents;

// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CA1812 // Avoid uninstantiated internal classes # used via reflection

namespace Compze.Tests.Unit.CQRS.Taggregates.SharedTomponents;

interface IProjectTevent : ITaggregateTevent
{
#pragma warning disable CA1715 // Nested tevent interface follows semantic events naming convention (compze.net/paradigms/semantic-events/event-naming.html)
   interface Created : IProjectTevent, ITaggregateCreatedTevent;
#pragma warning restore CA1715
}

interface IProjectTevent<out T> : ITaggregateTevent<T> where T : IProjectTevent;

class ProjectTevent : TaggregateTevent, IProjectTevent
{
   protected ProjectTevent() {}
   ProjectTevent(TaggregateId taggregateId) : base(taggregateId) {}

   internal class Created(TaggregateId taggregateId) : ProjectTevent(taggregateId), IProjectTevent.Created;
}

class ProjectTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IProjectTevent<T> where T : IProjectTevent;

///<summary>The adopting wrapper tevent of the project's checklist slot: an <see cref="IProjectTevent"/> that adopts an<br/>
/// <see cref="IChecklistItemTevent"/> into the project's tevent hierarchy. The whole checklist - every <see cref="ChecklistItem"/> in it -<br/>
/// publishes through this one wrapper type; individual items are told apart by the <see cref="ISharedTentityTevent{TTentityId}.EntityId"/> their tevents carry.</summary>
interface IChecklistTevent<out T> : IProjectTevent, IPublisherTevent<T> where T : IChecklistItemTevent;

class ChecklistTevent<T>(T tevent) : ProjectTevent, IChecklistTevent<T> where T : IChecklistItemTevent
{
   public T Tevent { get; } = tevent;
}

///<summary>The owner side of the shared tentity fixture: a project with a checklist of <see cref="ChecklistItem"/> shared tentities.</summary>
class ProjectTaggregate : Taggregate<ProjectTaggregate, IProjectTevent, ProjectTevent, IProjectTevent<IProjectTevent>, ProjectTevent<ProjectTevent>>, ITeventRouted
{
   static class Slots
   {
      internal static readonly SharedTomponentSlotOfAClass<ProjectTaggregate, IProjectTevent, ProjectTevent, IChecklistItemTevent, IChecklistTevent<IChecklistItemTevent>> Checklist =
         new(typeof(ChecklistTevent<>), static project => project.Checklist);
   }

   static readonly TeventRouting<ProjectTaggregate, IProjectTevent> Appliers = DeclareAppliers(it => it
     .For<IProjectTevent.Created>(static (_, _) => {}) //The base class applies the taggregate id and version; the project has no state of its own from creation.
     .SharedTomponent(Slots.Checklist));

   TeventRouting ITeventRouted.TeventAppliers => Appliers;

   public SharedTentityCollection<ChecklistItem, ChecklistItemId, IChecklistItemTevent, IChecklistItemTevent.Added> Checklist { get; }

   ProjectTaggregate() => Checklist = new SharedTentityCollection<ChecklistItem, ChecklistItemId, IChecklistItemTevent, IChecklistItemTevent.Added>(Slots.Checklist.In(this));

   public ChecklistItem AddChecklistItem(string title) => Checklist.AddByPublishing(new ChecklistItemTevent.Added(ChecklistItemId.New(), title));

   public static ProjectTaggregate Create()
   {
      var project = new ProjectTaggregate();
      project.Publish(new ProjectTevent.Created(new TaggregateId()));
      return project;
   }

   public static ProjectTaggregate LoadFromHistory(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history)
   {
      var project = new ProjectTaggregate();
      ((ITaggregate)project).LoadFromHistory(history);
      return project;
   }
}
