// ReSharper disable All
#pragma warning disable

namespace Website.docs.tessaging;

public class Basics
{
   public void DoNothing() {}

   #region tessage_handler
   void Handle(RegisterAccountTommand tommand) {}
   #endregion

   #region register_account_tommand_handler
   public class RegisterAccountTommandHandler : ITessageHandler<RegisterAccountTommand>
   {
      public void Handle(RegisterAccountTommand tommand) {}
   }
   #endregion
}

public class RegisterAccountTommand {}

public interface ITessageHandler<T>
{
   void Handle(T tommand);
}
