using Compze.Tessaging;

namespace Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;

public class TaggregateTeventRefactoringInformation(TessageId targetTevent, TaggregateTeventRefactoringType refactoringType)
{
    public static TaggregateTeventRefactoringInformation Replaces(TessageId teventId) => new(teventId, TaggregateTeventRefactoringType.Replace);
    public static TaggregateTeventRefactoringInformation InsertBefore(TessageId teventId) => new(teventId, TaggregateTeventRefactoringType.InsertBefore);
    public static TaggregateTeventRefactoringInformation InsertAfter(TessageId teventId) => new(teventId, TaggregateTeventRefactoringType.InsertAfter);

    public TessageId TargetTevent { get; } = targetTevent;
    public TaggregateTeventRefactoringType RefactoringType { get; } = refactoringType;
}
