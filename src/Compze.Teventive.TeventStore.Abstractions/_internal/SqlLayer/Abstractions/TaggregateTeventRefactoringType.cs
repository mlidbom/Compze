namespace Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.Abstractions;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "All enum values represent valid refactoring operations. A 'None' value would not be meaningful in this domain context.")]
enum TaggregateTeventRefactoringType
{
   Replace = 1,
   InsertBefore = 2,
   InsertAfter = 3
}