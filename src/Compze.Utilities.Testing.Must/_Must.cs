using Compze.Functional;
using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.Must;

public interface IAssertionContext
{
   string Expression { get; }
   IAssertionContext<T> Cast<T>();
}

public interface IAssertionContext<out T> : IAssertionContext
{
   T Actual { get; }
}

public abstract class AssertionContext : IAssertionContext
{
   public const string RemoveLine = nameof(RemoveLine);

   public string Expression { get; }

   protected AssertionContext(object? actual, string expression)
   {
      ActualUntyped = actual;
      Expression = this.NormalizeExpressionIndentation(expression);
   }

   public object? ActualUntyped { get; }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.
   public IAssertionContext<T> Cast<T>() => new AssertionContext<T>((T)ActualUntyped, Expression);
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

   public static readonly string Separator = "-".Repeat(50).Join();
}

public class AssertionContext<T> : AssertionContext, IAssertionContext<T>
{
   public AssertionContext(T actual, string expression) : base(actual, expression) => Actual = actual;

   public T Actual { get; }
}
