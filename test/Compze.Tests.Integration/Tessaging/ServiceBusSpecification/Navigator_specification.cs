using Compze.Abstractions.Hosting.Public;
using Compze.Typermedia.Client;
using Compze.Abstractions.Tessaging.Public;
using Compze.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing;
using Compze.Typermedia.Hosting.Testing;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Abstractions.Public;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Must.Assertions;
using Compze.Typermedia;
using Compze.Typermedia.HandlerRegistration;

// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable MemberCanBeMadeStatic.Local

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification;

public class Navigator_specification : UniversalTestBase
{
   readonly ITestingEndpointHost _host;
   readonly IEndpoint _endpoint;
   TypermediaTestClient _client = null!;

   public Navigator_specification()
   {
      var tueryResults = new List<UserResource>();

      _host = TestingEndpointHost.Create(new DistributedTessagingTestingEndpointHostFeature(), new DistributedTypermediaTestingEndpointHostFeature());

      _endpoint = _host.RegisterEndpoint(
         "Backend",
         new EndpointId(Guid.Parse("3A1B6A8C-D232-476C-A15A-9C8295413210")),
         builder =>
         {
            builder.TypeMapper.RegisterIntegrationTestTypeMappings();

            builder.RegisterTypermediaHandlers
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
      _client = await TypermediaTestClient.ConnectTo(_endpoint.TypermediaAddress!, mapper => mapper.RegisterIntegrationTestTypeMappings()).caf();
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _client.DisposeAsync().caf();
      await _host.DisposeAsync().caf();
   }

   [PCT]  public void Can_get_tommand_result()
   {
      var tommandResult1 = _client.Navigator.Post(RegisterUserTypermediaTommand.Create("new-user-name"));
      tommandResult1.Name.Must().Be("new-user-name");
   }

   [PCT]  public void Can_navigate_to_startpage_execute_tommand_and_follow_tommand_result_link_to_the_created_resource()
   {
      var userResource = _client.Navigator.Navigate(NavigationSpecification.Get(UserApiStartPage.Self)
                                                                                     .Post(startpage => startpage.RegisterUser("new-user-name"))
                                                                                     .Get(registerUserResult => registerUserResult.User));

      userResource.Name.Must().Be("new-user-name");
   }

   [PCT]  public async Task Can_navigate_async_to_startpage_execute_tommand_and_follow_tommand_result_link_to_the_created_resource()
   {
      var userResource = await _client.Navigator.NavigateAsync(NavigationSpecification.Get(UserApiStartPage.Self)
                                                                                    .Post(startpage => startpage.RegisterUser("new-user-name"))
                                                                                    .Get(registerUserResult => registerUserResult.User));

      userResource.Name.Must().Be("new-user-name");
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
