using AccountManagement.Domain.Events;
using Compze.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Typermedia.Public;
using Compze.Sql.DocumentDb;
using Compze.Utilities.Functional;
using JetBrains.Annotations;
using AccountLink = Compze.Tessaging.TyperMediaApi.EventStore.EventStoreApi.TueryApi.AggregateLink<AccountManagement.Domain.Account>;

namespace AccountManagement.Domain;

[UsedImplicitly] class EmailToAccountMapper
{
   static DocumentDbApi DocumentDb => new();

   internal static void UpdateMappingWhenEmailChanges(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForEvent(
      (AccountEvent.PropertyUpdated.Email emailUpdated, IInProcessHypermediaNavigator navigator) =>
      {
         if(emailUpdated.AggregateVersion > 1)
         {
            var previousAccountVersion = navigator.Execute(InternalApi.Queries.GetReadOnlyCopyOfVersion(emailUpdated.AggregateId, emailUpdated.AggregateVersion - 1));
            navigator.Execute(DocumentDb.Tommands.Delete<AccountLink>(previousAccountVersion.Email.StringValue));
         }

         var newEmail = emailUpdated.Email;
         navigator.Execute(DocumentDb.Tommands.Save(newEmail.StringValue, InternalApi.Queries.GetForUpdate(emailUpdated.AggregateId)));
      });

   internal static void TryGetAccountByEmail(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
      (InternalApi.Tuery.TryGetByEmailTuery tuery, IInProcessHypermediaNavigator navigator) =>
         navigator.Execute(DocumentDb.Queries.TryGet<AccountLink>(tuery.Email.StringValue)) is Some<AccountLink> accountLink
            ? Option.Some(navigator.Execute(accountLink.Value))
            : Option.None<Account>());
}