namespace Compze.Sql.Common.TeventStore.Abstractions;

public class AggregateTeventStorageInformation
{
   public int InsertedVersion { get; set; }
   public int EffectiveVersion { get; set; }

   public ReadOrder? ReadOrder { get; set; }

   public AggregateTeventRefactoringInformation? RefactoringInformation { get; set; }
}