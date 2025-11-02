namespace Compze.Tests.Infrastructure.Fluent;
#pragma warning disable IDE0200
// ReSharper disable InconsistentNaming

public static class Must_Be___Null___strings
{
   public static Must<string?> BeNullOrEmpty(this Must<string?> must) =>
      must.Satisfy(it => string.IsNullOrEmpty(it));

   public static Must<string> NotBeNullOrEmpty(this Must<string?> must) =>
      must.Satisfy(it => !string.IsNullOrEmpty(it))!;

   public static Must<string> NotBeNullOrWhiteSpace(this Must<string?> must) =>
      must.Satisfy(it => !string.IsNullOrWhiteSpace(it))!;
}
