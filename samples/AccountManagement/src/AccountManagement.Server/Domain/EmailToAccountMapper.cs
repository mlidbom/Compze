using AccountManagement.Domain.Tevents;
using Compze.DocumentDb;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using JetBrains.Annotations;
using AccountLink = Compze.Teventive.TeventStore.Typermedia.TeventStoreApi.TueryApi.TaggregateLink<AccountManagement.Domain.Account>;
using Compze.Typermedia;
using Compze.Typermedia.HandlerRegistration;

namespace AccountManagement.Domain;

[UsedImplicitly] class EmailToAccountMapper
{
   static DocumentDbApi DocumentDb => new();

   internal static void UpdateMappingWhenEmailChanges(ITessageHandlerRegistrar registrar) => registrar.ForTevent(
      (IAccountTevent.PropertyUpdated.Email emailUpdated, ILocalTypermediaNavigatorSession navigator) =>
      {
         if(emailUpdated.TaggregateVersion > 1)
         {
            var previousAccountVersion = navigator.Execute(InternalApi.Tueries.GetReadOnlyCopyOfVersion(emailUpdated.TaggregateId, emailUpdated.TaggregateVersion - 1));
            navigator.Execute(DocumentDb.Tommands.Delete<AccountLink>(previousAccountVersion.Email.StringValue));
         }

         var newEmail = emailUpdated.Email;
         navigator.Execute(DocumentDb.Tommands.Save(newEmail.StringValue, InternalApi.Tueries.GetForUpdate(emailUpdated.TaggregateId)));
      });

   internal static void TryGetAccountByEmail(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
      (InternalApi.Tuery.TryGetByEmailTuery tuery, ILocalTypermediaNavigatorSession navigator) =>
         navigator.Execute(DocumentDb.Tueries.TryGet<AccountLink>(tuery.Email.StringValue)) is { } accountLink
            ? navigator.Execute(accountLink)
            : null);
}