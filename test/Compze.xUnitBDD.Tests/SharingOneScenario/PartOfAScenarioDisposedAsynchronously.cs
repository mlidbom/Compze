namespace Compze.xUnitBDD.Tests.SharingOneScenario;

/// <summary>
/// A <see cref="PartOfAScenario"/> that is only asynchronously disposable: it records in the run that it was built, and that<br/>
/// it was disposed asynchronously.
/// </summary>
sealed class PartOfAScenarioDisposedAsynchronously : IAsyncDisposable
{
   static int _partsBuilt;
   readonly string _name;
   readonly int _number = Interlocked.Increment(ref _partsBuilt);

   public PartOfAScenarioDisposedAsynchronously(string name)
   {
      _name = name;
      XunitRunOfAClass.Record($"{name} built");
   }

   public async ValueTask DisposeAsync()
   {
      await Task.Yield();
      XunitRunOfAClass.Record($"{_name} disposed asynchronously");
   }

   public override string ToString() => $"{_name} #{_number}";
}
