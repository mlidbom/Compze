// ReSharper disable ConvertClosureToMethodGroup
namespace Compze.Utilities.Testing.Must;
#pragma warning disable IDE0200
// ReSharper disable InconsistentNaming

public static class Must_Be___Null___strings
{
   public static IAssertionContext<string?> BeNullOrEmpty(this IAssertionContext<string?> context) =>
      context.SatisfyInternal(it => string.IsNullOrEmpty(it));

   public static IAssertionContext<string> NotBeNullOrEmpty(this IAssertionContext<string?> context) =>
      context.SatisfyInternal(it => !string.IsNullOrEmpty(it))!;

   public static IAssertionContext<string> NotBeNullOrWhiteSpace(this IAssertionContext<string?> context) =>
      context.SatisfyInternal(it => !string.IsNullOrWhiteSpace(it))!;
}
