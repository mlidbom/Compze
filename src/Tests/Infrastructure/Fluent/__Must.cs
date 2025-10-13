namespace Compze.Tests.Infrastructure.Fluent;

public static class __Must
{
   public static AssertionBuilder<T> Must<T>(this T subject) => new(subject);
}
