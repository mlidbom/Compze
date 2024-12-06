using System;

namespace Compze.Persistence.EventStore.PersistenceLayer;

class AggregateEventRefactoringInformation(Guid targetEvent, AggregateEventRefactoringType refactoringType)
{
   internal static AggregateEventRefactoringInformation Replaces(Guid eventId) => new(eventId, AggregateEventRefactoringType.Replace);
   internal static AggregateEventRefactoringInformation InsertBefore(Guid eventId) => new(eventId, AggregateEventRefactoringType.InsertBefore);
   internal static AggregateEventRefactoringInformation InsertAfter(Guid eventId) => new(eventId, AggregateEventRefactoringType.InsertAfter);

   public Guid TargetEvent { get; } = targetEvent;
   public AggregateEventRefactoringType RefactoringType { get; } = refactoringType;
}