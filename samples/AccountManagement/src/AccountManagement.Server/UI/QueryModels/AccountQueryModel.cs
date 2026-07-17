using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Tevents;
using AccountManagement.Domain.Passwords;
using CommunityToolkit.Diagnostics;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;
using Compze.Teventive.TeventStore.Typermedia;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Typermedia.HandlerRegistration;

namespace AccountManagement.UI.QueryModels;

class AccountQueryModel : SelfGeneratingQueryModel<AccountQueryModel, IAccountTevent>, IAccountResourceData
{
   public Password Password { get; private set; } = null!; //Nullable status guaranteed by AssertInvariantsAreMet
   public Email Email { get; private set; } = null!;       //Nullable status guaranteed by AssertInvariantsAreMet

   AccountQueryModel(IEnumerable<ITaggregateTevent<IAccountTevent>> tevents) : base(TeventDispatcherConfig.Default.IgnoreUnhandled<IAccountTevent.LoginAttempted>()) //Login tevents change no account state, so they have no appliers.
   {
      RegisterTeventAppliers()
        .For<IAccountTevent.PropertyUpdated.Email>(tevent => Email = tevent.Email)
        .For<IAccountTevent.PropertyUpdated.Password>(tevent => Password = tevent.Password);

      LoadFromHistory(tevents);
   }

   AccountId IAccountResourceData.Id => (AccountId)base.Id;

   protected override void AssertInvariantsAreMet()
   {
      Guard.IsNotNull(Email);
      Guard.IsNotNull(Password);
   }

   // ReSharper disable MemberCanBeMadeStatic.Global fluent composable APIs and statics do not mix
   internal class Api
   {
      internal Tuery Tueries => new();
      internal class Tuery
      {
         public TessageTypes.StrictlyLocal.Tueries.EntityLink<AccountQueryModel> Get(EntityId id) => new(id);
      }

      public static void RegisterHandlers(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => Get(registrar);

      static void Get(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
         (TessageTypes.StrictlyLocal.Tueries.EntityLink<AccountQueryModel> tuery, ILocalTypermediaNavigatorSession navigator) =>
            //todo this Id conversion feels iffy
            new AccountQueryModel(navigator.Execute(new TeventStoreApi().Tueries.GetHistory<IAccountTevent>(new TaggregateId(tuery.EntityId.Value)))));
   }
}
