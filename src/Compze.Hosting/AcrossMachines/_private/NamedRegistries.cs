using Compze.Internals.SystemCE.CollectionsCE.GenericCE;

namespace Compze.Hosting.AcrossMachines._private;

///<summary>The registries an <see cref="EndpointRegistryServer"/> keeps, apart from each other by name. A registry is there<br/>
/// from when its first participant names it.</summary>
sealed class NamedRegistries
{
   readonly Dictionary<string, NamedRegistry> _registriesByName = new();

   internal NamedRegistry Named(string registryName) => _registriesByName.GetOrAdd(registryName, () => new NamedRegistry());
}
