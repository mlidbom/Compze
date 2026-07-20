using Compze.Abstractions.Public;
using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore.Abstractions.Public;
using Compze.Tessaging.Engine.HandlerRegistration.TessageHandlers;

// ReSharper disable MemberCanBePrivate.Global they are public so that serializers work

namespace Compze.Teventive.TeventStore.Typermedia;

public partial class TeventStoreApi
{
   public partial class TueryApi
   {
      public class TaggregateLink<TTaggregate> : StrictlyLocal.Tueries.StrictlyLocalTuery<TaggregateLink<TTaggregate>, TTaggregate> where TTaggregate : class, ITaggregate
      {
         [Obsolete("Used by serializer", error:true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
         // ReSharper disable once UnusedMember.Global
         public TaggregateLink() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

         internal TaggregateLink(TaggregateId id) => Id = id;
         public TaggregateId Id { get; private set; }

         internal static void RegisterHandler(TessageHandlerRegistrar registrar) => registrar.ForTuery((TaggregateLink<TTaggregate> tuery, ITeventStoreUpdater updater) => updater.Get<TTaggregate>(tuery.Id));
      }

      public class GetTaggregateHistory<TTevent> : StrictlyLocal.Tueries.StrictlyLocalTuery<GetTaggregateHistory<TTevent>, IEnumerable<ITaggregateTevent<TTevent>>> where TTevent : ITaggregateTevent
      {
         [Obsolete("for serializer", error: true)]
         // ReSharper disable once UnusedMember.Global
         public GetTaggregateHistory() => Id = null!;

         internal GetTaggregateHistory(TaggregateId id) => Id = id;
         public TaggregateId Id { get; private set; }

         internal static void RegisterHandler(TessageHandlerRegistrar registrar) => registrar.ForTuery((GetTaggregateHistory<TTevent> tuery, ITeventStoreReader reader) => reader.GetHistory(tuery.Id).Cast<ITaggregateTevent<TTevent>>());
      }

      public class GetReadonlyCopyOfTaggregate<TTaggregate> : StrictlyLocal.Tueries.StrictlyLocalTuery<GetReadonlyCopyOfTaggregate<TTaggregate>, TTaggregate> where TTaggregate : class, ITaggregate
      {
         [Obsolete("Used by serializer", error:true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
         // ReSharper disable once UnusedMember.Global
         public GetReadonlyCopyOfTaggregate() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

         internal GetReadonlyCopyOfTaggregate(TaggregateId id) => Id = id;
         public TaggregateId Id { get; private set; }

         internal static void RegisterHandler(TessageHandlerRegistrar registrar) => registrar.ForTuery((GetReadonlyCopyOfTaggregate<TTaggregate> tuery, ITeventStoreReader reader) => reader.GetReadonlyCopy<TTaggregate>(tuery.Id));
      }

      public class GetReadonlyCopyOfTaggregateVersion<TTaggregate> : StrictlyLocal.Tueries.StrictlyLocalTuery<GetReadonlyCopyOfTaggregateVersion<TTaggregate>, TTaggregate> where TTaggregate : class, ITaggregate
      {
         [Obsolete("Used by serializer", error:true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
         // ReSharper disable once UnusedMember.Global
         public GetReadonlyCopyOfTaggregateVersion() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

         internal GetReadonlyCopyOfTaggregateVersion(TaggregateId id, int version)
         {
            Id = id;
            Version = version;
         }

         public TaggregateId Id { get; private set; }
         public int Version { get; private set; }

         internal static void RegisterHandler(TessageHandlerRegistrar registrar) => registrar.ForTuery((GetReadonlyCopyOfTaggregateVersion<TTaggregate> tuery, ITeventStoreReader reader) => reader.GetReadonlyCopyOfVersion<TTaggregate>(tuery.Id, tuery.Version));
      }
   }

   public partial class TommandApi
   {
      public class SaveTaggregate<TTaggregate> : StrictlyLocal.Tommands.StrictlyLocalTommand
         where TTaggregate : class, ITaggregate
      {
         internal SaveTaggregate(TTaggregate entity) => Entity = entity;
         public TTaggregate Entity { get; private set; }

         internal static void RegisterHandler(TessageHandlerRegistrar registrar) => registrar.ForTommand((SaveTaggregate<TTaggregate> tommand, ITeventStoreUpdater updater) => updater.Save(tommand.Entity));
      }
   }

   public static void RegisterHandlersForTaggregate<TTaggregate, TTevent>(TessageHandlerRegistrar registrar)
      where TTaggregate : class, ITaggregate<TTevent>
      where TTevent : ITaggregateTevent
   {
      TommandApi.SaveTaggregate<TTaggregate>.RegisterHandler(registrar);
      TueryApi.TaggregateLink<TTaggregate>.RegisterHandler(registrar);
      TueryApi.GetReadonlyCopyOfTaggregate<TTaggregate>.RegisterHandler(registrar);
      TueryApi.GetReadonlyCopyOfTaggregateVersion<TTaggregate>.RegisterHandler(registrar);
      TueryApi.GetTaggregateHistory<TTevent>.RegisterHandler(registrar);
   }
}
