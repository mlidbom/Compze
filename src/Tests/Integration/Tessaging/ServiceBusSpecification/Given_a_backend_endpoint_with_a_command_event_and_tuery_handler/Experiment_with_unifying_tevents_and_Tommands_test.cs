using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TEventStore.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Core.Time.Public;
using Compze.Tessaging.Hosting.AspNetCore.Wiring;
using Compze.Tessaging.Hosting.Testing.Sql;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.Teventive.TeventStore.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Tests.Infrastructure.XUnit;
using FluentAssertions;

// ReSharper disable MemberCanBeInternal for testing
// ReSharper disable InconsistentNaming for testing
#pragma warning disable CA1724 // Type names should not match namespaces

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Experiment_with_unifying_tevents_and_tommands_test : UniversalTestBase
{
   readonly ITestingEndpointHost _host;

   IServiceLocator _userDomainServiceLocator = null!;
   readonly IEndpoint _clientEndpoint;
   readonly IEndpoint _userManagementDomainEndpoint;

   IRemoteHypermediaNavigator RemoteNavigator => _clientEndpoint.ServiceLocator.Resolve<IRemoteHypermediaNavigator>();

   public Experiment_with_unifying_tevents_and_tommands_test()
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
            builder.Container.Register().TeventStore(builder.Configuration.ConnectionStringName);

            builder.RegisterHandlers
                   .ForTevent((UserTevent.IUserRegistered _) => {})
                   .ForTuery((GetUserTuery tuery, ITeventStoreReader teventReader) => new UserResource(teventReader.GetHistory(tuery.UserId)))
                   .ForTommandWithResult((UserRegistrarTommand.RegisterUserTommand tommand, ITeventStoreUpdater store) =>
                    {
                       store.Save(UserTaggregate.Register(tommand));
                       return new RegisterUserResult(tommand.UserId);
                    });
         });

      _clientEndpoint = _host.RegisterClientEndpointForRegisteredEndpoints();
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync();

      _userDomainServiceLocator = _userManagementDomainEndpoint.ServiceLocator;

      _userDomainServiceLocator.ExecuteTransactionInIsolatedScope(() => _userDomainServiceLocator.Resolve<ITeventStoreUpdater>().Save(UserRegistrarTaggregate.Create()));
   }

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   [PCT] public void Can_register_user_and_fetch_user_resource()
   {
      var registrationResult = _userDomainServiceLocator.ExecuteInIsolatedScope(() => UserRegistrarTaggregate.RegisterUser(_userDomainServiceLocator.Resolve<IRemoteHypermediaNavigator>()));

      var user = _clientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => RemoteNavigator.Get(registrationResult.UserLink));

      user.Should().NotBe(null);
      user.History.Count().Should().Be(1);
   }

   public static class UserTevent
   {
      public interface IRoot : ITaggregateTevent;

      public interface IUserRegistered : IRoot, ITaggregateCreatedTevent;

      public static class Implementation
      {
         public class Root : TaggregateTevent, IRoot
         {
            protected Root() {}
            protected Root(Guid taggregateId) : base(taggregateId) {}
         }

         public class UserRegisteredTevent(Guid userId) : Root(userId), IUserRegistered;
      }
   }

   public static class UserRegistrarTommand
   {
      public class RegisterUserTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTommand<RegisterUserResult>
      {
         public Guid UserId { get; private set; } = Guid.NewGuid();

         RegisterUserTommand() : base(DeduplicationIdHandling.Reuse) {}

         internal static RegisterUserTommand Create() => new() { TessageId = Guid.CreateVersion7() };
      }
   }

   public static class UserRegistrarTevent
   {
      public interface IRoot : ITaggregateTevent;

      public static class Implementation
      {
         public class Root : TaggregateTevent, IRoot
         {
            protected Root() {}
            protected Root(Guid taggregateId) : base(taggregateId) {}
         }

         public class Created() : Root(UserRegistrarTaggregate.SingleId), ITaggregateCreatedTevent;
      }
   }

   public class UserRegistrarTaggregate : Taggregate<UserRegistrarTaggregate, UserRegistrarTevent.IRoot, UserRegistrarTevent.Implementation.Root>
   {
      internal static Guid SingleId = Guid.Parse("5C400DD9-50FB-40C7-8A13-265005588AED");

      internal static UserRegistrarTaggregate Create()
      {
         var registrar = new UserRegistrarTaggregate();
         registrar.Publish(new UserRegistrarTevent.Implementation.Created());
         return registrar;
      }

      UserRegistrarTaggregate() : base(DateTimeNowTimeSource.Instance)
         => RegisterTeventAppliers()
           .IgnoreUnhandled<UserRegistrarTevent.IRoot>();

      internal static RegisterUserResult RegisterUser(IRemoteHypermediaNavigator navigator) => UserRegistrarTommand.RegisterUserTommand.Create().PostOn(navigator);
   }

   public class UserTaggregate : Taggregate<UserTaggregate, UserTevent.IRoot, UserTevent.Implementation.Root>
   {
      UserTaggregate() : base(DateTimeNowTimeSource.Instance)
         => RegisterTeventAppliers()
           .IgnoreUnhandled<UserTevent.IRoot>();

      internal static UserTaggregate Register(UserRegistrarTommand.RegisterUserTommand tommand)
      {
         var registered = new UserTaggregate();
         registered.Publish(new UserTevent.Implementation.UserRegisteredTevent(tommand.UserId));
         return registered;
      }
   }

   public class GetUserTuery(Guid userId) : TessageTypes.Remotable.NonTransactional.Queries.Tuery<UserResource>
   {
      public Guid UserId { get; private set; } = userId;
   }

   public class UserResource(IEnumerable<ITaggregateTevent> history)
   {
      public IEnumerable<ITaggregateTevent> History { get; } = history;
   }

   public class RegisterUserResult(Guid userId)
   {
      public GetUserTuery UserLink { get; private set; } = new(userId);
   }
}
