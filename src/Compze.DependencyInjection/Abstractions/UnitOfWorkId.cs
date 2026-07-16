using System.Transactions;

namespace Compze.DependencyInjection.Abstractions;

///<summary>The identity of one unit of work — one <see cref="IScope"/> paired with one ambient transaction. Value-equal exactly<br/>
/// when it identifies the same unit of work, so it can key logs, caches, and idempotence checks on "which unit of work did this".</summary>
///<remarks>Backed by the transaction's <see cref="TransactionInformation.LocalIdentifier"/>: the ambient transaction IS the<br/>
/// ambient unit-of-work tracker (see <c>src/Compze.DependencyInjection/dev_docs/unit-of-work-model.md</c>), so the transaction's<br/>
/// identity is the unit of work's identity.</remarks>
public sealed class UnitOfWorkId : IEquatable<UnitOfWorkId>
{
   readonly string _value;

   internal UnitOfWorkId(Transaction transaction) => _value = transaction.TransactionInformation.LocalIdentifier;

   public bool Equals(UnitOfWorkId? other) => other is not null && _value == other._value;
   public override bool Equals(object? obj) => obj is UnitOfWorkId other && Equals(other);
   public override int GetHashCode() => _value.GetHashCode();
   public override string ToString() => _value;

   public static bool operator ==(UnitOfWorkId? left, UnitOfWorkId? right) => Equals(left, right);
   public static bool operator !=(UnitOfWorkId? left, UnitOfWorkId? right) => !Equals(left, right);
}
