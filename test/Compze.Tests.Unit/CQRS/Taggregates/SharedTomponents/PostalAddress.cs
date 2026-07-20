using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.BaseClasses.Shared;

// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CA1812 // Avoid uninstantiated internal classes # used via reflection

namespace Compze.Tests.Unit.CQRS.Taggregates.SharedTomponents;

///<summary>The reusable-library side of the shared tomponent fixture: a postal address whose tevents are rooted in its OWN hierarchy -<br/>
/// bare <see cref="ITevent"/>s carrying no taggregate identity. Any taggregate can own postal addresses; the owner's slot adopts each<br/>
/// published tevent into the owner's tevent hierarchy.</summary>
interface IPostalAddressTevent : ITevent
{
#pragma warning disable CA1715 // Nested tevent interface follows semantic events naming convention (compze.net/paradigms/semantic-events/event-naming.html)
   interface Changed : IPostalAddressTevent
   {
      string Street { get; }
   }
#pragma warning restore CA1715
}

class PostalAddressTevent : IPostalAddressTevent
{
   internal class Changed(string street) : PostalAddressTevent, IPostalAddressTevent.Changed
   {
      public string Street { get; } = street;
   }
}

class PostalAddress : SharedTomponent<IPostalAddressTevent>
{
   public PostalAddress(ISharedTomponentSlot<IPostalAddressTevent> slot) : base(slot) =>
      RegisterTeventAppliers().For<IPostalAddressTevent.Changed>(tevent => Street = tevent.Street);

   public string? Street { get; private set; }

   public void ChangeStreet(string street) => Publish(new PostalAddressTevent.Changed(street));
}
