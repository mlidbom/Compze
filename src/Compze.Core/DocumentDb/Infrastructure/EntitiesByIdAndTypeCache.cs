using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Contracts;
using Compze.Utilities.SystemCE;
using Compze.Threading.ResourceAccess;
using static Compze.Contracts.Contract;

namespace Compze.Core.DocumentDb.Infrastructure;

///<summary>Tracks entities by the combination of their ID and type</summary>
public class EntitiesByIdAndTypeCache
{
   readonly IThreadShared<Dictionary<IdAndType, object>> _data = IThreadShared.New(new Dictionary<IdAndType, object>());

   public void Add<T>(object id, T value) => _data.Locked(data =>
   {
      Argument.Assert(value is not null);
      var key = IdAndType.Create(id, value.GetType());
      AssertNotPresent(key);
      data[key] = value;
   });

   internal void Remove(object id, Type documentType) => _data.Locked(data =>
   {
      IdAndType.Create(id, documentType)
               ._assert(data.Remove, it => $"No object with id: {it} of type: {documentType.FullName} is present");
   });

   internal IList<KeyValuePair<string, object>> GetAll() =>
      _data.Locked(data => data
                        .Select(pair => KeyValuePair.Create(pair.Key.Id, pair.Value))
                        .ToList());

   internal bool Contains(Type type, object id) => ContainsInternal(IdAndType.Create(id, type));

   public bool TryGet<T>(object id, out T value) =>
      _data.LockedOut((data, out value) =>
                    {
                       if(data.TryGetValue(IdAndType.Create(id, typeof(T)), out var found))
                       {
                          value = (T)found;
                          return true;
                       }

                       value = default!;
                       return false;
                    },
                    out value);

   void AssertNotPresent(IdAndType key)
   {
      if(ContainsInternal(key)) throw new Exception($"Instance of {key.DocumentType.FullName} with Id: {key.Id} is already present");
   }

   bool ContainsInternal(IdAndType key) => _data.Locked(it => it.TryGetValue(key, out _));

   internal readonly record struct IdAndType(string Id, Type DocumentType)
   {
      internal static IdAndType Create(object id, Type type) =>
         new(id._assert().NotNull().ToStringNotNull().ToUpperInvariant().TrimEnd(trimChar: ' '), type);
   }
}
