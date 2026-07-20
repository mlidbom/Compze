using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Typermedia.Client;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Typermedia;
using Compze.Tests.Common;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Abstractions.Public;
using Compze.Must;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Engine.HandlerRegistration.TessageHandlers;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore.Abstractions.Public;
using Compze.Teventive.TeventStore.Wiring;
using Compze.Tessaging.Typermedia;

#pragma warning disable CA1715 //Interfaces without I prefix
// ReSharper disable MemberCanBeInternal for testing
// ReSharper disable InconsistentNaming for testing
#pragma warning disable CA1724 // Type names should not match namespaces

using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Experiment_with_unifying_tevents_and_tommands_test : UniversalTestBase
{
   readonly TestingEndpointHost _host;

   IScopeFactory _userDomainScopeFactory = null!;
   TypermediaTestClient _client = null!;
   readonly ExactlyOnceEndpoint _userManagementDomainEndpoint;

   public Experiment_with_unifying_tevents_and_tommands_test()
   {
      _host = TestingEndpointHost.Create();

      _userManagementDomainEndpoint = _host.RegisterExactlyOnceEndpoint(
         "UserManagementDomain",
         new EndpointId(Guid.Parse("A4A2BA96-8D82-47AC-8A1B-38476C7B5D5D")),
         endpointBuilder =>
         {
            endpointBuilder.RegisterComponents(registrar =>
            {
               registrar.RequireIntegrationTestTypeMappings();
               registrar.RequireMappedTypesFromAssemblyContaining<ITaggregateTevent>();
            });

            endpointBuilder.Registrar.TeventStore(endpointBuilder.Configuration.ConnectionStringName);

            endpointBuilder.RegisterTessageHandlers(handle => handle
                      .ForTevent((IUserTevent.UserRegistered _) => Task.CompletedTask)
                      .ForTuery((GetUserTuery tuery, ITeventStoreReader teventReader) => new UserResource(teventReader.GetHistory(tuery.UserId)))
                      .ForTommand((UserRegistrarTommand.RegisterUserTypermediaTommand typermediaTommand, ITeventStoreUpdater store) =>
                       {
                          store.Save(UserTaggregate.Register(typermediaTommand));
                          return new RegisterUserResult(typermediaTommand.UserId);
                       }));
         });
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync();

      _client = await TypermediaTestClient.ConnectTo(_userManagementDomainEndpoint.Address!,
                                                     registrar => registrar.RequireIntegrationTestTypeMappings());

      _userDomainScopeFactory = _userManagementDomainEndpoint.ServiceLocator.Resolve<IScopeFactory>();

      _userDomainScopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Save(UserRegistrarTaggregate.Create()));
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _client.DisposeAsync();
      await _host.DisposeAsync();
   }

   [PCT] public void Can_register_user_and_fetch_user_resource()
   {
      var registrationResult = UserRegistrarTaggregate.RegisterUser(_client.Navigator);

      var user = _client.Navigator.Get(registrationResult.UserLink);

      user.Must().NotBeNull();
      user.History.Count().Must().Be(1);
   }

   public interface IUserTevent<out T> : ITaggregateTevent<T> where T : IUserTevent;
   public interface IUserTevent : ITaggregateTevent
   {
      public interface UserRegistered : IUserTevent, ITaggregateCreatedTevent;
   }

   public class UserTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IUserTevent<T> where T : IUserTevent;
   public class UserTevent : TaggregateTevent, IUserTevent
   {
      UserTevent(TaggregateId taggregateId) : base(taggregateId) {}

      public class UserRegisteredTevent(TaggregateId userId) : UserTevent(userId), IUserTevent.UserRegistered;
   }

   public static class UserRegistrarTommand
   {
      public class RegisterUserTypermediaTommand : Remotable.AtMostOnce.AtMostOnceTypermediaTommand<RegisterUserResult>
      {
         public TaggregateId UserId { get; private set; } = new();

         RegisterUserTypermediaTommand() {}

         internal static RegisterUserTypermediaTommand Create() => new() { Id = new TessageId() };
      }
   }

   public interface IUserRegistrarTevent<out T> : ITaggregateTevent<T> where T : IUserRegistrarTevent;
   public interface IUserRegistrarTevent : ITaggregateTevent;

   public class UserRegistrarTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IUserRegistrarTevent<T> where T : IUserRegistrarTevent;

   public class UserRegistrarTevent : TaggregateTevent, IUserRegistrarTevent
   {
      UserRegistrarTevent(TaggregateId taggregateId) : base(taggregateId) {}

      public class Created() : UserRegistrarTevent(UserRegistrarTaggregate.SingletonId), ITaggregateCreatedTevent;
   }

   public class UserRegistrarTaggregate : Taggregate<UserRegistrarTaggregate, IUserRegistrarTevent, UserRegistrarTevent, IUserRegistrarTevent<IUserRegistrarTevent>, UserRegistrarTevent<UserRegistrarTevent>>
   {
      internal static readonly TaggregateId SingletonId = new(Guid.Parse("5C400DD9-50FB-40C7-8A13-265005588AED"));

      internal static UserRegistrarTaggregate Create()
      {
         var registrar = new UserRegistrarTaggregate();
         registrar.Publish(new UserRegistrarTevent.Created());
         return registrar;
      }

      UserRegistrarTaggregate() : base(TeventDispatcherConfig.IgnoreAllUnhandled) {} //This test taggregate maintains no state, so no tevent has an applier.

      internal static RegisterUserResult RegisterUser(IRemoteTypermediaNavigator navigator) => UserRegistrarTommand.RegisterUserTypermediaTommand.Create().PostOn(navigator);
   }

   public class UserTaggregate : Taggregate<UserTaggregate, IUserTevent, UserTevent, IUserTevent<IUserTevent>, UserTevent<UserTevent>>
   {
      UserTaggregate() : base(TeventDispatcherConfig.IgnoreAllUnhandled) {} //This test taggregate maintains no state, so no tevent has an applier.

      internal static UserTaggregate Register(UserRegistrarTommand.RegisterUserTypermediaTommand typermediaTommand)
      {
         var registered = new UserTaggregate();
         registered.Publish(new UserTevent.UserRegisteredTevent(typermediaTommand.UserId));
         return registered;
      }
   }

   public class GetUserTuery(TaggregateId userId) : Remotable.NonTransactional.Tueries.Tuery<UserResource>
   {
      public TaggregateId UserId { get; private set; } = userId;
   }

   public class UserResource(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history)
   {
      public IEnumerable<ITaggregateTevent<ITaggregateTevent>> History { get; } = history;
   }

   public class RegisterUserResult(TaggregateId userId)
   {
      public GetUserTuery UserLink { get; private set; } = new(userId);
   }
}
