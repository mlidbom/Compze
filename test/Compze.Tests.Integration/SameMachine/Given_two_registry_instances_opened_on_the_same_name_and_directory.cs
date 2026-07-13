using Compze.Abstractions.Hosting.Public;
using Compze.Hosting.SameMachine;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;

namespace Compze.Tests.Integration.SameMachine;

public class Given_two_registry_instances_opened_on_the_same_name_and_directory : UniversalTestBase
{
   static DirectoryInfo TestDirectory => new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "EndpointRegistry"))._mutate(it => it.Create());

   readonly InterprocessEndpointRegistry _registryInThePublishingRole;
   readonly InterprocessEndpointRegistry _registryInTheReadingRole;
   readonly string _registryName = Guid.NewGuid().ToString();
   readonly EndpointId _endpointId = new();
   readonly EndpointAddress _address = new(new Uri("compze.pipe://localhost/Compze.registry-spec"));

   public Given_two_registry_instances_opened_on_the_same_name_and_directory()
   {
      _registryInThePublishingRole = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(_registryName, TestDirectory);
      _registryInTheReadingRole = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(_registryName, TestDirectory);
   }

   protected override void DisposeInternal()
   {
      _registryInThePublishingRole.Delete();
      _registryInThePublishingRole.Dispose();
      _registryInTheReadingRole.Dispose();
   }

   public class after_the_publishing_instance_registers_an_endpoint_address : Given_two_registry_instances_opened_on_the_same_name_and_directory
   {
      public after_the_publishing_instance_registers_an_endpoint_address() =>
         _registryInThePublishingRole.RegisterEndpointAddress(_endpointId, _address, PublishingProcess.Current);

      [XF] public void the_reading_instance_lists_the_address() => _registryInTheReadingRole.ServerEndpointAddresses.Must().Contain(_address);
      [XF] public void the_publishing_instance_lists_the_address() => _registryInThePublishingRole.ServerEndpointAddresses.Must().Contain(_address);

      [XF] public void an_instance_opened_afterwards_lists_the_address()
      {
         using var registryOpenedAfterRegistration = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(_registryName, TestDirectory);
         registryOpenedAfterRegistration.ServerEndpointAddresses.Must().Contain(_address);
      }

      public class and_then_unregisters_the_endpoint : after_the_publishing_instance_registers_an_endpoint_address
      {
         public and_then_unregisters_the_endpoint() => _registryInThePublishingRole.UnregisterEndpointAddress(_endpointId);

         [XF] public void no_address_is_listed() => _registryInTheReadingRole.ServerEndpointAddresses.Must().BeEmpty();
      }

      public class and_then_registers_a_new_address_for_the_same_endpoint : after_the_publishing_instance_registers_an_endpoint_address
      {
         readonly EndpointAddress _replacementAddress = new(new Uri("compze.pipe://localhost/Compze.registry-spec-replacement"));

         public and_then_registers_a_new_address_for_the_same_endpoint() =>
            _registryInThePublishingRole.RegisterEndpointAddress(_endpointId, _replacementAddress, PublishingProcess.Current);

         [XF] public void only_the_new_address_is_listed() => _registryInTheReadingRole.ServerEndpointAddresses.Single().Must().Be(_replacementAddress);
      }
   }

   public class after_registering_an_address_whose_publishing_process_is_no_longer_running : Given_two_registry_instances_opened_on_the_same_name_and_directory
   {
      //A process id equal to ours but with a different start time identifies a process that has exited and whose id the OS has since reused — the reuse case the start-time disambiguator exists for.
      public after_registering_an_address_whose_publishing_process_is_no_longer_running() =>
         _registryInThePublishingRole.RegisterEndpointAddress(_endpointId, _address, new PublishingProcess(Environment.ProcessId, PublishingProcess.Current.StartTimeTicks - 1));

      [XF] public void the_address_is_not_listed() => _registryInTheReadingRole.ServerEndpointAddresses.Must().BeEmpty();

      public class and_a_live_address_is_registered_afterwards : after_registering_an_address_whose_publishing_process_is_no_longer_running
      {
         readonly EndpointId _liveEndpointId = new();
         readonly EndpointAddress _liveAddress = new(new Uri("compze.pipe://localhost/Compze.registry-spec-live"));

         public and_a_live_address_is_registered_afterwards() =>
            _registryInThePublishingRole.RegisterEndpointAddress(_liveEndpointId, _liveAddress, PublishingProcess.Current);

         [XF] public void only_the_live_address_is_listed() => _registryInTheReadingRole.ServerEndpointAddresses.Single().Must().Be(_liveAddress);
      }
   }
}
