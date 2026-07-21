using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;

namespace Compze.Teventive.TeventStore.Abstractions.QueryModels.Generators;

///<summary>Reads query models: read models produced on demand from taggregate histories by the registered<br/>
/// <see cref="IQueryModelGenerator"/>s.</summary>
public interface IQueryModelReader
{
   ///<summary>The query model with key <paramref name="key"/>. Call it only when the model is known to exist —<br/>
   /// when absence is an expected outcome, use <see cref="TryGet{TValue}"/>.</summary>
   TValue Get<TValue>(EntityId key) where TValue : class;

   ///<summary>False when no query model with key <paramref name="key"/> and type <typeparamref name="TValue"/> can be produced;<br/>
   /// otherwise true with the model in <paramref name="value"/> — the door for keys that may legitimately not exist.</summary>
   bool TryGet<TValue>(EntityId key, [NotNullWhen(true)] out TValue? value) where TValue : class;
}
