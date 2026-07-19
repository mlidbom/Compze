using Compze.DependencyInjection.Wiring.Registration;

namespace Compze.DependencyInjection.LightInject;

/// <summary>
/// Assigns each component set member registration its own LightInject service name, and groups those names back up by service
/// type. Both the registration side (<see cref="LightInjectContainerBuilder"/>) and the resolution side
/// (<see cref="LightInjectContainer"/>) derive names the same way — "this registration's index in the overall registrations
/// list" — from the same underlying list, so the two sides agree without sharing any mutable state.
/// </summary>
/// <remarks>
/// LightInject's own <c>GetAllInstances</c>/<c>IEnumerable&lt;T&gt;</c> resolution matches by assignability across the whole
/// container under <c>EnableMicrosoftCompatibility</c> — it would incorrectly return an unrelated singularly-registered
/// component whose concrete type happens to implement the set's contract type. Resolving each member by its own exact name
/// instead of trusting that aggregation is what avoids the leak.
/// </remarks>
static class LightInjectComponentSetNaming
{
   internal static string NameFor(int registrationIndex) => registrationIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);

   internal static IReadOnlyDictionary<Type, IReadOnlyList<string>> ComputeMemberNamesByServiceType(IReadOnlyList<ComponentRegistration> registrations) =>
      registrations
         .Select((registration, index) => (registration, name: NameFor(index)))
         .Where(it => it.registration.IsComponentSetMember)
         .SelectMany(it => it.registration.ServiceTypes.Select(serviceType => (serviceType, it.name)))
         .GroupBy(it => it.serviceType, it => it.name)
         .ToDictionary(group => group.Key, IReadOnlyList<string> (group) => group.ToArray());
}
