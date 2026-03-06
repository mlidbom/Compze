using AccountManagement.Domain.Tevents;
using Compze.DocumentDb;
using Compze.Core.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using JetBrains.Annotations;
using AccountLink = Compze.Tessaging.TyperMediaApi.EventStore.TeventStoreApi.TueryApi.TaggregateLink<AccountManagement.Domain.Account>;

namespace AccountManagement.Domain;

[UsedImplicitly] class EmailToAccountMapper
{
   static DocumentDbApi DocumentDb => new();

   internal static void UpdateMappingWhenEmailChanges(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTevent(
      (IAccountTevent.PropertyUpdated.Email emailUpdated, IInProcessTypermediaNavigator navigator) =>
      {
         if(emailUpdated.TaggregateVersion > 1)
         {
            var previousAccountVersion = navigator.Execute(InternalApi.Tueries.GetReadOnlyCopyOfVersion(emailUpdated.TaggregateId, emailUpdated.TaggregateVersion - 1));
            navigator.Execute(DocumentDb.Tommands.Delete<AccountLink>(previousAccountVersion.Email.StringValue));
         }

         var newEmail = emailUpdated.Email;
         navigator.Execute(DocumentDb.Tommands.Save(newEmail.StringValue, InternalApi.Tueries.GetForUpdate(emailUpdated.TaggregateId)));
      });

   internal static void TryGetAccountByEmail(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
      (InternalApi.Tuery.TryGetByEmailTuery tuery, IInProcessTypermediaNavigator navigator) =>
         navigator.Execute(DocumentDb.Tueries.TryGet<AccountLink>(tuery.Email.StringValue)) is { } accountLink
            ? navigator.Execute(accountLink)
            : null);
}