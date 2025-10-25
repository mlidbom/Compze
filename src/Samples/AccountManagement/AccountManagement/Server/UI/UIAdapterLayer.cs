using Compze.Abstractions.Tessaging.Hosting.MessageHandling.Registration.Public;

namespace AccountManagement.UI;

static class UIAdapterLayer
{
   public static void Register(TessageHandlerRegistrarWithDependencyInjectionSupport registrar)
   {
      AccountUIAdapter.GetById(registrar);
      AccountUIAdapter.Register(registrar);
      AccountUIAdapter.ChangeEmail(registrar);
      AccountUIAdapter.ChangePassword(registrar);
      AccountUIAdapter.Login(registrar);
   }
}