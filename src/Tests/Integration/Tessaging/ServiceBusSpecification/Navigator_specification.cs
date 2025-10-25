using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.AspNetCore.Wiring;
using Compze.Tessaging.Hosting.Testing.Sql;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using FluentAssertions;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification;

public class Navigator_specification : UniversalTestBase
{
   readonly ITestingEndpointHost _host;
   readonly IEndpoint _clientEndpoint;

   public Navigator_specification()
   {
      var tueryResults = new List<UserResource>();

      _host = TestingEndpointHost.Create(TestingContainerFactory.CreateWithRegisteredServiceLocator);

      _host.RegisterEndpoint(
         "Backend",
         new EndpointId(Guid.Parse("3A1B6A8C-D232-476C-A15A-9C8295413210")),
         builder =>
         {
            builder.Container.Register()
                   .AspNetCoreTransport()
                   .CurrentTestsConfiguredSqlLayer();
            builder.RegisterHandlers
                   .ForTuery((GetUserTuery tuery) => tueryResults.Single(result => result.Name == tuery.Name))
                   .ForTuery((UserApiStartPageTuery _) => new UserApiStartPage())
                   .ForTommandWithResult((RegisterUserTommand tommand, IServiceBusSession _) =>
                    {
                       tueryResults.Add(new UserResource(tommand.Name));
                       return new UserRegisteredConfirmationResource(tommand.Name);
                    });
         });

      _clientEndpoint = _host.RegisterClientEndpointForRegisteredEndpoints();
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   [PCT]  public void Can_get_tommand_result()
   {
      var tommandResult1 = _clientEndpoint.ExecuteClientRequest(navigator => navigator.Post(RegisterUserTommand.Create("new-user-name")));
      tommandResult1.Name.Should().Be("new-user-name");
   }

   [PCT]  public void Can_navigate_to_startpage_execute_tommand_and_follow_tommand_result_link_to_the_created_resource()
   {
      var userResource = _clientEndpoint.ExecuteClientRequest(NavigationSpecification.Get(UserApiStartPage.Self)
                                                                                     .Post(startpage => startpage.RegisterUser("new-user-name"))
                                                                                     .Get(registerUserResult => registerUserResult.User));

      userResource.Name.Should().Be("new-user-name");
   }

   [PCT]  public async Task Can_navigate_async_to_startpage_execute_tommand_and_follow_tommand_result_link_to_the_created_resource()
   {
      var userResource = _clientEndpoint.ExecuteRequestAsync(NavigationSpecification.Get(UserApiStartPage.Self)
                                                                                    .Post(startpage => startpage.RegisterUser("new-user-name"))
                                                                                    .Get(registerUserResult => registerUserResult.User));

      (await userResource).Name.Should().Be("new-user-name");
   }

   protected internal class UserApiStartPage
   {
      public static UserApiStartPageTuery Self => new();
      public RegisterUserTommand RegisterUser(string userName) => RegisterUserTommand.Create(userName);
   }

   protected internal class GetUserTuery(string name) : TessageTypes.Remotable.NonTransactional.Queries.Tuery<UserResource>
   {
      public string Name { get; private set; } = name;
   }

   protected internal class UserResource(string name)
   {
      public string Name { get; private set; } = name;
   }

   protected internal class RegisterUserTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTommand<UserRegisteredConfirmationResource>
   {
      RegisterUserTommand() : base(DeduplicationIdHandling.Reuse) {}

      public static RegisterUserTommand Create(string name) => new()
                                                               {
                                                                  Name = name,
                                                                  TessageId = Guid.CreateVersion7()
                                                               };

      public string Name { get; private set; } = "";
   }

   protected internal class UserRegisteredConfirmationResource(string name)
   {
      public GetUserTuery User => new(Name);
      public string Name { get; } = name;
   }

   protected internal class UserApiStartPageTuery : TessageTypes.Remotable.NonTransactional.Queries.Tuery<UserApiStartPage>;
}
