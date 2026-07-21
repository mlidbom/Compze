using Compze.Tessaging.Typermedia;

namespace AccountManagement.UI;

static class UIAdapterLayer
{
   public static void Register(TypermediaHandlerRegistrar registrar)
   {
      AccountUIAdapter.GetById(registrar);
      AccountUIAdapter.Register(registrar);
      AccountUIAdapter.ChangeEmail(registrar);
      AccountUIAdapter.ChangePassword(registrar);
      AccountUIAdapter.Login(registrar);
   }
}