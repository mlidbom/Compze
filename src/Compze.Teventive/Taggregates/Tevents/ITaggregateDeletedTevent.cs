namespace Compze.Teventive.Taggregates.Tevents;

///<summary>Marks a tevent as meaning that the taggregate was deleted: publishing one is how a taggregate is deleted.<br/>
/// A taggregate store no longer finds a taggregate once it has published one, and takes it out of the store when the<br/>
/// unit of work commits. Nothing may be published on the taggregate after it.</summary>
///<remarks>A store that keeps state, such as the state store, removes the taggregate. The tevent store keeps its history,<br/>
/// with this tevent last, and treats such a history as no taggregate; the history can still be read.</remarks>
public interface ITaggregateDeletedTevent : ITaggregateTevent;
