using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive.Taggregates.BaseClasses.Shared._private;

///<summary>What a <see cref="SharedTomponent{TTomponentTevent}"/> publishes through when its slot belongs to its owner's class instead of to the one owner:<br/>
/// the slot is told which owner the tomponent is in, since it is the same slot for every owner of the class.</summary>
interface ISharedTomponentSlot<in TTomponentTevent>
   where TTomponentTevent : class, ITevent
{
   ///<summary>Publishes <paramref name="tevent"/>, which <paramref name="publisher"/>, the shared tomponent in this slot of <paramref name="owner"/>, publishes.</summary>
   void PublishInternal(object owner, object publisher, TTomponentTevent tevent);
}
