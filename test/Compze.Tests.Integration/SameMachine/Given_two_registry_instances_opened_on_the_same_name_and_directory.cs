using Compze.Abstractions.Hosting.Public;
using Compze.Hosting.SameMachine;
using Compze.Internals.SystemCE.DiagnosticsCE;
using Compze.Must;
using Compze.Must.Assertions;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;

namespace Compze.Tests.Integration.SameMachine;

public class Given_two_registry_instances_opened_on_the_same_name_and_directory : UniversalTestBase
{
   static DirectoryInfo TestDirectory => new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "EndpointRegistry"))._mutate(it => it.Create());

   readonly InterprocessEndpointRegistry _registryInTheAnnouncingRole;
   readonly InterprocessEndpointRegistry _registryInTheReadingRole;
   readonly string _registryName = Guid.NewGuid().ToString();
   readonly EndpointId _endpointId = new();
   readonly EndpointAddress _address = new(new Uri("compze.pipe://localhost/Compze.registry-spec"));

   public Given_two_registry_instances_opened_on_the_same_name_and_directory()
   {
      _registryInTheAnnouncingRole = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(_registryName, TestDirectory);
      _registryInTheReadingRole = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(_registryName, TestDirectory);
   }

   protected override void DisposeInternal()
   {
      _registryInTheAnnouncingRole.Delete();
      _registryInTheAnnouncingRole.Dispose();
      _registryInTheReadingRole.Dispose();
   }

   public class after_the_announcing_instance_announces_an_endpoint_address : Given_two_registry_instances_opened_on_the_same_name_and_directory
   {
      public after_the_announcing_instance_announces_an_endpoint_address() =>
         _registryInTheAnnouncingRole.AnnounceEndpointAddress(_endpointId, _address, AnnouncingProcess.Current);

      [XF] public void the_reading_instance_lists_the_address() => _registryInTheReadingRole.ServerEndpointAddresses.Must().Contain(_address);
      [XF] public void the_announcing_instance_lists_the_address() => _registryInTheAnnouncingRole.ServerEndpointAddresses.Must().Contain(_address);

      [XF] public void an_instance_opened_afterwards_lists_the_address()
      {
         using var registryOpenedAfterTheAnnouncement = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(_registryName, TestDirectory);
         registryOpenedAfterTheAnnouncement.ServerEndpointAddresses.Must().Contain(_address);
      }

      public class and_then_retracts_the_endpoint_address : after_the_announcing_instance_announces_an_endpoint_address
      {
         public and_then_retracts_the_endpoint_address() => _registryInTheAnnouncingRole.RetractEndpointAddress(_endpointId);

         [XF] public void no_address_is_listed() => _registryInTheReadingRole.ServerEndpointAddresses.Must().BeEmpty();
      }

      public class and_then_announces_a_new_address_for_the_same_endpoint : after_the_announcing_instance_announces_an_endpoint_address
      {
         readonly EndpointAddress _replacementAddress = new(new Uri("compze.pipe://localhost/Compze.registry-spec-replacement"));

         public and_then_announces_a_new_address_for_the_same_endpoint() =>
            _registryInTheAnnouncingRole.AnnounceEndpointAddress(_endpointId, _replacementAddress, AnnouncingProcess.Current);

         [XF] public void only_the_new_address_is_listed() => _registryInTheReadingRole.ServerEndpointAddresses.Single().Must().Be(_replacementAddress);
      }
   }

   public class after_an_address_is_announced_whose_announcing_process_is_no_longer_running : Given_two_registry_instances_opened_on_the_same_name_and_directory
   {
      //A process id equal to ours but started a minute earlier identifies a process that has exited and whose id the OS has since reused — the reuse case the start-time disambiguator exists for. The minute is well beyond the reader-skew tolerance that lets two processes agree they are looking at the same live process.
      public after_an_address_is_announced_whose_announcing_process_is_no_longer_running() =>
         _registryInTheAnnouncingRole.AnnounceEndpointAddress(_endpointId, _address, new AnnouncingProcess(new ProcessIdentity(Environment.ProcessId, AnnouncingProcess.Current.Identity.StartTime - TimeSpan.FromMinutes(1))));

      [XF] public void the_address_is_not_listed() => _registryInTheReadingRole.ServerEndpointAddresses.Must().BeEmpty();

      public class and_a_live_address_is_announced_afterwards : after_an_address_is_announced_whose_announcing_process_is_no_longer_running
      {
         readonly EndpointId _liveEndpointId = new();
         readonly EndpointAddress _liveAddress = new(new Uri("compze.pipe://localhost/Compze.registry-spec-live"));

         public and_a_live_address_is_announced_afterwards() =>
            _registryInTheAnnouncingRole.AnnounceEndpointAddress(_liveEndpointId, _liveAddress, AnnouncingProcess.Current);

         [XF] public void only_the_live_address_is_listed() => _registryInTheReadingRole.ServerEndpointAddresses.Single().Must().Be(_liveAddress);
      }
   }
}
