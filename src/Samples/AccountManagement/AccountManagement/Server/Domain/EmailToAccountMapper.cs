using AccountManagement.Domain.Tevents;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.DocumentDb;
using Compze.Utilities.Functional;
using JetBrains.Annotations;
using AccountLink = Compze.Tessaging.TyperMediaApi.EventStore.TeventStoreApi.TueryApi.TaggregateLink<AccountManagement.Domain.Account>;

namespace AccountManagement.Domain;

[UsedImplicitly] class EmailToAccountMapper
{
   static DocumentDbApi DocumentDb => new();

   internal static void UpdateMappingWhenEmailChanges(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTevent(
      (AccountTevent.PropertyUpdated.Email emailUpdated, IInProcessHypermediaNavigator navigator) =>
      {
         if(emailUpdated.TaggregateVersion > 1)
         {
            var previousAccountVersion = navigator.Execute(InternalApi.Queries.GetReadOnlyCopyOfVersion(emailUpdated.TaggregateId, emailUpdated.TaggregateVersion - 1));
            navigator.Execute(DocumentDb.Tommands.Delete<AccountLink>(previousAccountVersion.Email.StringValue));
         }

         var newEmail = emailUpdated.Email;
         navigator.Execute(DocumentDb.Tommands.Save(newEmail.StringValue, InternalApi.Queries.GetForUpdate(emailUpdated.TaggregateId)));
      });

   internal static void TryGetAccountByEmail(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
      (InternalApi.Tuery.TryGetByEmailTuery tuery, IInProcessHypermediaNavigator navigator) =>
         navigator.Execute(DocumentDb.Queries.TryGet<AccountLink>(tuery.Email.StringValue)) is Some<AccountLink> accountLink
            ? Option.Some(navigator.Execute(accountLink.Value))
            : Option.None<Account>());
}