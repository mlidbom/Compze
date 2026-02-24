using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Core.DocumentDb.Infrastructure;

///<summary>Tracks entities by the combination of their ID and type</summary>
public class EntitiesByIdAndTypeCache
{
   readonly IThreadShared<Dictionary<IdAndType, object>> _data = IThreadShared.WithDefaultTimeouts(new Dictionary<IdAndType, object>());

   public void Add<T>(object id, T value) => _data.Update(data =>
   {
      Argument.NotNull(value);
      var key = IdAndType.Create(id, value.GetType());
      AssertNotPresent(key);
      data[key] = value;
   });

   public void Remove(object id, Type documentType) => _data.Update(data =>
   {
      IdAndType.Create(id, documentType)
               ._assert(data.Remove, it => $"No object with id: {it} of type: {documentType.FullName} is present");
   });

   public IList<KeyValuePair<string, object>> GetAll() =>
      _data.Read(data => data
                        .Select(pair => KeyValuePair.Create(pair.Key.Id, pair.Value))
                        .ToList());

   public bool Contains(Type type, object id) => ContainsInternal(IdAndType.Create(id, type));

   public bool TryGet<T>(object id, out T value) =>
      _data.ReadOut((Dictionary<IdAndType, object> data, out T value) =>
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

   bool ContainsInternal(IdAndType key) => _data.Read(it => it.TryGetValue(key, out _));

   public readonly record struct IdAndType(string Id, Type DocumentType)
   {
      public static IdAndType Create(object id, Type type) =>
         new(ReturnValue.ReturnNotNull(id).ToStringNotNull().ToUpperInvariant().TrimEnd(trimChar: ' '), type);
   }
}
