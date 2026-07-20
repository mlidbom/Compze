using Compze.Tessaging.Engine.HandlerRegistration.TessageHandlers;

namespace AccountManagement.UI;

static class UIAdapterLayer
{
   public static void Register(TessageHandlerRegistrar registrar)
   {
      AccountUIAdapter.GetById(registrar);
      AccountUIAdapter.Register(registrar);
      AccountUIAdapter.ChangeEmail(registrar);
      AccountUIAdapter.ChangePassword(registrar);
      AccountUIAdapter.Login(registrar);
   }
}