using System;

namespace Compze.Sql.Common.TeventStore.Abstractions;

public class AggregateTeventRefactoringInformation(Guid targetTevent, AggregateTeventRefactoringType refactoringType)
{
    public static AggregateTeventRefactoringInformation Replaces(Guid teventId) => new(teventId, AggregateTeventRefactoringType.Replace);
    public static AggregateTeventRefactoringInformation InsertBefore(Guid teventId) => new(teventId, AggregateTeventRefactoringType.InsertBefore);
    public static AggregateTeventRefactoringInformation InsertAfter(Guid teventId) => new(teventId, AggregateTeventRefactoringType.InsertAfter);

    public Guid TargetTevent { get; } = targetTevent;
    public AggregateTeventRefactoringType RefactoringType { get; } = refactoringType;
}
