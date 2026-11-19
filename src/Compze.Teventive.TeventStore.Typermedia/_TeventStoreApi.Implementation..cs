using Compze.Abstractions;
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TeventStore.Abstractions;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.TessageTypes;

// ReSharper disable MemberCanBePrivate.Global they are public so that serializers work

namespace Compze.Teventive.TeventStore.Typermedia;

public partial class TeventStoreApi
{
   public partial class TueryApi
   {
      public class TaggregateLink<TTaggregate> : StrictlyLocal.Tueries.StrictlyLocalTuery<TaggregateLink<TTaggregate>, TTaggregate> where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>
      {
         [Obsolete("Used by serializer", error:true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
         // ReSharper disable once UnusedMember.Global
         public TaggregateLink() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

         internal TaggregateLink(TaggregateId id) => Id = id;
         public TaggregateId Id { get; private set; }

         internal static void RegisterHandler(TypermediaHandlerRegistrar registrar) => registrar.ForTuery((TaggregateLink<TTaggregate> tuery, ITaggregateStoreUpdater updater) => updater.GetForUpdate<TTaggregate>(tuery.Id));
      }

      public class GetTaggregateHistory<TTevent> : StrictlyLocal.Tueries.StrictlyLocalTuery<GetTaggregateHistory<TTevent>, IEnumerable<ITaggregateTevent<TTevent>>> where TTevent : class, ITaggregateTeventWithIdentity
      {
         [Obsolete("for serializer", error: true)]
         // ReSharper disable once UnusedMember.Global
         public GetTaggregateHistory() => Id = null!;

         internal GetTaggregateHistory(TaggregateId id) => Id = id;
         public TaggregateId Id { get; private set; }

         internal static void RegisterHandler(TypermediaHandlerRegistrar registrar) => registrar.ForTuery((GetTaggregateHistory<TTevent> tuery, ITeventStoreReader reader) => reader.GetHistory(tuery.Id).Cast<ITaggregateTevent<TTevent>>());
      }

      ///<summary>Gets the taggregate as <see cref="ITaggregateStoreReader"/> gets it.</summary>
      public class GetTaggregate<TTaggregate> : StrictlyLocal.Tueries.StrictlyLocalTuery<GetTaggregate<TTaggregate>, TTaggregate> where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>
      {
         [Obsolete("Used by serializer", error:true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
         // ReSharper disable once UnusedMember.Global
         public GetTaggregate() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

         internal GetTaggregate(TaggregateId id) => Id = id;
         public TaggregateId Id { get; private set; }

         internal static void RegisterHandler(TypermediaHandlerRegistrar registrar) => registrar.ForTuery((GetTaggregate<TTaggregate> tuery, ITaggregateStoreReader reader) => reader.Get<TTaggregate>(tuery.Id));
      }

      public class GetReadonlyCopyOfTaggregateVersion<TTaggregate> : StrictlyLocal.Tueries.StrictlyLocalTuery<GetReadonlyCopyOfTaggregateVersion<TTaggregate>, TTaggregate> where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>
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

         internal static void RegisterHandler(TypermediaHandlerRegistrar registrar) => registrar.ForTuery((GetReadonlyCopyOfTaggregateVersion<TTaggregate> tuery, ITeventStoreReader reader) => reader.GetReadonlyCopyOfVersion<TTaggregate>(tuery.Id, tuery.Version));
      }
   }

   public partial class TommandApi
   {
      public class AddTaggregate<TTaggregate> : StrictlyLocal.Tommands.StrictlyLocalTommand
         where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>
      {
         internal AddTaggregate(TTaggregate entity) => Entity = entity;
         public TTaggregate Entity { get; private set; }

         internal static void RegisterHandler(TypermediaHandlerRegistrar registrar) => registrar.ForTommand((AddTaggregate<TTaggregate> tommand, ITaggregateStoreUpdater updater) => updater.Add(tommand.Entity));
      }
   }

   public static void RegisterHandlersForTaggregate<TTaggregate, TTevent>(TypermediaHandlerRegistrar registrar)
      where TTaggregate : class, ITaggregate<TTevent>
      where TTevent : class, ITaggregateTeventWithIdentity
   {
      TommandApi.AddTaggregate<TTaggregate>.RegisterHandler(registrar);
      TueryApi.TaggregateLink<TTaggregate>.RegisterHandler(registrar);
      TueryApi.GetTaggregate<TTaggregate>.RegisterHandler(registrar);
      TueryApi.GetReadonlyCopyOfTaggregateVersion<TTaggregate>.RegisterHandler(registrar);
      TueryApi.GetTaggregateHistory<TTevent>.RegisterHandler(registrar);
   }
}
