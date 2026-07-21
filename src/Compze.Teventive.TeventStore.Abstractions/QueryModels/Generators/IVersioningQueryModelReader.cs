using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;

namespace Compze.Teventive.TeventStore.Abstractions.QueryModels.Generators;

///<summary>An <see cref="IQueryModelReader"/> that can additionally produce a query model as of a specific taggregate version.</summary>
public interface IVersioningQueryModelReader : IQueryModelReader
{
   ///<summary>The query model with key <paramref name="key"/> as of taggregate version <paramref name="version"/>. Call it only<br/>
   /// when the model is known to exist — when absence is an expected outcome, use <see cref="TryGetVersion{TValue}"/>.</summary>
   TValue GetVersion<TValue>(EntityId key, int version) where TValue : class;

   ///<summary>False when no query model with key <paramref name="key"/> and type <typeparamref name="TValue"/> can be produced as of<br/>
   /// taggregate version <paramref name="version"/>; otherwise true with the model in <paramref name="value"/>.</summary>
   bool TryGetVersion<TValue>(EntityId key, int version, [NotNullWhen(true)] out TValue? value) where TValue : class;
}
