namespace Compze.Tests.Infrastructure.Fluent;

public interface IAssertionBuilder
{
   public static IAssertionBuilder<T> Create<T>(T instance) => new AssertionBuilder<T>(instance);

   public class AssertionBuilder<T>(T subject) : IAssertionBuilder<T>
   {
      public T Subject { get; } = subject;
   }
}

public interface IAssertionBuilder<out T>
{
   T Subject { get; }
}
