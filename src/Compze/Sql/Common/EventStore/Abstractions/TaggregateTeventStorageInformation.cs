namespace Compze.Sql.Common.TeventStore.Abstractions;

public class TaggregateTeventStorageInformation
{
   public int InsertedVersion { get; set; }
   public int EffectiveVersion { get; set; }

   public ReadOrder? ReadOrder { get; set; }

   public TaggregateTeventRefactoringInformation? RefactoringInformation { get; set; }
}