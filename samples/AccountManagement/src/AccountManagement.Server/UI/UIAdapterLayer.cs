using Compze.Tessaging.Typermedia.HandlerRegistration;

namespace AccountManagement.UI;

static class UIAdapterLayer
{
   public static void Register(TypermediaHandlerRegistrarWithDependencyInjectionSupport typermediaRegistrar)
   {
      AccountUIAdapter.GetById(typermediaRegistrar);
      AccountUIAdapter.Register(typermediaRegistrar);
      AccountUIAdapter.ChangeEmail(typermediaRegistrar);
      AccountUIAdapter.ChangePassword(typermediaRegistrar);
      AccountUIAdapter.Login(typermediaRegistrar);
   }
}