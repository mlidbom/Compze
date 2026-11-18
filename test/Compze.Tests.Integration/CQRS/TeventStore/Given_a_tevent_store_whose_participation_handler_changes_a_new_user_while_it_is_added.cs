using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.TessageBus;
using Compze.Tests.Common;
using Compze.Tests.Common.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Teventive.TaggregateStores;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.TeventStore;

///<summary>A participation handler that gets a new user while the tevent store adds it, and changes it: the store tracks the user<br/>
/// before it publishes its tevents, so the handler gets the very instance being added, and the change it makes is stored and<br/>
/// published after the user's first tevent, in the order published.</summary>
public class Given_a_tevent_store_whose_participation_handler_changes_a_new_user_while_it_is_added : UniversalTestBase
{
   readonly IDependencyInjectionContainer _container;
   readonly TaggregateId _userId = new();
   readonly List<string> _receivedByTheHandlers = [];
   readonly User _userAdded = new();
   User? _userTheHandlerGot;

   public Given_a_tevent_store_whose_participation_handler_changes_a_new_user_while_it_is_added()
   {
      _container = TestEnv.DIContainer.SetupTestingContainer(registrar => registrar.RequireIntegrationTestTypeMappings(),
                                                             composeEngine: engine => engine.RegisterTessageBusHandlers(handle => handle
                                                                .ForTevent((IUserRegistered it, ITaggregateStoreUpdater taggregates) =>
                                                                 {
                                                                    _userTheHandlerGot = taggregates.GetForUpdate<User>(it.TaggregateId);
                                                                    _userTheHandlerGot.ChangeEmail("changed@example.com");
                                                                 })
                                                                .ForTevent((IUserTevent it) => _receivedByTheHandlers.Add(it.GetType().Name))),
                                                             declareTeventStoreTaggregates: store => store.Taggregate<User>());

      _userAdded.Register("registered@example.com", "password", _userId);
      _container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TaggregateStoreUpdater().Add(_userAdded));
   }

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync().AsTask();

   [PCT] public void the_handler_got_the_very_user_being_added() => _userTheHandlerGot.Must().ReferenceEqual(_userAdded);

   [PCT] public void the_handlers_received_its_tevents_once_each_in_the_order_published() =>
      _receivedByTheHandlers.Must().SequenceEqual([nameof(UserRegistered), nameof(UserChangedEmail)]);

   [PCT] public void the_user_loaded_afterwards_has_the_email_the_handler_gave_it() =>
      _container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TaggregateStoreUpdater().GetForUpdate<User>(_userId).Email).Must().Be("changed@example.com");
}
