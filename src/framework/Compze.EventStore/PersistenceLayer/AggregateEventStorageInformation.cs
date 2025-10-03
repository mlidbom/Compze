namespace Compze.EventStore.PersistenceLayer;

public class AggregateEventStorageInformation
{
   public int InsertedVersion { get; set; }
   public int EffectiveVersion { get; set; }

   public ReadOrder? ReadOrder { get; set; }

   public AggregateEventRefactoringInformation? RefactoringInformation { get; set; }
}