// ReSharper disable All
#pragma warning disable //Reviewed OK: This is API experimental code that is never ever used.
namespace Compze.Tests.ScratchPad.APIDraft;

interface IAccountTevent { }

class SendAccountRegistrationWelcomeEmailTommand { }

class AccountTaggregate { }
class AccountReadModel { }
class EmailToAccountLookupModel { }

class GetAccountTuery { }
class AccountCreatedTevent { }
class CreateAccountTommand { }

class AccountTueryHandler
{
   public string Handle(GetAccountTuery tuery) => string.Empty;
}

class AccountQueryModelUpdater
{
   public void Handle(AccountCreatedTevent tevent) { }
}

class AccountTommandHandler
{
   public void Handle(CreateAccountTommand tommand) { }
}

class AccountController
{
   public string Handle(GetAccountTuery tuery) => string.Empty;
   public void Handle(AccountCreatedTevent tevent) { }
   public void Handle(CreateAccountTommand tommand) { }
}