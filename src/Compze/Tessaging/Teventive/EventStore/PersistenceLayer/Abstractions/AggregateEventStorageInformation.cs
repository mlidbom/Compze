namespace Compze.Tessaging.Teventive.EventStore.PersistenceLayer.Abstractions;

public class AggregateEventStorageInformation
{
   public int InsertedVersion { get; set; }
   public int EffectiveVersion { get; set; }

   public ReadOrder? ReadOrder { get; set; }

   public AggregateEventRefactoringInformation? RefactoringInformation { get; set; }
}