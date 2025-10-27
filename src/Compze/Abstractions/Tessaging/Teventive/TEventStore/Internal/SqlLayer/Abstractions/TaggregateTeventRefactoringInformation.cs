using System;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

public class TaggregateTeventRefactoringInformation(Guid targetTevent, TaggregateTeventRefactoringType refactoringType)
{
    public static TaggregateTeventRefactoringInformation Replaces(Guid teventId) => new(teventId, TaggregateTeventRefactoringType.Replace);
    public static TaggregateTeventRefactoringInformation InsertBefore(Guid teventId) => new(teventId, TaggregateTeventRefactoringType.InsertBefore);
    public static TaggregateTeventRefactoringInformation InsertAfter(Guid teventId) => new(teventId, TaggregateTeventRefactoringType.InsertAfter);

    public Guid TargetTevent { get; } = targetTevent;
    public TaggregateTeventRefactoringType RefactoringType { get; } = refactoringType;
}
