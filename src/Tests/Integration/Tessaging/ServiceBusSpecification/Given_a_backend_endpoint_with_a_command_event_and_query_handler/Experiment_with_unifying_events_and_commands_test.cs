using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Abstractions.Internal.Time;
using FluentAssertions;
using NUnit.Framework;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.AspNetCore.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.Sql;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Teventive;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Tessaging.Teventive.EventStore.DependencyInjection;
using Compze.Tessaging.Typermedia.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Tests.Infrastructure.NUnit;

// ReSharper disable MemberCanBeInternal for testing
// ReSharper disable InconsistentNaming for testing
#pragma warning disable CA1724 // Type names should not match namespaces

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Experiment_with_unifying_events_and_commands_test(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   ITestingEndpointHost _host;

   IServiceLocator _userDomainServiceLocator;
   IEndpoint _clientEndpoint;

   IRemoteHypermediaNavigator RemoteNavigator => _clientEndpoint.ServiceLocator.Resolve<IRemoteHypermediaNavigator>();

   [SetUp] public async Task Setup()
   {
      _host = TestingEndpointHost.Create(TestingContainerFactory.CreateWithRegisteredServiceLocator);

      var userManagementDomainEndpoint = _host.RegisterEndpoint(
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
                   .ForQuery((GetUserQuery query, IEventStoreReader eventReader) => new UserResource(eventReader.GetHistory(query.UserId)))
                   .ForCommandWithResult((UserRegistrarCommand.RegisterUserCommand command, IEventStoreUpdater store) =>
                    {
                       store.Save(UserAggregate.Register(command));
                       return new RegisterUserResult(command.UserId);
                    });
         });

      _clientEndpoint = _host.RegisterClientEndpointForRegisteredEndpoints();

      await _host.StartAsync();

      _userDomainServiceLocator = userManagementDomainEndpoint.ServiceLocator;

      _userDomainServiceLocator.ExecuteTransactionInIsolatedScope(() => _userDomainServiceLocator.Resolve<IEventStoreUpdater>().Save(UserRegistrarAggregate.Create()));
   }

   [Test] public void Can_register_user_and_fetch_user_resource()
   {
      var registrationResult = _userDomainServiceLocator.ExecuteInIsolatedScope(() => UserRegistrarAggregate.RegisterUser(_userDomainServiceLocator.Resolve<IRemoteHypermediaNavigator>()));

      var user = _clientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => RemoteNavigator.Get(registrationResult.UserLink));

      user.Should().NotBe(null);
      user.History.Count().Should().Be(1);
   }

   [TearDown] public async Task TeardownAsync() => await _host.DisposeAsync();

   public static class UserEvent
   {
      public interface IRoot : IAggregateEvent;

      public interface IUserRegistered : IRoot, IAggregateCreatedEvent;

      public static class Implementation
      {
         public class Root : AggregateEvent, IRoot
         {
            protected Root() {}
            protected Root(Guid aggregateId) : base(aggregateId) {}
         }

         public class UserRegisteredEvent(Guid userId) : Root(userId), IUserRegistered;
      }
   }

   public static class UserRegistrarCommand
   {
      public class RegisterUserCommand : MessageTypes.Remotable.AtMostOnce.AtMostOnceCommand<RegisterUserResult>
      {
         public Guid UserId { get; private set; } = Guid.NewGuid();

         RegisterUserCommand() : base(DeduplicationIdHandling.Reuse) {}

         internal static RegisterUserCommand Create() => new() { MessageId = Guid.CreateVersion7() };
      }
   }

   public static class UserRegistrarEvent
   {
      public interface IRoot : IAggregateEvent;

      public static class Implementation
      {
         public class Root : AggregateEvent, IRoot
         {
            protected Root() {}
            protected Root(Guid aggregateId) : base(aggregateId) {}
         }

         public class Created() : Root(UserRegistrarAggregate.SingleId), IAggregateCreatedEvent;
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

      internal static RegisterUserResult RegisterUser(IRemoteHypermediaNavigator navigator) => UserRegistrarCommand.RegisterUserCommand.Create().PostOn(navigator);
   }

   public class UserAggregate : Aggregate<UserAggregate, UserEvent.IRoot, UserEvent.Implementation.Root>
   {
      UserAggregate() : base(DateTimeNowTimeSource.Instance)
         => RegisterEventAppliers()
           .IgnoreUnhandled<UserEvent.IRoot>();

      internal static UserAggregate Register(UserRegistrarCommand.RegisterUserCommand command)
      {
         var registered = new UserAggregate();
         registered.Publish(new UserEvent.Implementation.UserRegisteredEvent(command.UserId));
         return registered;
      }
   }

   public class GetUserQuery(Guid userId) : MessageTypes.Remotable.NonTransactional.Queries.Query<UserResource>
   {
      public Guid UserId { get; private set; } = userId;
   }

   public class UserResource(IEnumerable<IAggregateEvent> history)
   {
      public IEnumerable<IAggregateEvent> History { get; } = history;
   }

   public class RegisterUserResult(Guid userId)
   {
      public GetUserQuery UserLink { get; private set; } = new(userId);
   }
}
