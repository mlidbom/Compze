﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Messaging;
using Compze.Messaging.Buses;
using Compze.Messaging.Hypermedia;
using Compze.Testing;
using Compze.Testing.DependencyInjection;
using Compze.Testing.Messaging.Buses;
using Compze.Testing.Persistence;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace Compze.Tests.Integration.Messaging.ServiceBusSpecification;

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
            builder.RegisterCurrentTestsConfiguredPersistenceLayer();
            builder.RegisterHandlers
                   .ForQuery((GetUserQuery query) => queryResults.Single(result => result.Name == query.Name))
                   .ForQuery((UserApiStartPageQuery _) => new UserApiStartPage())
                   .ForCommandWithResult((RegisterUserCommand command, IServiceBusSession _) =>
                    {
                       queryResults.Add(new UserResource(command.Name));
                       return new UserRegisteredConfirmationResource(command.Name);
                    });

            builder.TypeMapper
                   .Map<GetUserQuery>("44b8b0b6-fe09-4e3b-a22c-8d09bd51dbb0")
                   .Map<RegisterUserCommand>("ed799a31-0de9-41ae-ae7a-421438f2d857")
                   .Map<UserApiStartPageQuery>("4367ec6e-ddbc-42ea-91ad-9af1e6e4e29a")
                   .Map<UserRegisteredConfirmationResource>("c60604b2-2917-450b-bcbf-7d023065c005")
                   .Map<UserApiStartPage>("10b699df-35ac-430b-acb5-131df3cec5e1")
                   .Map<UserResource>("7e2c57ef-e079-4615-a402-1a76c70b5b0b");
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

   class UserApiStartPage
   {
      public static UserApiStartPageQuery Self => new();
      public RegisterUserCommand RegisterUser(string userName) => RegisterUserCommand.Create(userName);
   }

   protected class GetUserQuery(string name) : MessageTypes.Remotable.NonTransactional.Queries.Query<UserResource>
   {
      public string Name { get; private set; } = name;
   }

   protected class UserResource(string name)
   {
      public string Name { get; private set; } = name;
   }

   protected class RegisterUserCommand : MessageTypes.Remotable.AtMostOnce.AtMostOnceCommand<UserRegisteredConfirmationResource>
   {
      RegisterUserCommand() : base(DeduplicationIdHandling.Reuse) {}

      public static RegisterUserCommand Create(string name) => new()
                                                               {
                                                                  Name = name,
                                                                  MessageId = Guid.NewGuid()
                                                               };

      public string Name { get; private set; } = "";
   }

   protected class UserRegisteredConfirmationResource(string name)
   {
      public GetUserQuery User => new(Name);
      public string Name { get; } = name;
   }

   class UserApiStartPageQuery : MessageTypes.Remotable.NonTransactional.Queries.Query<UserApiStartPage>;
}