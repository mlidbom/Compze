using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Tessaging.Transport.Internal;
using Compze.Abstractions.Tessaging.Typermedia.Public;
using Compze.Abstractions.Time.Public;
using Compze.Tessaging.Hosting.AspNetCore.Wiring;
using Compze.Tessaging.Hosting.Testing.Sql;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.Teventive;
using Compze.Tessaging.Teventive.EventStore.DependencyInjection;
using Compze.Tests.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Tests.Infrastructure.XUnit;
using FluentAssertions;

// ReSharper disable MemberCanBeInternal for testing
// ReSharper disable InconsistentNaming for testing
#pragma warning disable CA1724 // Type names should not match namespaces

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Experiment_with_unifying_events_and_commands_test : UniversalTestBase
{
   readonly ITestingEndpointHost _host;

   IServiceLocator _userDomainServiceLocator = null!;
   readonly IEndpoint _clientEndpoint;
   readonly IEndpoint _userManagementDomainEndpoint;

   IRemoteHypermediaNavigator RemoteNavigator => _clientEndpoint.ServiceLocator.Resolve<IRemoteHypermediaNavigator>();

   public Experiment_with_unifying_events_and_commands_test()
   {
      _host = TestingEndpointHost.Create(TestingContainerFactory.CreateWithRegisteredServiceLocator);

      _userManagementDomainEndpoint = _host.RegisterEndpoint(
         "UserManagement.Domain",
         new EndpointId(Guid.Parse("A4A2BA96-8D82-47AC-8A1B-38476C7B5D5D")),
         builder =>
         {
            builder.Container.Register()
                   .AspNetCoreTransport()
                   .CurrentTestsConfiguredSqlLayer();
            builder.Container.Register().EventStore(builder.Configuration.ConnectionStringName);

            builder.RegisterHandlers
                   .ForEvent((UserEvent.IUserRegistered _) => {})
                   .ForQuery((GetUserTuery tuery, IEventStoreReader eventReader) => new UserResource(eventReader.GetHistory(tuery.UserId)))
                   .ForCommandWithResult((UserRegistrarCommand.RegisterUserTommand tommand, IEventStoreUpdater store) =>
                    {
                       store.Save(UserAggregate.Register(tommand));
                       return new RegisterUserResult(tommand.UserId);
                    });
         });

      _clientEndpoint = _host.RegisterClientEndpointForRegisteredEndpoints();
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync();

      _userDomainServiceLocator = _userManagementDomainEndpoint.ServiceLocator;

      _userDomainServiceLocator.ExecuteTransactionInIsolatedScope(() => _userDomainServiceLocator.Resolve<IEventStoreUpdater>().Save(UserRegistrarAggregate.Create()));
   }

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   [PCT] public void Can_register_user_and_fetch_user_resource()
   {
      var registrationResult = _userDomainServiceLocator.ExecuteInIsolatedScope(() => UserRegistrarAggregate.RegisterUser(_userDomainServiceLocator.Resolve<IRemoteHypermediaNavigator>()));

      var user = _clientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => RemoteNavigator.Get(registrationResult.UserLink));

      user.Should().NotBe(null);
      user.History.Count().Should().Be(1);
   }

   public static class UserEvent
   {
      public interface IRoot : IAggregateTevent;

      public interface IUserRegistered : IRoot, IAggregateCreatedTevent;

      public static class Implementation
      {
         public class Root : AggregateTevent, IRoot
         {
            protected Root() {}
            protected Root(Guid aggregateId) : base(aggregateId) {}
         }

         public class UserRegisteredEvent(Guid userId) : Root(userId), IUserRegistered;
      }
   }

   public static class UserRegistrarCommand
   {
      public class RegisterUserTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTommand<RegisterUserResult>
      {
         public Guid UserId { get; private set; } = Guid.NewGuid();

         RegisterUserTommand() : base(DeduplicationIdHandling.Reuse) {}

         internal static RegisterUserTommand Create() => new() { TessageId = Guid.CreateVersion7() };
      }
   }

   public static class UserRegistrarEvent
   {
      public interface IRoot : IAggregateTevent;

      public static class Implementation
      {
         public class Root : AggregateTevent, IRoot
         {
            protected Root() {}
            protected Root(Guid aggregateId) : base(aggregateId) {}
         }

         public class Created() : Root(UserRegistrarAggregate.SingleId), IAggregateCreatedTevent;
      }
   }

   public class UserRegistrarAggregate : Aggregate<UserRegistrarAggregate, UserRegistrarEvent.IRoot, UserRegistrarEvent.Implementation.Root>
   {
      internal static Guid SingleId = Guid.Parse("5C400DD9-50FB-40C7-8A13-265005588AED");

      internal static UserRegistrarAggregate Create()
      {
         var registrar = new UserRegistrarAggregate();
         registrar.Publish(new UserRegistrarEvent.Implementation.Created());
         return registrar;
      }

      UserRegistrarAggregate() : base(DateTimeNowTimeSource.Instance)
         => RegisterEventAppliers()
           .IgnoreUnhandled<UserRegistrarEvent.IRoot>();

      internal static RegisterUserResult RegisterUser(IRemoteHypermediaNavigator navigator) => UserRegistrarCommand.RegisterUserTommand.Create().PostOn(navigator);
   }

   public class UserAggregate : Aggregate<UserAggregate, UserEvent.IRoot, UserEvent.Implementation.Root>
   {
      UserAggregate() : base(DateTimeNowTimeSource.Instance)
         => RegisterEventAppliers()
           .IgnoreUnhandled<UserEvent.IRoot>();

      internal static UserAggregate Register(UserRegistrarCommand.RegisterUserTommand tommand)
      {
         var registered = new UserAggregate();
         registered.Publish(new UserEvent.Implementation.UserRegisteredEvent(tommand.UserId));
         return registered;
      }
   }

   public class GetUserTuery(Guid userId) : TessageTypes.Remotable.NonTransactional.Queries.Tuery<UserResource>
   {
      public Guid UserId { get; private set; } = userId;
   }

   public class UserResource(IEnumerable<IAggregateTevent> history)
   {
      public IEnumerable<IAggregateTevent> History { get; } = history;
   }

   public class RegisterUserResult(Guid userId)
   {
      public GetUserTuery UserLink { get; private set; } = new(userId);
   }
}
