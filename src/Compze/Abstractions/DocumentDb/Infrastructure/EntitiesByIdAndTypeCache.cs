using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Core.DocumentDb.Infrastructure;

///<summary>Tracks entities by the combination of their ID and type</summary>
class EntitiesByIdAndTypeCache
{
    readonly Dictionary<IdAndType, object> _stringIdToInstance = new();
    readonly ILock _monitor = ILock.WithDefaultTimeout();

    public void Add<T>(object id, T value) => _monitor.Update(action: () =>
    {
        Argument.NotNull(value);
        var key = IdAndType.Create(id, value.GetType());
        AssertNotPresent(key);
        _stringIdToInstance[key] = value;
    });

    public void Remove(object id, Type documentType) => _monitor.Update(action: () =>
    {
        IdAndType.Create(id, documentType)
        .assert(_stringIdToInstance.Remove, it => $"No object with id: {it} of type: {documentType.FullName} is present");
    });

    public IList<KeyValuePair<string, object>> GetAll() =>
        _monitor.Read(() => _stringIdToInstance
                           .Select(pair => KeyValuePair.Create(pair.Key.Id, pair.Value))
                           .ToList());

    internal bool Contains(Type type, object id) => _monitor.Read(func: () => ContainsInternal(IdAndType.Create(id, type)));

    internal bool TryGet<T>(object id, out T value)
    {
        using(_monitor.TakeUpdateLock())
        {
            if(TryGet(IdAndType.Create(id, typeof(T)), out var found))
            {
                value = (T)found;
                return true;
            }

            value = default!;
            return false;
        }
    }

    void AssertNotPresent(IdAndType key)
    {
        if (ContainsInternal(key)) throw new Exception($"Instance of {key.DocumentType.FullName} with Id: {key.Id} is already present");
    }

    bool ContainsInternal(IdAndType key) => TryGet(key, out _);

    bool TryGet(IdAndType key, [NotNullWhen(returnValue: true)] out object? value) => _stringIdToInstance.TryGetValue(key, out value);

    readonly record struct IdAndType(string Id, Type DocumentType)
    {
        internal static IdAndType Create(object id, Type type) =>
            new(Result.ReturnNotNull(id).ToStringNotNull().ToUpperInvariant().TrimEnd(trimChar: ' '), type);
    }
}
