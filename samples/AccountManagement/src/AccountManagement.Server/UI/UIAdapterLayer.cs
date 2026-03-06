using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Typermedia;

namespace AccountManagement.UI;

static class UIAdapterLayer
{
   public static void Register(TessageHandlerRegistrarWithDependencyInjectionSupport tessagingRegistrar, TypermediaHandlerRegistrarWithDependencyInjectionSupport typermediaRegistrar)
   {
      AccountUIAdapter.GetById(typermediaRegistrar);
      AccountUIAdapter.Register(typermediaRegistrar);
      AccountUIAdapter.ChangeEmail(tessagingRegistrar);
      AccountUIAdapter.ChangePassword(tessagingRegistrar);
      AccountUIAdapter.Login(typermediaRegistrar);
   }
}