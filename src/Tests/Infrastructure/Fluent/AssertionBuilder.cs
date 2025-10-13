namespace Compze.Tests.Infrastructure.Fluent;

public class AssertionBuilder<T>(T subject) : IAssertionBuilder<T>
{
   public T Subject { get; } = subject;
}
