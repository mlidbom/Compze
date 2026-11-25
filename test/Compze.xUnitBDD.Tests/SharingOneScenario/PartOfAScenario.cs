namespace Compze.xUnitBDD.Tests.SharingOneScenario;

/// <summary>
/// A part of the scenario of a class that a specification runs with <see cref="XunitRunOfAClass"/>: it records in the run<br/>
/// that it was built, and that it was disposed, under its name. Written into what a fact records, it is its name and a<br/>
/// number of its own, which tells it from another part of that name, and the part it was built on, if any.
/// </summary>
sealed class PartOfAScenario : IDisposable
{
   static int _partsBuilt;
   readonly string _name;
   readonly int _number = Interlocked.Increment(ref _partsBuilt);
   readonly PartOfAScenario? _builtOn;

   public PartOfAScenario(string name, PartOfAScenario? builtOn = null)
   {
      _name = name;
      _builtOn = builtOn;
      XunitRunOfAClass.Record($"{name} built");
   }

   public void Dispose() => XunitRunOfAClass.Record($"{_name} disposed");

   public override string ToString() => _builtOn is null ? $"{_name} #{_number}" : $"{_name} #{_number}, built on {_builtOn}";
}
