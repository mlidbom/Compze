using Compze.DocumentDb.Public;
using Compze.Contracts;

namespace Compze.DocumentDb.Private;

public partial class DocumentDbSession
{
   public class DocumentItem
   {
      readonly IDocumentDb _backingStore;
      readonly Dictionary<Type, Dictionary<string, string>> _persistentValues;
      DocumentKey Key { get; set; }

      internal DocumentItem(DocumentKey key, IDocumentDb backingStore, Dictionary<Type, Dictionary<string, string>> persistentValues)
      {
         _backingStore = backingStore;
         _persistentValues = persistentValues;
         Key = key;
      }

      object? Document { get; set; }
      internal bool IsDeleted { get; private set; }
      bool IsInBackingStore { get; set; }

      bool ScheduledForAdding => !IsInBackingStore && !IsDeleted && Document != null;
      bool ScheduledForRemoval => IsInBackingStore && IsDeleted;
      bool ScheduledForUpdate => IsInBackingStore && !IsDeleted;

      internal void Delete() => IsDeleted = true;

      internal void Save(object document)
      {
         Document = document._assert().NotNull();
         IsDeleted = false;
      }

      internal void DocumentLoadedFromBackingStore(object document)
      {
         Document = Document = document._assert().NotNull();
         IsInBackingStore = true;
      }

      readonly ReentrancyGuard _reentrancyGuard = new();

      internal void CommitChangesToBackingStore() => _reentrancyGuard.ExecuteIfNotReEntering(() =>
      {
         if(ScheduledForAdding)
         {
            IsInBackingStore = true;
            _backingStore.Add(Key.Id, Document._assert().NotNull(), _persistentValues);
         } else if(ScheduledForRemoval)
         {
            var docType = Document!.GetType();
            Document = null;
            IsInBackingStore = false;
            _backingStore.Remove(Key.Id, docType);
         } else if(ScheduledForUpdate)
         {
            _backingStore.Update([new KeyValuePair<string, object>(Key.Id, Document!)], _persistentValues);
         }
      });
   }
}
