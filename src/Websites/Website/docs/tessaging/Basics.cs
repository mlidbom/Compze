// ReSharper disable All
#pragma warning disable

namespace Website.docs.tessaging;

class Basics
{
   public void DoNothing() {}

   #region tessage_handler
   void Handle(RegisterAccountTommand tommand) {}
   #endregion

   #region register_account_tommand_handler
   class RegisterAccountTommandHandler : ITessageHandler<RegisterAccountTommand>
   {
      public void Handle(RegisterAccountTommand tommand) {}
   }
   #endregion
}

class RegisterAccountTommand {}

interface ITessageHandler<T>
{
   void Handle(T tommand);
}
