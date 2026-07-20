namespace Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;

class TaggregateTeventStorageInformation
{
   public int InsertedVersion { get; set; }
   public int EffectiveVersion { get; set; }

   public ReadOrder? ReadOrder { get; set; }

   public TaggregateTeventRefactoringInformation? RefactoringInformation { get; set; }
}