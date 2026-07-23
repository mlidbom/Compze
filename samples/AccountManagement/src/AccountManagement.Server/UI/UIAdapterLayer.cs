using Compze.Tessaging.Typermedia;

namespace AccountManagement.UI;

static class UIAdapterLayer
{
   public static void RegisterTommandHandlers(ITypermediaTommandHandlerRegistrar registrar)
   {
      AccountUIAdapter.Register(registrar);
      AccountUIAdapter.ChangeEmail(registrar);
      AccountUIAdapter.ChangePassword(registrar);
      AccountUIAdapter.Login(registrar);
   }

   public static void RegisterTueryHandlers(ITueryHandlerRegistrar registrar) => AccountUIAdapter.GetById(registrar);
}