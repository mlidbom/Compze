using Compze.Abstractions;

namespace Compze.Tessaging.TessageTypes;

public static class StrictlyLocal
{
   public static class Tueries
   {
#pragma warning disable CA1724 //Class name conflicts with namespace name.
      public abstract class StrictlyLocalTuery<TTuery, TResult> : IStrictlyLocalTuery<TTuery, TResult> where TTuery : StrictlyLocalTuery<TTuery, TResult>;
#pragma warning restore CA1724 //

      public sealed class EntityLink<TResult>(EntityId entityId) : StrictlyLocal.Tueries.StrictlyLocalTuery<EntityLink<TResult>, TResult>
         where TResult : IEntity<Guid>
      {
         public EntityId EntityId { get; private set; } = entityId;
      }
   }

   public static class Tommands
   {
      public abstract class StrictlyLocalTommand : IStrictlyLocalTommand;

      public abstract class StrictlyLocalTommand<TResult> : StrictlyLocalTommand, IStrictlyLocalTommand<TResult>;
   }
}
