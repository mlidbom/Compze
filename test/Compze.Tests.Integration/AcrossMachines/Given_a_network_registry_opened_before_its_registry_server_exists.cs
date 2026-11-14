using System.Net;
using System.Net.Sockets;
using Compze.Hosting.AcrossMachines;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.AcrossMachines;

///<summary>A <see cref="NetworkEndpointRegistry"/> is opened, and announced to, while nothing listens at its<br/>
/// <see cref="EndpointRegistryServer"/>'s address: the order in which a guest's endpoints and the server on its host<br/>
/// start must not matter. It keeps trying to reach the server, and announces once it does.</summary>
public class Given_a_network_registry_opened_before_its_registry_server_exists : UniversalTestBase
{
   const string RegistryName = "the registry opened before its server";

   readonly IPEndPoint _whereTheServerWillListen = ALoopbackAddressNothingListensAt();
   readonly NetworkEndpointRegistry _registry;
   readonly EndpointAddress _address = new(new Uri("compze.tcp://192.0.2.1:1001/"));
   EndpointRegistryServer? _server;
   NetworkEndpointRegistry? _registryOpenedOnceTheServerListens;

   public Given_a_network_registry_opened_before_its_registry_server_exists()
   {
      _registry = NetworkEndpointRegistry.OpenOnServerAt(RegistryName, _whereTheServerWillListen);
      _registry.AnnounceEndpointAddress(new EndpointId(), _address);
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _registry.DisposeAsync();
      if(_registryOpenedOnceTheServerListens != null) await _registryOpenedOnceTheServerListens.DisposeAsync();
      if(_server != null) await _server.DisposeAsync();
   }

   [XF] public void it_lists_no_address() => _registry.ServerEndpointAddresses.Must().BeEmpty();

   public class once_the_registry_server_starts_listening : Given_a_network_registry_opened_before_its_registry_server_exists
   {
      public once_the_registry_server_starts_listening()
      {
         _server = EndpointRegistryServer.StartListeningAt(_whereTheServerWillListen);
         _registryOpenedOnceTheServerListens = NetworkEndpointRegistry.OpenOnServerAt(RegistryName, _whereTheServerWillListen);
      }

      [XF] public void it_lists_the_address_it_was_announced() => _registry.AddressesListedOnceTheyAre(_address).Must().SequenceEqual([_address]);
      [XF] public void a_registry_opened_on_the_same_name_lists_the_address() => _registryOpenedOnceTheServerListens!.AddressesListedOnceTheyAre(_address).Must().SequenceEqual([_address]);
   }

   ///<summary>An address on loopback with a port that was free a moment ago: the operating system picked it for a socket<br/>
   /// that is closed again.</summary>
   static IPEndPoint ALoopbackAddressNothingListensAt()
   {
      using var socketGivenAFreePort = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      socketGivenAFreePort.Bind(new IPEndPoint(IPAddress.Loopback, port: 0));
      return (IPEndPoint)socketGivenAFreePort.LocalEndPoint!;
   }
}
