using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;

using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Hosting;
using Compze.Tests.Infrastructure;
using Compze.Typermedia;
using Compze.Typermedia.Client;
using Compze.Typermedia.HandlerRegistration;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.InProcess;

///<summary>
/// An endpoint that declares only the in-process features has no transports, no discovery, and no runtime
/// lifecycle — the host starts it with zero components — yet strictly local navigation and in-process tevent
/// publication work in full.
///</summary>
public class Given_an_endpoint_declaring_only_in_process_tessaging_and_typermedia : UniversalTestBase
{
   readonly ITestingEndpointHost _host;
   readonly IEndpoint _endpoint;
   readonly List<IMyGreetingRequestedTevent> _receivedTevents = [];

   public Given_an_endpoint_declaring_only_in_process_tessaging_and_typermedia()
   {
      _host = TestingEndpointHost.Create();
      _endpoint = _host.RegisterEndpoint(
         "InProcessOnly",
         new EndpointId(Guid.Parse("7C93BE71-88F5-4B34-A4D4-9E0C22ABB01D")),
         builder =>
         {
            builder.AddInProcessTessaging();
            builder.AddInProcessTypermedia();

            builder.RegisterTessagingHandlers.ForTevent((IMyGreetingRequestedTevent tevent) => _receivedTevents.Add(tevent));
            builder.RegisterTypermediaHandlers.ForTuery((MyStrictlyLocalGreetingTuery tuery) => new MyGreeting { Message = $"Hello {tuery.Name}!" });
         });
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync().caf();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   [PCT] public void the_endpoint_runs_no_components() => _endpoint.Components.Must().BeEmpty();

   [PCT] public void the_endpoint_has_no_tessaging_address() => _endpoint.TessagingAddress.Must().BeNull();

   [PCT] public void the_endpoint_has_no_typermedia_address() => _endpoint.TypermediaAddress.Must().BeNull();

   [PCT] public void a_strictly_local_tuery_executes_through_the_endpoints_in_process_navigator() =>
      _endpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteInIsolatedScope(scope =>
         scope.Resolve<ILocalTypermediaNavigatorSession>().Execute(new MyStrictlyLocalGreetingTuery { Name = "World" }).Message.Must().Be("Hello World!"));

   [PCT] public void a_tevent_published_in_process_reaches_the_endpoints_subscriber()
   {
      _endpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
         unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MySpecialGreetingRequestedTevent()));
      _receivedTevents.Must().HaveCount(1);
   }
}
