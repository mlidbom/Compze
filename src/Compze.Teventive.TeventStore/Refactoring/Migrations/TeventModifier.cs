using Compze.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Contracts;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents.Public;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations;

//Yes this class has quite a bit of code that looks overly complex. Like it could be simplified a lot.
//What you are seeing is likely optimizations. Please do not change this class for reasons of readability unless you do thorough performance testing and verify that no performance is lost.
//The performance of this class is extremely important since it is called at least once for every tevent that is loaded from the tevent store when you have any migrations activated. It is called A LOT.
//This is one of those central classes for which optimization is actually vitally important.
//Each of the optimizations were done with the help of a profiler and running benchmarks on the tested performance improvements time and time again.
//Performance: Consider whether using the new stackalloc and Range types might allow us to improve performance of migrations.
public class TeventModifier(Action<IReadOnlyList<TeventModifier.RefactoredTevent>> teventsAddedCallback) : ITeventModifier
{
   public class RefactoredTevent(ITaggregateTevent<ITaggregateTevent> newWrappedTevent, TaggregateTeventStorageInformation storageInformation)
   {
      internal ITaggregateTevent<ITaggregateTevent> NewWrappedTevent { get; private set; } = newWrappedTevent;
      internal TaggregateTeventStorageInformation StorageInformation { get; private set; } = storageInformation;
   }

   readonly Action<IReadOnlyList<RefactoredTevent>> _teventsAddedCallback = teventsAddedCallback;
   internal LinkedList<ITaggregateTevent<ITaggregateTevent>>? WrappedTevents;
   RefactoredTevent[]? _replacementTevents;
   RefactoredTevent[]? _insertedTevents;

   ITaggregateTevent<ITaggregateTevent>? _inspectedWrappedTevent;

   LinkedListNode<ITaggregateTevent<ITaggregateTevent>>? _currentNode;
   ITaggregateTevent<ITaggregateTevent>? _lastWrappedTeventInActualStream;

   LinkedListNode<ITaggregateTevent<ITaggregateTevent>> CurrentNode
   {
      get
      {
         if (_currentNode == null)
         {
            WrappedTevents = [];
            _currentNode = WrappedTevents.AddFirst(_inspectedWrappedTevent!);
         }
         return _currentNode;
      }
      set
      {
         _currentNode = value;
         _inspectedWrappedTevent = _currentNode.Value;
      }
   }

   void AssertNoPriorModificationsHaveBeenMade()
   {
      if(_replacementTevents != null || _insertedTevents != null)
      {
         throw new Exception("You can only modify the current tevent once.");
      }

   }

   public void Replace(params ITaggregateTevent<ITaggregateTevent>[] wrappedTevents)
   {
      AssertNoPriorModificationsHaveBeenMade();
      if(_inspectedWrappedTevent?.Tevent is EndOfTaggregateHistoryTeventPlaceHolder)
      {
         throw new Exception("You cannot call replace on the tevent that signifies the end of the stream");

      }

      _replacementTevents = wrappedTevents.Select(wrappedTevent => new RefactoredTevent(wrappedTevent, new TaggregateTeventStorageInformation())).ToArray();

      _replacementTevents.ForEach(
         (e, index) =>
         {
#pragma warning disable CS0618 // Type or member is obsolete
             ((IMutableTaggregateTevent)e.NewWrappedTevent.Tevent).SetTaggregateVersionInternal(_inspectedWrappedTevent!.Tevent.TaggregateVersion + index);

             e.StorageInformation.RefactoringInformation = TaggregateTeventRefactoringInformation.Replaces(_inspectedWrappedTevent.Tevent.Id);
            e.StorageInformation.EffectiveVersion = _inspectedWrappedTevent.Tevent.TaggregateVersion + index;

            ((IMutableTaggregateTevent)e.NewWrappedTevent.Tevent).SetTaggregateIdInternal(_inspectedWrappedTevent.Tevent.TaggregateId);
            ((IMutableTaggregateTevent)e.NewWrappedTevent.Tevent).SetUtcTimeStampInternal(_inspectedWrappedTevent.Tevent.UtcTimeStamp);
         });
#pragma warning restore CS0618 // Type or member is obsolete

        CurrentNode = CurrentNode.Replace(wrappedTevents);
      _teventsAddedCallback.Invoke(_replacementTevents);
   }

