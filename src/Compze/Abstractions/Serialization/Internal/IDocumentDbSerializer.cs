using System;

namespace Compze.Core.Serialization.Internal;

interface IDocumentDbSerializer
{
   string Serialize(object instance);
   object Deserialize(Type teventType, string json);
}
