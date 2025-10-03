using Compze.Functional;
using Compze.SystemCE.ThreadingCE.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static Compze.Contracts.Assert;

namespace Compze.Persistence.DocumentDb;

///<summary>Tracks entities by converting the supplied id into string and indexing objects by that string combined with the supplied type</summary>
class EntityIdMap
{
    readonly Dictionary<StringTypeKey, object> _stringIdToInstance = new();
    readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();

    public void Add<T>(object id, T value) => _monitor.Update(action: () =>
    {
        Argument.NotNull(value);

        var key = StringTypeKey.Create(id, value.GetType());
        if(ContainsInternal(key)) throw new Exception($"Instance of {value.GetType().FullName} with Id: {key} is already present");

        _stringIdToInstance[key] = value;
    });

    public void Remove(object id, Type documentType) => _monitor.Update(action: () =>
    {
        if(!StringTypeKey.Create(id, documentType)._(_stringIdToInstance.Remove))
            throw new Exception($"No object with id: {id} of type: {documentType.FullName} is present");
    });

    public IList<KeyValuePair<string, object>> GetAll() =>
        _monitor.Read(() => _stringIdToInstance
                           .Select(pair => KeyValuePair.Create(pair.Key.Id, pair.Value))
                           .ToList());

    internal bool Contains(Type type, object id) => _monitor.Read(func: () => ContainsInternal(StringTypeKey.Create(id, type)));

    internal bool TryGet<T>(object id, out T value)
    {
        using(_monitor.TakeUpdateLock())
        {
            if(TryGet(StringTypeKey.Create(id, typeof(T)), out var found))
            {
                value = (T)found;
                return true;
            }

            value = default!;
            return false;
        }
    }

    bool ContainsInternal(StringTypeKey key) => TryGet(key, out _);

    bool TryGet(StringTypeKey key, [NotNullWhen(returnValue: true)] out object? value) => _stringIdToInstance.TryGetValue(key, out value);

    readonly record struct StringTypeKey(string Id, Type DocumentType)
    {
        internal static StringTypeKey Create(object id, Type type) =>
            new(Result.ReturnNotNull(id).ToStringNotNull().ToUpperInvariant().TrimEnd(trimChar: ' '), type);
    }
}
