namespace Compze.Must;

/// <summary>The non-generic context for a <see cref="MustExtensions.Must{T}(T, string)"/> assertion chain: the captured expression text plus a way to narrow to a typed context.</summary>
public interface IAssertionContext
{
   /// <summary>The source text of the expression under assertion, captured automatically from the call site by the compiler.</summary>
   string Expression { get; }
   /// <summary>Narrows this context to an assertion over a value of type <typeparamref name="T"/>.</summary>
   IAssertionContext<T> Cast<T>();
}

/// <summary>The context for a <see cref="MustExtensions.Must{T}(T, string)"/> assertion chain over a value of type <typeparamref name="T"/>. Assertions are extension methods on this interface.</summary>
public interface IAssertionContext<out T> : IAssertionContext
{
   /// <summary>The value under assertion.</summary>
   T Actual { get; }
}

abstract class AssertionContext : IAssertionContext
{
   public const string RemoveLine = nameof(RemoveLine);

   public string Expression { get; }

   protected AssertionContext(object? actual, string expression)
   {
      ActualUntyped = actual;
      Expression = this.NormalizeExpressionIndentation(expression);
   }

   object? ActualUntyped { get; }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.
   public IAssertionContext<T> Cast<T>() => new AssertionContext<T>((T)ActualUntyped, Expression);
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

   public static readonly string Separator = "-".Repeat(50).Join();
}

class AssertionContext<T>(T actual, string expression) : AssertionContext(actual, expression), IAssertionContext<T>
{
   public T Actual { get; } = actual;
}
