using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Validation;

namespace Compze.Core.Tessaging.Teventive.Infrastructure.Validation;

public static class TessageInspector
{
   internal static void AssertValid(IReadOnlyList<Type> teventTypesToInspect) => teventTypesToInspect.ForEach(TessageTypeInspector.AssertValid);

   public static void AssertValidForSubscription<TTessage>() => TessageTypeInspector.AssertValidForSubscription(typeof(TTessage));

   public static void AssertValid<TTessage>() => TessageTypeInspector.AssertValid(typeof(TTessage));

   public static void AssertValidToSendRemote(ITessage tessage) => TessageValidator.AssertValidToSendRemote(tessage);

   public static void AssertValidToExecuteLocally(ITessage tessage) => TessageValidator.AssertValidToExecuteLocally(tessage);
}
