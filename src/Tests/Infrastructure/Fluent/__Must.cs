namespace Compze.Tests.Infrastructure.Fluent;

public static class __Must
{
   public static IAssertionBuilder<T> Must<T>(this T subject) => IAssertionBuilder.Create(subject);
}
