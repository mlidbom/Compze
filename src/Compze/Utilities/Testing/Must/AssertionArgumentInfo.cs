namespace Compze.Utilities.Testing.Must;

public class ExpressionValue(string expression, object? value)
{
   public string Expression { get; } = expression;
   public object? Value { get; } = value;
}
