using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Compze.Contracts;
using Compze.Functional;
using Compze.SystemCE.CollectionsCE.GenericCE;
using Compze.SystemCE.LinqCE;
using Compze.SystemCE.ThreadingCE.ResourceAccess;
using static Compze.Contracts.Assert;

namespace Compze.Persistence.DocumentDb;

///<summary>Tracks entities by converting the supplied id into a string and maintaining a dictionary from that string id to a list of instances to enable polymorphism</summary>
class PolymorphicEntityIdMap : IEnumerable<KeyValuePair<string, object>>
{
   readonly Dictionary<string, List<object>> _stringIdToInstance = new(StringComparer.InvariantCultureIgnoreCase);
   readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();

   internal bool Contains(Type type, object id) => _monitor.Read(() => ContainsInternal(type, id));

   bool ContainsInternal(Type type, object id) => TryGet(type, id, out _);

   internal bool TryGet<T>(object id, [MaybeNullWhen(false)] out T value)
   {
      using(_monitor.TakeUpdateLock())
      {
         if(TryGet(typeof(T), id, out var found))
         {
            value = (T)found;
            return true;
         }

         value = default!;
         return false;
      }
   }

   bool TryGet(Type typeOfValue, object id, [NotNullWhen(true)] out object? value)
   {
      var idString = GetIdStringRepresentation(id);
      value = null;

      if(!_stringIdToInstance.TryGetValue(idString, out var matchesId)) return false;

      var found = matchesId.Where(typeOfValue.IsInstanceOfType).ToList();
      if(!found.Any()) return false;

      value = found.Single();
      return true;
   }

   static string GetIdStringRepresentation(object id) => Result.ReturnNotNull(id).ToStringNotNull().ToUpperInvariant().TrimEnd(' ');

   public virtual void Add<T>(object id, T value) => _monitor.Update(() =>
   {
      Argument.NotNull(value);

      var idString = GetIdStringRepresentation(id);
      if(ContainsInternal(value.GetType(), idString))
      {
         throw new AttemptToSaveAlreadyPersistedValueException(id, value);
      }

      _stringIdToInstance.GetOrAddDefault(idString).Add(value);
   });

   public void Remove(object id, Type documentType) => _monitor.Update(() =>
   {
      var idString = GetIdStringRepresentation(id);
      var removed = _stringIdToInstance.GetOrAddDefault(idString).RemoveWhere(documentType.IsInstanceOfType);
      if(removed.None())
      {
         throw new NoSuchDocumentException(id, documentType);
      }

      if(removed.Count > 1)
      {
         throw new Exception("It really should be impossible to hit multiple documents with one Id, but apparently you just did it!");
      }
   });

   public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _monitor.Read(
      () => _stringIdToInstance.SelectMany(m => m.Value.Select(inner => new KeyValuePair<string, object>(m.Key, inner)))
               .ToList() //ToList is to make it thread safe...
               .GetEnumerator());

   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
