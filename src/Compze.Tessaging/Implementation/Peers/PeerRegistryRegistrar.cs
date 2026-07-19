using Compze.DependencyInjection;
using Compze.DependencyInjection.Runtime.Resolution;
using Compze.DependencyInjection.Wiring.Registration;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Implementation.Peers;

static class PeerRegistryRegistrar
{
   ///<summary>Registers the endpoint's one <see cref="IPeerRegistry"/> — every transport-speaking endpoint keeps peer memory.<br/>
   /// Durability follows the foundation: an endpoint whose foundation declares Tessaging persistence remembers its peers in its<br/>
   /// database (<see cref="DurablePeerRegistry"/>, surviving restarts); a database-less endpoint remembers them in memory for the<br/>
   /// life of the process (<see cref="ProcessLifetimePeerRegistry"/>).</summary>
   public static IComponentRegistrar PeerRegistry(this IComponentRegistrar registrar)
      => registrar.IsRegistered<ITessagingSqlLayer.IPeerRegistrySqlLayer>()
            ? registrar.Register(Singleton.For<IPeerRegistry>()
                                          .CreatedBy((ITessagingSqlLayer.IPeerRegistrySqlLayer sqlLayer, ITypeMap typeMap, IComponentSet<IPeerLifecycleObserver> lifecycleObservers) => new DurablePeerRegistry(sqlLayer, typeMap, [..lifecycleObservers])))
            : registrar.Register(Singleton.For<IPeerRegistry>()
                                          .CreatedBy((ITypeMap typeMap, IComponentSet<IPeerLifecycleObserver> lifecycleObservers) => new ProcessLifetimePeerRegistry(typeMap, [..lifecycleObservers])));
}
