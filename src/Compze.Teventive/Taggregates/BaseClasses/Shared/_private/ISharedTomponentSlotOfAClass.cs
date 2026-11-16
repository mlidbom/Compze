using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive.Taggregates.BaseClasses.Shared._private;

///<summary>What a <see cref="SharedTomponent{TTomponentTevent}"/> publishes through when its slot belongs to its owner's class instead of to the one owner:<br/>
/// the slot is told which owner the tomponent is in, since it is the same slot for every owner of the class.</summary>
interface ISharedTomponentSlotOfAClass<in TTomponentTevent>
   where TTomponentTevent : class, ITevent
{
   void PublishInternal(object owner, TTomponentTevent tevent);
}
