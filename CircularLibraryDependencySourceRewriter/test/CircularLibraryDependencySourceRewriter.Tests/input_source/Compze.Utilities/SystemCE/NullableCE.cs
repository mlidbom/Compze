using System;

namespace Compze.Utilities.SystemCE;

public static class NullableCE
{
   //Urgent: Move to contracts
   public static T NotNull<T>(this T? @this) where T : class => @this ?? throw new ArgumentNullException(nameof(@this));
   public static T NotNull<T>(this T? @this, Func<string> tessageFactory) where T : class => @this ?? throw new ArgumentNullException(tessageFactory());

}