namespace Compze.Utilities.Testing.Must;

public class AssertionArgumentInfo(string name, string expression, object? value)
{
   public string Name { get; } = name;
   public string Expression { get; } = expression;
   public object? Value { get; } = value;
}