   public void Reset(ITaggregateTevent<ITaggregateTevent> wrappedTevent)
   {
      if(wrappedTevent.Tevent is EndOfTaggregateHistoryTeventPlaceHolder && _inspectedWrappedTevent?.Tevent is not EndOfTaggregateHistoryTeventPlaceHolder)
      {
         _lastWrappedTeventInActualStream = _inspectedWrappedTevent;
      }
      _inspectedWrappedTevent = wrappedTevent;
      WrappedTevents = null;
      _currentNode = null;
      _insertedTevents = null;
      _replacementTevents = null;
   }

   public void MoveTo(LinkedListNode<ITaggregateTevent<ITaggregateTevent>> current)
   {
      if (current.Value.Tevent is EndOfTaggregateHistoryTeventPlaceHolder && _inspectedWrappedTevent?.Tevent is not EndOfTaggregateHistoryTeventPlaceHolder)
      {
         _lastWrappedTeventInActualStream = _inspectedWrappedTevent;
      }
      CurrentNode = current;
      _insertedTevents = null;
      _replacementTevents = null;
   }

   public void InsertBefore(params ITaggregateTevent<ITaggregateTevent>[] insert)
   {
      AssertNoPriorModificationsHaveBeenMade();

      _insertedTevents = insert.Select(wrappedTevent => new RefactoredTevent(wrappedTevent, new TaggregateTeventStorageInformation())).ToArray();

#pragma warning disable CS0618 // Type or member is obsolete
        if (_inspectedWrappedTevent!.Tevent is EndOfTaggregateHistoryTeventPlaceHolder)
      {
         _insertedTevents.ForEach(
            (e, index) =>
            {
               ((IMutableTaggregateTevent)e.NewWrappedTevent.Tevent).SetTaggregateVersionInternal(_inspectedWrappedTevent.Tevent.TaggregateVersion + index);

               e.StorageInformation.RefactoringInformation = TaggregateTeventRefactoringInformation.InsertAfter(_lastWrappedTeventInActualStream!.Tevent.Id);
               e.StorageInformation.EffectiveVersion = _inspectedWrappedTevent.Tevent.TaggregateVersion + index;

               ((IMutableTaggregateTevent)e.NewWrappedTevent.Tevent).SetTaggregateIdInternal(_inspectedWrappedTevent.Tevent.TaggregateId);
               ((IMutableTaggregateTevent)e.NewWrappedTevent.Tevent).SetUtcTimeStampInternal(_lastWrappedTeventInActualStream.Tevent.UtcTimeStamp);
            });
      }
      else
      {
         _insertedTevents.ForEach(
            (e, index) =>
            {
               ((IMutableTaggregateTevent)e.NewWrappedTevent.Tevent).SetTaggregateVersionInternal(_inspectedWrappedTevent!.Tevent.TaggregateVersion + index);

               e.StorageInformation.RefactoringInformation = TaggregateTeventRefactoringInformation.InsertBefore(_inspectedWrappedTevent.Tevent.Id);
               e.StorageInformation.EffectiveVersion = _inspectedWrappedTevent.Tevent.TaggregateVersion + index;

               ((IMutableTaggregateTevent)e.NewWrappedTevent.Tevent).SetTaggregateIdInternal(_inspectedWrappedTevent.Tevent.TaggregateId);
               ((IMutableTaggregateTevent)e.NewWrappedTevent.Tevent).SetUtcTimeStampInternal(_inspectedWrappedTevent.Tevent.UtcTimeStamp);
            });
      }
      CurrentNode.ValuesFrom().ForEach((wrappedTevent, _) => ((IMutableTaggregateTevent)wrappedTevent.Tevent).SetTaggregateVersionInternal(wrappedTevent.Tevent.TaggregateVersion + _insertedTevents.Length));

      CurrentNode.AddBefore(insert);
      _teventsAddedCallback.Invoke(_insertedTevents);
#pragma warning restore CS0618 // Type or member is obsolete
    }

#pragma warning disable CA1819 // Array property needed for migration tevent history
    public ITaggregateTevent<ITaggregateTevent>[] MutatedHistory => WrappedTevents?.ToArray() ?? [_inspectedWrappedTevent._assert().NotNull()];
#pragma warning restore CA1819
}
