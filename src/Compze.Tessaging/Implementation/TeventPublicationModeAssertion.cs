using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Teventive.TeventStore.Internal;

namespace Compze.Tessaging.Implementation;

///<summary>Guards the invariant that a container declares exactly one tevent publication mode — in-process-only or distributed, never both.</summary>
static class TeventPublicationModeAssertion
{
   extension(IComponentRegistrar @this)
   {
      ///<summary>Throws with a clear message if a tevent publication mode is already declared, i.e. an <see cref="ITeventStoreTeventPublisher"/> is already registered.</summary>
      internal IComponentRegistrar AssertNoTeventPublicationModeIsDeclared()
      {
         State.Assert(!@this.IsRegistered<ITeventStoreTeventPublisher>(),
                      () => $"A tevent publication mode is already declared: an {nameof(ITeventStoreTeventPublisher)} is already registered. Tessaging is spoken either in-process-only or distributed, never both — declare exactly one mode.");
         return @this;
      }
   }
}
