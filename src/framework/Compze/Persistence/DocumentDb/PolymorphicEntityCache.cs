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
        if(ContainsInternal(key)) throw new AttemptToSaveAlreadyPersistedValueException(id, value);

        _stringIdToInstance[key] = value;
    });

    public void Remove(object id, Type documentType) => _monitor.Update(action: () =>
    {
        var idString = GetIdStringRepresentation(id);
        var key = new StringTypeKey(idString, documentType);
        var removed = _stringIdToInstance.Remove(key);
        if(!removed) throw new NoSuchDocumentException(id, documentType);
    });

    public IList<KeyValuePair<string, object>> GetAll() =>
        _monitor.Read(() => _stringIdToInstance
                           .Select(pair => new KeyValuePair<string, object>(pair.Key.Id, pair.Value))
                           .ToList());

    internal bool Contains(Type type, object id) => _monitor.Read(func: () => ContainsInternal(StringTypeKey.Create(id, type)));

    internal bool TryGet<T>(object id, [MaybeNullWhen(returnValue: false)] out T value)
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

    static string GetIdStringRepresentation(object id) => Result.ReturnNotNull(id).ToStringNotNull().ToUpperInvariant().TrimEnd(trimChar: ' ');

    readonly struct StringTypeKey : IEquatable<StringTypeKey>
    {
        public StringTypeKey(string id, Type documentType)
        {
            Id = id;
            DocumentType = documentType;
        }

        internal static StringTypeKey Create(object id, Type type) => new(GetIdStringRepresentation(id), type);

        public readonly string Id;
        public readonly Type DocumentType;

        public bool Equals(StringTypeKey other) => Id == other.Id && DocumentType == other.DocumentType;
        public override bool Equals(object? obj) => obj is StringTypeKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, DocumentType);
    }
}
