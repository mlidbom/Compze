using Compze.Must;
using Compze.Tests.Infrastructure.Diagnostics;
using Compze.Tests.Unit.CQRS.Taggregates.SnapshotRoundTrips;
using Compze.xUnitBDD;

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.Allocation;

///<summary>The bytes of managed memory that a book, with state in itself, in a component and in a shared tomponent, allocates for what a<br/>
/// taggregate store does with it: a store that keeps taggregates as their state makes each unit of work's copy by restoring an empty<br/>
/// book from a snapshot, and every change publishes a tevent. The bounds are what each cost when they were set, with about a quarter<br/>
/// more as headroom, so that what Compze adds to them can't grow unseen.</summary>
///<remarks>Measured on the calling thread, in the Debug build that the suite and CI run in, where the JIT allocates every object on<br/>
/// the heap. A Release build allocates the same or less. When the bounds were set (2026-11-16): an empty book 1,264 B, a copy<br/>
/// 1,712 B, and a published tevent 776 B in the book, 768 B in its component and 936 B in its shared tomponent.</remarks>
public class Allocating_a_book
{
   readonly BookServices _services = new(SnapshotHandling.Correctly);
   readonly Book _book;
   readonly BookSnapshot _snapshot;
   readonly Queue<string> _tagsNotYetAdded = new(["second tag", "third tag"]);

   public Allocating_a_book()
   {
      _book = Book.Create(_services, "The title", "The author");
      _book.Rate("reader one", 4);
      _book.Chapters.Add("first");
      _book.Tags.Add("fiction");
      _snapshot = _book.TakeSnapshot();
   }

   [XF] public void an_empty_book_allocates_at_most_1_600_bytes() => ManagedAllocation.BytesAllocatedBy(() => new Book(_services)).Must().BeLessThanOrEqualTo(1_600);

   [XF] public void a_copy_restored_from_a_snapshot_allocates_at_most_2_100_bytes() => ManagedAllocation.BytesAllocatedBy(RestoredCopy).Must().BeLessThanOrEqualTo(2_100);

   [XF] public void publishing_a_tevent_the_book_applies_allocates_at_most_1_000_bytes() =>
      ManagedAllocation.BytesAllocatedBy(() => _book._mutate(it => it.Rate("reader three", 3))).Must().BeLessThanOrEqualTo(1_000);

   [XF] public void publishing_a_tevent_its_component_applies_allocates_at_most_1_000_bytes() =>
      ManagedAllocation.BytesAllocatedBy(() => _book._mutate(it => it.Chapters.Add("another"))).Must().BeLessThanOrEqualTo(1_000);

   [XF] public void publishing_a_tevent_its_shared_tomponent_applies_allocates_at_most_1_200_bytes() =>
      ManagedAllocation.BytesAllocatedBy(() => _book._mutate(it => it.Tags.Add(_tagsNotYetAdded.Dequeue()))).Must().BeLessThanOrEqualTo(1_200);

   Book RestoredCopy()
   {
      var copy = new Book(_services);
      copy.RestoreFrom(_snapshot);
      return copy;
   }
}
