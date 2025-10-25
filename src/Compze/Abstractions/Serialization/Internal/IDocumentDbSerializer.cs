using System;

namespace Compze.Abstractions.Serialization.Internal;

interface IDocumentDbSerializer
{
   string Serialize(object instance);
   object Deserialize(Type teventType, string json);
}
