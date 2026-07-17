using AccountManagement.Domain.Tevents;
using Compze.DocumentDb;
using Compze.Abstractions.Public;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Engine;
using Compze.DependencyInjection;
using Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;
using Compze.Tessaging.Typermedia.Client;
using Compze.Tessaging.Typermedia;

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

   //Account tevents are exactly-once kinds, and exactly-once kinds are async end to end - so the handler is declared async; its work is synchronous today, so it completes its task synchronously.
   static void MaintainStatisticsWhenRelevantTeventsAreReceived(TessageHandlerRegistrar registrar) => registrar.ForTevent(
      (IAccountTevent tevent, ILocalTypermediaNavigatorSession navigator, StatisticsSingletonInitializer initializer) =>
      {
         initializer.EnsureInitialized(navigator);

         if(new SingletonStatisticsQueryModel().HandlesTevent(tevent))
         {
            navigator.Execute(new DocumentDbApi().Tueries.GetForUpdate<SingletonStatisticsQueryModel>(SingletonStatisticsQueryModel.StaticId))
                     .ApplyTevent(tevent);
         }

         return Task.CompletedTask;
      });

   internal static void Register(ExactlyOnceEndpointBuilder endpoint)
   {
      endpoint.Registrar.Register(Singleton.For<StatisticsSingletonInitializer>().CreatedBy(() => new StatisticsSingletonInitializer()));
      endpoint.RegisterTessageHandlers(MaintainStatisticsWhenRelevantTeventsAreReceived);
   }

   class StatisticsSingletonInitializer
   {
      readonly Lock _lock = new();
      bool _isInitialized;
      readonly DocumentDbApi _documentDbApi = new();
      public void EnsureInitialized(ILocalTypermediaNavigatorSession navigator)
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
