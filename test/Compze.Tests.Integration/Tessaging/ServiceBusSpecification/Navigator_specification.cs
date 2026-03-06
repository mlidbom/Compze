using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Testing.Tessaging;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Abstractions.Public;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Must;
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable MemberCanBeMadeStatic.Local

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification;

public class Navigator_specification : UniversalTestBase
{
   readonly ITestingEndpointHost _host;
   readonly IEndpoint _endpoint;
   IClient _client = null!;

   public Navigator_specification()
   {
      var tueryResults = new List<UserResource>();

      _host = TestingEndpointHost.Create();

      _endpoint = _host.RegisterEndpoint(
         "Backend",
         new EndpointId(Guid.Parse("3A1B6A8C-D232-476C-A15A-9C8295413210")),
         builder =>
         {
            builder.RegisterHandlers
                   .ForTuery((GetUserTuery tuery) => tueryResults.Single(result => result.Name == tuery.Name))
                   .ForTuery((UserApiStartPageTuery _) => new UserApiStartPage())
                   .ForTommandWithResult((RegisterUserTypermediaTommand typermediaTommand, IServiceBusSession _) =>
                    {
                       tueryResults.Add(new UserResource(typermediaTommand.Name));
                       return new UserRegisteredConfirmationResource(typermediaTommand.Name);
                    });
         });
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync().caf();
      _client = await TestClient.ConnectTo(_endpoint.Address!).caf();
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _client.DisposeAsync().caf();
      await _host.DisposeAsync().caf();
   }

   [PCT]  public void Can_get_tommand_result()
   {
      var tommandResult1 = _client.ExecuteRequest(navigator => navigator.Post(RegisterUserTypermediaTommand.Create("new-user-name")));
      tommandResult1.Name.Must().Be("new-user-name");
   }

   [PCT]  public void Can_navigate_to_startpage_execute_tommand_and_follow_tommand_result_link_to_the_created_resource()
   {
      var userResource = _client.ExecuteRequest(NavigationSpecification.Get(UserApiStartPage.Self)
                                                                                     .Post(startpage => startpage.RegisterUser("new-user-name"))
                                                                                     .Get(registerUserResult => registerUserResult.User));

      userResource.Name.Must().Be("new-user-name");
   }

   [PCT]  public async Task Can_navigate_async_to_startpage_execute_tommand_and_follow_tommand_result_link_to_the_created_resource()
   {
      var userResource = _client.ExecuteRequestAsync(NavigationSpecification.Get(UserApiStartPage.Self)
                                                                                    .Post(startpage => startpage.RegisterUser("new-user-name"))
                                                                                    .Get(registerUserResult => registerUserResult.User));

      (await userResource).Name.Must().Be("new-user-name");
   }

   protected internal class UserApiStartPage
   {
      public static UserApiStartPageTuery Self => new();
      public RegisterUserTypermediaTommand RegisterUser(string userName) => RegisterUserTypermediaTommand.Create(userName);
   }

   protected internal class GetUserTuery(string name) : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<UserResource>
   {
      public string Name { get; private set; } = name;
   }

   protected internal class UserResource(string name)
   {
      public string Name { get; private set; } = name;
   }

   protected internal class RegisterUserTypermediaTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand<UserRegisteredConfirmationResource>
   {
      RegisterUserTypermediaTommand() {}

      public static RegisterUserTypermediaTommand Create(string name) => new()
                                                               {
                                                                  Name = name,
                                                                  Id = new TessageId()
                                                               };

      public string Name { get; private init; } = "";
   }

   protected internal class UserRegisteredConfirmationResource(string name)
   {
      public GetUserTuery User => new(Name);
      public string Name { get; } = name;
   }

   protected internal class UserApiStartPageTuery : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<UserApiStartPage>;
}
