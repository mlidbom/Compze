using System;

namespace Compze.Persistence.EventStore.PersistenceLayer;

class AggregateEventRefactoringInformation
{
   internal static AggregateEventRefactoringInformation Replaces(Guid eventId) => new(eventId, AggregateEventRefactoringType.Replace);
   internal static AggregateEventRefactoringInformation InsertBefore(Guid eventId) => new(eventId, AggregateEventRefactoringType.InsertBefore);
   internal static AggregateEventRefactoringInformation InsertAfter(Guid eventId) => new(eventId, AggregateEventRefactoringType.InsertAfter);

   public AggregateEventRefactoringInformation(Guid targetEvent, AggregateEventRefactoringType refactoringType)
   {
      TargetEvent = targetEvent;
      RefactoringType = refactoringType;
   }

   public Guid TargetEvent { get; }
   public AggregateEventRefactoringType RefactoringType { get; }
}