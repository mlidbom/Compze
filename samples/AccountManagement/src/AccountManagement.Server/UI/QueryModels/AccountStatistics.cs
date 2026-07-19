using AccountManagement.Domain.Tevents;
using Compze.DocumentDb;
using Compze.Abstractions.Public;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Engine;
using Compze.DependencyInjection;
using Compze.Tessaging.Endpoints.ExactlyOnce;
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

   internal static void Register(ExactlyOnceEndpointBuilder endpointBuilder)
   {
      endpointBuilder.Registrar.Register(Singleton.For<StatisticsSingletonInitializer>().CreatedBy(() => new StatisticsSingletonInitializer()));
      endpointBuilder.RegisterTessageHandlers(MaintainStatisticsWhenRelevantTeventsAreReceived);
   }

   class StatisticsSingletonInitializer
   {
      readonly DocumentDbApi _documentDbApi = new();

      ///<summary>Ensures the singleton document exists, inside the caller's own transaction and idempotently per invocation:<br/>
      /// no in-memory initialized-flag, deliberately — such a flag set inside a transaction outlives its rollback, and a<br/>
      /// failed first handling attempt would then skip initialization on every retry. The tevent handlers this serves never<br/>
      /// run in parallel with each other, so the check-then-save cannot race itself.</summary>
      public void EnsureInitialized(ILocalTypermediaNavigatorSession navigator)
      {
         if(navigator.Execute(_documentDbApi.Tueries.TryGet<SingletonStatisticsQueryModel>(SingletonStatisticsQueryModel.StaticId)) is null)
         {
            navigator.Execute(_documentDbApi.Tommands.Save(new SingletonStatisticsQueryModel()));
         }
      }
   }
}
