using System.Net;
using Compze.Hosting.AcrossMachines;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.AcrossMachines;

///<summary>Two <see cref="NetworkEndpointRegistry"/> instances opened on the same name on one <see cref="EndpointRegistryServer"/>,<br/>
/// as two processes on two machines would open them: what one of them announces, the other lists, for as long as the<br/>
/// announcing one's connection to the server lasts. Here both are in this process and the server listens on loopback.</summary>
public class Given_two_network_registries_opened_on_the_same_name_on_a_registry_server : UniversalTestBase
{
   const string RegistryName = "the registry both open";

   EndpointRegistryServer _server = EndpointRegistryServer.StartListeningAt(new IPEndPoint(IPAddress.Loopback, port: 0));
   readonly NetworkEndpointRegistry _announcingRegistry;
   readonly NetworkEndpointRegistry _otherRegistry;
   readonly List<NetworkEndpointRegistry> _registriesOpenedByASpecification = [];
   readonly EndpointId _endpointId = new();
   readonly EndpointAddress _address = new(new Uri("compze.tcp://192.0.2.1:1001/"));
   readonly EndpointAddress _newAddress = new(new Uri("compze.tcp://192.0.2.1:1002/"));

   public Given_two_network_registries_opened_on_the_same_name_on_a_registry_server()
   {
      _announcingRegistry = NetworkEndpointRegistry.OpenOnServerAt(RegistryName, _server.ListensAt);
      _otherRegistry = NetworkEndpointRegistry.OpenOnServerAt(RegistryName, _server.ListensAt);
   }

   protected override async Task DisposeAsyncInternal()
   {
      foreach(var registry in _registriesOpenedByASpecification) await registry.DisposeAsync();
      await _announcingRegistry.DisposeAsync();
      await _otherRegistry.DisposeAsync();
      await _server.DisposeAsync();
   }

   NetworkEndpointRegistry OpenARegistryNamed(string registryName)
   {
      var registry = NetworkEndpointRegistry.OpenOnServerAt(registryName, _server.ListensAt);
      _registriesOpenedByASpecification.Add(registry);
      return registry;
   }

   public class after_one_of_them_announces_an_endpoint_address : Given_two_network_registries_opened_on_the_same_name_on_a_registry_server
   {
      public after_one_of_them_announces_an_endpoint_address() => _announcingRegistry.AnnounceEndpointAddress(_endpointId, _address);

      [XF] public void the_other_lists_the_address() => _otherRegistry.AddressesListedOnceTheyAre(_address).Must().SequenceEqual([_address]);
      [XF] public void the_announcing_one_lists_the_address() => _announcingRegistry.AddressesListedOnceTheyAre(_address).Must().SequenceEqual([_address]);
      [XF] public void one_opened_afterwards_on_the_same_name_lists_the_address() => OpenARegistryNamed(RegistryName).AddressesListedOnceTheyAre(_address).Must().SequenceEqual([_address]);

      [XF] public void one_opened_on_another_name_lists_no_address()
      {
         var registryOfAnotherName = OpenARegistryNamed("another registry");
         _otherRegistry.AddressesListedOnceTheyAre(_address); //By now the server has the announcement, so a registry that lists nothing is not just yet to be told.
         registryOfAnotherName.ServerEndpointAddresses.Must().BeEmpty();
      }

      public class and_then_retracts_it : after_one_of_them_announces_an_endpoint_address
      {
         public and_then_retracts_it()
         {
            _otherRegistry.AddressesListedOnceTheyAre(_address);
            _announcingRegistry.RetractEndpointAddress(_endpointId);
         }

         [XF] public void the_other_lists_no_address() => _otherRegistry.AddressesListedOnceTheyAre().Must().BeEmpty();
      }

      public class and_then_announces_a_new_address_for_the_same_endpoint : after_one_of_them_announces_an_endpoint_address
      {
         public and_then_announces_a_new_address_for_the_same_endpoint() => _announcingRegistry.AnnounceEndpointAddress(_endpointId, _newAddress);

         [XF] public void the_other_lists_only_the_new_address() => _otherRegistry.AddressesListedOnceTheyAre(_newAddress).Must().SequenceEqual([_newAddress]);
      }

      public class and_is_then_disposed_as_when_its_process_ends : after_one_of_them_announces_an_endpoint_address
      {
         protected override async Task InitializeAsyncInternal()
         {
            _otherRegistry.AddressesListedOnceTheyAre(_address);
            await _announcingRegistry.DisposeAsync();
         }

         [XF] public void the_other_lists_no_address() => _otherRegistry.AddressesListedOnceTheyAre().Must().BeEmpty();
      }

      public class and_the_other_then_announces_a_new_address_for_the_same_endpoint_as_when_the_endpoint_starts_again_in_another_process : after_one_of_them_announces_an_endpoint_address
      {
         public and_the_other_then_announces_a_new_address_for_the_same_endpoint_as_when_the_endpoint_starts_again_in_another_process()
         {
            _otherRegistry.AddressesListedOnceTheyAre(_address);
            _otherRegistry.AnnounceEndpointAddress(_endpointId, _newAddress);
         }

         [XF] public void both_list_only_the_new_address() =>
            new { TheFirst = _announcingRegistry.AddressesListedOnceTheyAre(_newAddress), TheOther = _otherRegistry.AddressesListedOnceTheyAre(_newAddress) }
              .Must().Satisfy(it => it.TheFirst.SequenceEqual([_newAddress]) && it.TheOther.SequenceEqual([_newAddress]));

         public class and_the_first_is_then_disposed : and_the_other_then_announces_a_new_address_for_the_same_endpoint_as_when_the_endpoint_starts_again_in_another_process
         {
            protected override async Task InitializeAsyncInternal()
            {
               _announcingRegistry.AddressesListedOnceTheyAre(_newAddress);
               await _announcingRegistry.DisposeAsync();
            }

            [XF] public void a_registry_opened_afterwards_lists_the_new_address() => OpenARegistryNamed(RegistryName).AddressesListedOnceTheyAre(_newAddress).Must().SequenceEqual([_newAddress]);
         }
      }

      public class and_the_registry_server_is_then_disposed : after_one_of_them_announces_an_endpoint_address
      {
         protected override async Task InitializeAsyncInternal()
         {
            _otherRegistry.AddressesListedOnceTheyAre(_address);
            await _server.DisposeAsync();
         }

         [XF] public void the_other_lists_no_address() => _otherRegistry.AddressesListedOnceTheyAre().Must().BeEmpty();
         [XF] public void the_announcing_one_lists_no_address() => _announcingRegistry.AddressesListedOnceTheyAre().Must().BeEmpty();

         public class and_a_registry_server_then_starts_listening_at_the_same_address : and_the_registry_server_is_then_disposed
         {
            protected override async Task InitializeAsyncInternal()
            {
               await base.InitializeAsyncInternal();
               _otherRegistry.AddressesListedOnceTheyAre();
               _server = EndpointRegistryServer.StartListeningAt(_server.ListensAt);
            }

            [XF] public void the_other_lists_the_address_again() => _otherRegistry.AddressesListedOnceTheyAre(_address).Must().SequenceEqual([_address]);
         }
      }
   }
}
