namespace Compze.Tests.Infrastructure.Fluent;

public interface IAssertionBuilder<out T>
{
   T Subject { get; }
}
