using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Hosting;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Public;
using Compze.Tessaging.Hosting.Testing.Tessaging;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Teventive.TeventStore.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Abstractions.Public;
using Compze.Must;
using Compze.Typermedia;
using Compze.Typermedia.HandlerRegistration;

#pragma warning disable CA1715 //Interfaces without I prefix
// ReSharper disable MemberCanBeInternal for testing
// ReSharper disable InconsistentNaming for testing
#pragma warning disable CA1724 // Type names should not match namespaces

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Experiment_with_unifying_tevents_and_tommands_test : UniversalTestBase
{
   readonly ITestingEndpointHost _host;

   IServiceLocator _userDomainServiceLocator = null!;
   TestClient _client = null!;
   readonly IEndpoint _userManagementDomainEndpoint;

   public Experiment_with_unifying_tevents_and_tommands_test()
   {
      _host = TestingEndpointHost.Create();

      _userManagementDomainEndpoint = _host.RegisterEndpoint(
         "UserManagement.Domain",
         new EndpointId(Guid.Parse("A4A2BA96-8D82-47AC-8A1B-38476C7B5D5D")),
         builder =>
         {
            builder.Container.Register().TeventStore(builder.Configuration.ConnectionStringName);

            builder.RegisterTessagingHandlers
                   .ForTevent((IUserTevent.UserRegistered _) => {});

            builder.RegisterTypermediaHandlers()
                   .ForTuery((GetUserTuery tuery, ITeventStoreReader teventReader) => new UserResource(teventReader.GetHistory(tuery.UserId)))
                   .ForTommandWithResult((UserRegistrarTommand.RegisterUserTypermediaTommand typermediaTommand, ITeventStoreUpdater store) =>
                    {
                       store.Save(UserTaggregate.Register(typermediaTommand));
                       return new RegisterUserResult(typermediaTommand.UserId);
                    });
         });
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync();

      _client = await TestClient.ConnectTo(_userManagementDomainEndpoint.TypermediaAddress!);

      _userDomainServiceLocator = _userManagementDomainEndpoint.ServiceLocator;

      _userDomainServiceLocator.ExecuteTransactionInIsolatedScope(() => _userDomainServiceLocator.Resolve<ITeventStoreUpdater>().Save(UserRegistrarTaggregate.Create()));
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

   public interface IUserTevent<out T> : ITaggregateIdentifyingTevent<T> where T : IUserTevent;
   public interface IUserTevent : ITaggregateTevent
   {
      public interface UserRegistered : IUserTevent, ITaggregateCreatedTevent;
   }

   public class UserTevent<T>(T tevent) : TaggregateIdentifyingTevent<T>(tevent), IUserTevent<T> where T : IUserTevent;
   public class UserTevent : TaggregateTevent, IUserTevent
   {
      UserTevent(TaggregateId taggregateId) : base(taggregateId) {}

      public class UserRegisteredTevent(TaggregateId userId) : UserTevent(userId), IUserTevent.UserRegistered;
   }

   public static class UserRegistrarTommand
   {
      public class RegisterUserTypermediaTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand<RegisterUserResult>
      {
         public TaggregateId UserId { get; private set; } = new();

         RegisterUserTypermediaTommand() {}

         internal static RegisterUserTypermediaTommand Create() => new() { Id = new TessageId() };
      }
   }

   public interface IUserRegistrarTevent<out T> : ITaggregateIdentifyingTevent<T> where T : IUserRegistrarTevent;
   public interface IUserRegistrarTevent : ITaggregateTevent;

   public class UserRegistrarTevent<T>(T tevent) : TaggregateIdentifyingTevent<T>(tevent), IUserRegistrarTevent<T> where T : IUserRegistrarTevent;

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

      UserRegistrarTaggregate()
         => RegisterTeventAppliers()
           .IgnoreUnhandled<IUserRegistrarTevent>();

      internal static RegisterUserResult RegisterUser(IRemoteTypermediaNavigator navigator) => UserRegistrarTommand.RegisterUserTypermediaTommand.Create().PostOn(navigator);
   }

   public class UserTaggregate : Taggregate<UserTaggregate, IUserTevent, UserTevent, IUserTevent<IUserTevent>, UserTevent<UserTevent>>
   {
      UserTaggregate()
         => RegisterTeventAppliers()
           .IgnoreUnhandled<IUserTevent>();

      internal static UserTaggregate Register(UserRegistrarTommand.RegisterUserTypermediaTommand typermediaTommand)
      {
         var registered = new UserTaggregate();
         registered.Publish(new UserTevent.UserRegisteredTevent(typermediaTommand.UserId));
         return registered;
      }
   }

   public class GetUserTuery(TaggregateId userId) : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<UserResource>
   {
      public TaggregateId UserId { get; private set; } = userId;
   }

   public class UserResource(IEnumerable<ITaggregateTevent> history)
   {
      public IEnumerable<ITaggregateTevent> History { get; } = history;
   }

   public class RegisterUserResult(TaggregateId userId)
   {
      public GetUserTuery UserLink { get; private set; } = new(userId);
   }
}
