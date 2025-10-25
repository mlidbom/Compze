// ReSharper disable All
#pragma warning disable

namespace Website.docs.tessaging;

class Basics
{
   public void DoNothing() {}

   #region tessage_handler
   void Handle(RegisterAccountCommand command) {}
   #endregion

   #region register_account_command_handler
   class RegisterAccountCommandHandler : ITessageHandler<RegisterAccountCommand>
   {
      public void Handle(RegisterAccountCommand command) {}
   }
   #endregion
}

class RegisterAccountCommand {}

interface ITessageHandler<T>
{
   void Handle(T command);
}
