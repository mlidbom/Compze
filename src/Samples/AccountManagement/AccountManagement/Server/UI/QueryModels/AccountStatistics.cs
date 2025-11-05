using System;
using System.Threading;
using AccountManagement.Domain.Tevents;
using Compze.Core.DocumentDb;
using Compze.Core.Public;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.Functional;

namespace AccountManagement.UI.QueryModels;

static class AccountStatistics
{
   /// <summary>
   /// Note that we use a <see cref="SelfGeneratingQueryModel{TQueryModel,TTaggregateTevent}"/> even though we will store it in a document database.
   /// Doing so lets the tuery model cleanly encapsulate how it maintains its own state when it receives tevents.
   /// </summary>
   public class SingletonStatisticsQueryModel : SelfGeneratingQueryModel<SingletonStatisticsQueryModel, AccountTevent.Root>
   {
      public SingletonStatisticsQueryModel()
      {
         RegisterTeventAppliers()
           .For<AccountTevent.Created>(_ => NumberOfAccounts++)
           .For<AccountTevent.LoginAttempted>(_ => NumberOfLoginsAttempts++)
           .For<AccountTevent.LoggedIn>(_ => NumberOfSuccessfulLogins++)
           .For<AccountTevent.LoginFailed>(_ => NumberOfFailedLogins++);
      }

      public int NumberOfAccounts { get; private set; }
      public int NumberOfLoginsAttempts { get; private set; }
      public int NumberOfSuccessfulLogins { get; private set; }
      public int NumberOfFailedLogins { get; private set; }

      //Since this is a singleton tuery model and not bound to a specific Taggregate's tevents we override the Id member to always be the singleton Id.
      public override EntityId Id => new(StaticId);
      internal static Guid StaticId = Guid.Parse("93498554-5C2E-4D6A-862D-2DA7BCCAC747");
   }

   static void MaintainStatisticsWhenRelevantTeventsAreReceived(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTevent(
      (AccountTevent.Root tevent, IInProcessTypermediaNavigator navigator, StatisticsSingletonInitializer initializer) =>
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
      builder.Container.Register(Singleton.For<StatisticsSingletonInitializer>().CreatedBy(() => new StatisticsSingletonInitializer()));
      MaintainStatisticsWhenRelevantTeventsAreReceived(builder.RegisterHandlers);
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
               if(navigator.Execute(_documentDbApi.Tueries.TryGet<SingletonStatisticsQueryModel>(SingletonStatisticsQueryModel.StaticId)) is None<SingletonStatisticsQueryModel>)
               {
                  navigator.Execute(_documentDbApi.Tommands.Save(new SingletonStatisticsQueryModel()));
               }
            }
         }
      }
   }
}