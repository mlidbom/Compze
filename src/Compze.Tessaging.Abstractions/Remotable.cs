using Compze.Abstractions.Public;

// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable once PropertyCanBeMadeInitOnly.Global serializers need setters.

namespace Compze.Tessaging;

public static class Remotable
{
   public static class AtMostOnce
   {
      //Todo:review: How can we prevent UI's from just defaulting to using a constructor that creates a new guid?
      public class AtMostOnceTypermediaTommand : IAtMostOnceTypermediaTommand
      {
         public TessageId Id { get; protected set; } = new();
      }

      public class AtMostOnceTypermediaTommand<TResult> : AtMostOnceTypermediaTommand, IAtMostOnceTypermediaTommand<TResult>
      {
         protected AtMostOnceTypermediaTommand() {}
      }
   }

   public static class NonTransactional
   {
      public static class Tueries
      {
#pragma warning disable CA1724 //Class name conflicts with namespace name.
         public abstract class Tuery<TResult> : IRemotableTuery<TResult>;
#pragma warning restore CA1724 //Class name conflicts with namespace name.

         public class TaggregateLink<TResult> : Remotable.NonTransactional.Tueries.Tuery<TResult>
         {
            [Obsolete("Used by serializer", error:true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
            // ReSharper disable once UnusedMember.Global
            public TaggregateLink() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
            public TaggregateLink(TaggregateId taggregateId) => TaggregateId = taggregateId;
            public TaggregateId TaggregateId { get; private set; }
         }

         /// <summary>Implements <see cref="IRemotableCreateMyOwnResultTuery{TResult}"/> by calling the default constructor on <typeparamref name="TResult"/></summary>
         public class NewableResultLink<TResult> : Tuery<TResult>, IRemotableCreateMyOwnResultTuery<TResult>
         {
            static readonly Func<TResult> Constructor = Internals.SystemCE.ReflectionCE.Constructor.For<TResult>.DefaultConstructor.Instance;
            public TResult CreateResult() => Constructor();
         }
      }
   }

   public static class ExactlyOnce
   {
      public class Tommand : IExactlyOnceTommand
      {
         public TessageId Id { get; private set; }

         protected Tommand()
            : this(Guid.CreateVersion7()) {}

         Tommand(Guid id) => Id = new TessageId(id);
      }
   }
}
