// ReSharper disable All
#pragma warning disable

namespace Website.docs.messaging;

class Basics
{
   public void DoNothing() {}

   #region message_handler
   void Handle(RegisterAccountCommand command) {}
   #endregion

   #region register_account_command_handler
   class RegisterAccountCommandHandler : IMessageHandler<RegisterAccountCommand>
   {
      public void Handle(RegisterAccountCommand command) {}
   }
   #endregion
}

class RegisterAccountCommand {}

interface IMessageHandler<T>
{
   void Handle(T command);
}
