using System;
using System.Collections.Generic;
using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Tevents;
using AccountManagement.Domain.Passwords;
using CommunityToolkit.Diagnostics;
using Compze.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Typermedia.Public;
using Compze.Tessaging.Teventive.TeventStore.Tuery.Models.SelfGeneratingQueryModels;
using Compze.Tessaging.TyperMediaApi.TeventStore;

namespace AccountManagement.UI.QueryModels;

class AccountQueryModel : SelfGeneratingQueryModel<AccountQueryModel, AccountTevent.Root>, IAccountResourceData
{
   public Password Password { get; private set; } = null!; //Nullable status guaranteed by AssertInvariantsAreMet
   public Email Email { get; private set; } = null!;       //Nullable status guaranteed by AssertInvariantsAreMet

   AccountQueryModel(IEnumerable<AccountTevent.Root> tevents)
   {
      RegisterTeventAppliers()
        .For<AccountTevent.PropertyUpdated.Email>(@tevent => Email = @tevent.Email)
        .For<AccountTevent.PropertyUpdated.Password>(@tevent => Password = @tevent.Password)
        .IgnoreUnhandled<AccountTevent.LoginAttempted>();

      LoadFromHistory(tevents);
   }

   protected override void AssertInvariantsAreMet()
   {
      Guard.IsNotNull(Email);
      Guard.IsNotNull(Password);
   }

   // ReSharper disable MemberCanBeMadeStatic.Global fluent composable APIs and statics do not mix
   internal class Api
   {
      internal Tuery Queries => new();
      internal class Tuery
      {
         public TessageTypes.StrictlyLocal.Queries.EntityLink<AccountQueryModel> Get(Guid id) => new(id);
      }

      public static void RegisterHandlers(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => Get(registrar);

      static void Get(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
         (TessageTypes.StrictlyLocal.Queries.EntityLink<AccountQueryModel> tuery, IInProcessHypermediaNavigator navigator) =>
            new AccountQueryModel(navigator.Execute(new TeventStoreApi().Queries.GetHistory<AccountTevent.Root>(tuery.EntityId))));
   }
}