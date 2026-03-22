using AccountManagement.Domain.Tevents;
using Compze.DocumentDb;
using Compze.Abstractions.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;
using Compze.DependencyInjection;
using Compze.Typermedia;

namespace AccountManagement.UI.QueryModels;

static class AccountStatistics
{
   /// <summary>
   /// Note that we use a <see cref="SelfGeneratingQueryModel{TQueryModel,TTaggregateTevent}"/> even though we will store it in a document database.
   /// Doing so lets the tuery model cleanly encapsulate how it maintains its own state when it receives tevents.
   /// </summary>
   public class SingletonStatisticsQueryModel : SelfGeneratingQueryModel<SingletonStatisticsQueryModel, IAccountTevent>
   {
      public SingletonStatisticsQueryModel()
      {
         RegisterTeventAppliers()
           .For<IAccountTevent.Created>(_ => NumberOfAccounts++)
           .For<IAccountTevent.LoginAttempted>(_ => NumberOfLoginsAttempts++)
           .For<IAccountTevent.LoggedIn>(_ => NumberOfSuccessfulLogins++)
           .For<IAccountTevent.LoginFailed>(_ => NumberOfFailedLogins++);
      }

      int NumberOfAccounts { get; set; }
      int NumberOfLoginsAttempts { get; set; }
      int NumberOfSuccessfulLogins { get; set; }
      int NumberOfFailedLogins { get; set; }

      //Since this is a singleton tuery model and not bound to a specific Taggregate's tevents we override the Id member to always be the singleton Id.
      public override EntityId Id => new(StaticId);
      internal static Guid StaticId = Guid.Parse("93498554-5C2E-4D6A-862D-2DA7BCCAC747");
   }

   static void MaintainStatisticsWhenRelevantTeventsAreReceived(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTevent(
      (IAccountTevent tevent, IInProcessTypermediaNavigator navigator, StatisticsSingletonInitializer initializer) =>
      {
         initializer.EnsureInitialized(navigator);

         if(new SingletonStatisticsQueryModel().HandlesTevent(tevent))
         {
            navigator.Execute(new DocumentDbApi().Tueries.GetForUpdate<SingletonStatisticsQueryModel>(SingletonStatisticsQueryModel.StaticId))
                     .ApplyTevent(tevent);
         }
      });

   internal static void Register(IEndpointBuilder builder)
   {
      builder.Registrar.Register(Singleton.For<StatisticsSingletonInitializer>().CreatedBy(() => new StatisticsSingletonInitializer()));
      MaintainStatisticsWhenRelevantTeventsAreReceived(builder.RegisterTessagingHandlers);
   }

   class StatisticsSingletonInitializer
   {
      readonly Lock _lock = new();
      bool _isInitialized;
      readonly DocumentDbApi _documentDbApi = new();
      public void EnsureInitialized(IInProcessTypermediaNavigator navigator)
      {
         lock(_lock)
         {
            if(!_isInitialized)
            {
               _isInitialized = true;
               if(navigator.Execute(_documentDbApi.Tueries.TryGet<SingletonStatisticsQueryModel>(SingletonStatisticsQueryModel.StaticId)) is null)
               {
                  navigator.Execute(_documentDbApi.Tommands.Save(new SingletonStatisticsQueryModel()));
               }
            }
         }
      }
   }
}