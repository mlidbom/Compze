namespace Compze.xUnitBDD.Tests.SharingOneScenario;

/// <summary>A part of the scenario of a class that a specification runs, whose disposal throws, saying that it could not be disposed.</summary>
sealed class PartOfAScenarioThatFailsToDispose(string name) : IDisposable
{
   readonly string _name = name;

   public void Dispose() => throw new InvalidOperationException($"{_name} could not be disposed.");
}
