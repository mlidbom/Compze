namespace Compze.Teventive.Taggregates.Tevents;

///<summary>Marks the tevent a snapshot of a taggregate starts with, when <see cref="ITaggregate.LoadFromSnapshot"/> restores the taggregate from a stored state<br/>
/// instead of from its history. The tevent restores the taggregate's own state, and establishes its identity, as <see cref="ITaggregateCreatedTevent"/> does<br/>
/// for a taggregate that is created.</summary>
///<remarks>A snapshot tevent implements the tevents that update the properties it restores, so that the same tevent appliers restore a taggregate and<br/>
/// change it: a vocabulary note's snapshot, for instance, implements the tevents that update its question, its forms and its answer.</remarks>
///<remarks>It isn't an <see cref="ITaggregateCreatedTevent"/>: a taggregate restored wasn't created, and Compze reads that tevent as creation.<br/>
/// review-naming:snapshot: ITaggregateSnapshotTevent or ITaggregateRestoredTevent, for Magnus to decide (taggregates-opus, 2026-11-16).</remarks>
public interface ITaggregateSnapshotTevent : ITaggregateTevent;
