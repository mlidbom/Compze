using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.AspNetCore.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.Persistence;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Typermedia.Abstractions;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification;

public class Navigator_specification(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   ITestingEndpointHost _host;
   IEndpoint _clientEndpoint;

   [SetUp] public async Task Setup()
   {
      var queryResults = new List<UserResource>();

      _host = TestingEndpointHost.Create(TestingContainerFactory.Create);

      _host.RegisterEndpoint(
         "Backend",
         new EndpointId(Guid.Parse("3A1B6A8C-D232-476C-A15A-9C8295413210")),
         builder =>
         {
            builder.Container.Register()
                   .AspNetCoreTransport()
                   .CurrentTestsConfiguredPersistenceLayer();
            builder.RegisterHandlers
                   .ForQuery((GetUserQuery query) => queryResults.Single(result => result.Name == query.Name))
                   .ForQuery((UserApiStartPageQuery _) => new UserApiStartPage())
                   .ForCommandWithResult((RegisterUserCommand command, IServiceBusSession _) =>
                    {
                       queryResults.Add(new UserResource(command.Name));
                       return new UserRegisteredConfirmationResource(command.Name);
                    });
         });

      _clientEndpoint = _host.RegisterClientEndpointForRegisteredEndpoints();

      await _host.StartAsync();
   }

   [TearDown] public async Task TearDown() => await _host.DisposeAsync();

   [Test] public void Can_get_command_result()
   {
      var commandResult1 = _clientEndpoint.ExecuteClientRequest(navigator => navigator.Post(RegisterUserCommand.Create("new-user-name")));
      commandResult1.Name.Should().Be("new-user-name");
   }

   [Test] public void Can_navigate_to_startpage_execute_command_and_follow_command_result_link_to_the_created_resource()
   {
      var userResource = _clientEndpoint.ExecuteClientRequest(NavigationSpecification.Get(UserApiStartPage.Self)
                                                                                     .Post(startpage => startpage.RegisterUser("new-user-name"))
                                                                                     .Get(registerUserResult => registerUserResult.User));

      userResource.Name.Should().Be("new-user-name");
   }

   [Test] public async Task Can_navigate_async_to_startpage_execute_command_and_follow_command_result_link_to_the_created_resource()
   {
      var userResource = _clientEndpoint.ExecuteRequestAsync(NavigationSpecification.Get(UserApiStartPage.Self)
                                                                                    .Post(startpage => startpage.RegisterUser("new-user-name"))
                                                                                    .Get(registerUserResult => registerUserResult.User));

      (await userResource).Name.Should().Be("new-user-name");
   }

   protected internal class UserApiStartPage
   {
      public static UserApiStartPageQuery Self => new();
      public RegisterUserCommand RegisterUser(string userName) => RegisterUserCommand.Create(userName);
   }

   protected internal class GetUserQuery(string name) : MessageTypes.Remotable.NonTransactional.Queries.Query<UserResource>
   {
      public string Name { get; private set; } = name;
   }

   protected internal class UserResource(string name)
   {
      public string Name { get; private set; } = name;
   }

   protected internal class RegisterUserCommand : MessageTypes.Remotable.AtMostOnce.AtMostOnceCommand<UserRegisteredConfirmationResource>
   {
      RegisterUserCommand() : base(DeduplicationIdHandling.Reuse) {}

      public static RegisterUserCommand Create(string name) => new()
                                                               {
                                                                  Name = name,
                                                                  MessageId = Guid.CreateVersion7()
                                                               };

      public string Name { get; private set; } = "";
   }

   protected internal class UserRegisteredConfirmationResource(string name)
   {
      public GetUserQuery User => new(Name);
      public string Name { get; } = name;
   }

   protected internal class UserApiStartPageQuery : MessageTypes.Remotable.NonTransactional.Queries.Query<UserApiStartPage>;
}
