// ReSharper disable ConvertClosureToMethodGroup
namespace Compze.Utilities.Testing.Must;
#pragma warning disable IDE0200
// ReSharper disable InconsistentNaming

public static class Must_Be___Null___strings
{
   public static IMust<string?> BeNullOrEmpty(this IMust<string?> must) =>
      must.SatisfyInternal(it => string.IsNullOrEmpty(it));

   public static IMust<string> NotBeNullOrEmpty(this IMust<string?> must) =>
      must.SatisfyInternal(it => !string.IsNullOrEmpty(it))!;

   public static IMust<string> NotBeNullOrWhiteSpace(this IMust<string?> must) =>
      must.SatisfyInternal(it => !string.IsNullOrWhiteSpace(it))!;
}
