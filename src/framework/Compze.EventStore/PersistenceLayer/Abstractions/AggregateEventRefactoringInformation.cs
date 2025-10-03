using System;

namespace Compze.EventStore.PersistenceLayer.Abstractions;

public class AggregateEventRefactoringInformation(Guid targetEvent, AggregateEventRefactoringType refactoringType)
{
    public static AggregateEventRefactoringInformation Replaces(Guid eventId) => new(eventId, AggregateEventRefactoringType.Replace);
    public static AggregateEventRefactoringInformation InsertBefore(Guid eventId) => new(eventId, AggregateEventRefactoringType.InsertBefore);
    public static AggregateEventRefactoringInformation InsertAfter(Guid eventId) => new(eventId, AggregateEventRefactoringType.InsertAfter);

    public Guid TargetEvent { get; } = targetEvent;
    public AggregateEventRefactoringType RefactoringType { get; } = refactoringType;
}
