using System.Reflection;

namespace Compze.xUnitBDD._private;

/// <summary>
/// Where a part of a <see cref="ScenarioOfTheClass"/> is built, by which the scenario keeps it: the file and line of the call<br/>
/// to <see cref="ISharesOneScenario.Once{TPart}"/>, and the method that builds the part, the lambda that call is given.
/// </summary>
/// <remarks>The method that builds the part tells two calls on one line apart, which the file and line alone can't.</remarks>
sealed class PlaceInTheCode(string file, int line, MethodInfo buildsThePart) : IEquatable<PlaceInTheCode>
{
   readonly string _file = file;
   readonly int _line = line;
   readonly MethodInfo _buildsThePart = buildsThePart;

   public bool Equals(PlaceInTheCode? other) =>
      other is not null && _line == other._line && _file == other._file && _buildsThePart.Equals(other._buildsThePart);

   public override bool Equals(object? obj) => obj is PlaceInTheCode other && Equals(other);

   public override int GetHashCode() => HashCode.Combine(_file, _line, _buildsThePart);
}
