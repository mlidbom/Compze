// ReSharper disable All
#pragma warning disable //Reviewed OK: This is API experimental code that is never ever used.
namespace Compze.Tests.ScratchPad.APIDraft;

interface IAccountEvent { }

class SendAccountRegistrationWelcomeEmailTommand { }

class AccountAggregate { }
class AccountReadModel { }
class EmailToAccountLookupModel { }

class GetAccountTuery { }
class AccountCreatedEvent { }
class CreateAccountTommand { }

class AccountTueryHandler
{
   public string Handle(GetAccountTuery tuery) => string.Empty;
}

class AccountQueryModelUpdater
{
   public void Handle(AccountCreatedEvent @event) { }
}

class AccountTommandHandler
{
   public void Handle(CreateAccountTommand tommand) { }
}

class AccountController
{
   public string Handle(GetAccountTuery tuery) => string.Empty;
   public void Handle(AccountCreatedEvent @event) { }
   public void Handle(CreateAccountTommand tommand) { }
}