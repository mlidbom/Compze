using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Public;
using Compze.Typermedia.HandlerRegistration;

// ReSharper disable MemberCanBePrivate.Global they are public so that serializers work

namespace Compze.Tessaging.TyperMediaApi.EventStore;

public partial class TeventStoreApi
{
   public partial class TueryApi
   {
      public class TaggregateLink<TTaggregate> : TessageTypes.StrictlyLocal.Tueries.StrictlyLocalTuery<TaggregateLink<TTaggregate>, TTaggregate> where TTaggregate : class, ITaggregate
      {
         [Obsolete("Used by serializer", error:true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
         // ReSharper disable once UnusedMember.Global
         public TaggregateLink() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

         internal TaggregateLink(TaggregateId id) => Id = id;
         public TaggregateId Id { get; private set; }

         internal static void RegisterHandler(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery((TaggregateLink<TTaggregate> tuery, ITeventStoreUpdater updater) => updater.Get<TTaggregate>(tuery.Id));
      }

      public class GetTaggregateHistory<TTevent> : TessageTypes.StrictlyLocal.Tueries.StrictlyLocalTuery<GetTaggregateHistory<TTevent>, IEnumerable<TTevent>> where TTevent : ITaggregateTevent
      {
         [Obsolete("for serializer", error: true)]
         // ReSharper disable once UnusedMember.Global
         public GetTaggregateHistory() => Id = null!;

         internal GetTaggregateHistory(TaggregateId id) => Id = id;
         public TaggregateId Id { get; private set; }

         internal static void RegisterHandler(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery((GetTaggregateHistory<TTevent> tuery, ITeventStoreReader reader) => reader.GetHistory(tuery.Id).Cast<TTevent>());
      }

      public class GetReadonlyCopyOfTaggregate<TTaggregate> : TessageTypes.StrictlyLocal.Tueries.StrictlyLocalTuery<GetReadonlyCopyOfTaggregate<TTaggregate>, TTaggregate> where TTaggregate : class, ITaggregate
      {
         [Obsolete("Used by serializer", error:true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
         // ReSharper disable once UnusedMember.Global
         public GetReadonlyCopyOfTaggregate() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

         internal GetReadonlyCopyOfTaggregate(TaggregateId id) => Id = id;
         public TaggregateId Id { get; private set; }

         internal static void RegisterHandler(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery((GetReadonlyCopyOfTaggregate<TTaggregate> tuery, ITeventStoreReader reader) => reader.GetReadonlyCopy<TTaggregate>(tuery.Id));
      }

      public class GetReadonlyCopyOfTaggregateVersion<TTaggregate> : TessageTypes.StrictlyLocal.Tueries.StrictlyLocalTuery<GetReadonlyCopyOfTaggregateVersion<TTaggregate>, TTaggregate> where TTaggregate : class, ITaggregate
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

         internal static void RegisterHandler(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery((GetReadonlyCopyOfTaggregateVersion<TTaggregate> tuery, ITeventStoreReader reader) => reader.GetReadonlyCopyOfVersion<TTaggregate>(tuery.Id, tuery.Version));
      }
   }

   public partial class TommandApi
   {
      public class SaveTaggregate<TTaggregate> : TessageTypes.StrictlyLocal.Tommands.StrictlyLocalTommand
         where TTaggregate : class, ITaggregate
      {
         internal SaveTaggregate(TTaggregate entity) => Entity = entity;
         public TTaggregate Entity { get; private set; }

         internal static void RegisterHandler(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTommand((SaveTaggregate<TTaggregate> tommand, ITeventStoreUpdater updater) => updater.Save(tommand.Entity));
      }
   }

   public static void RegisterHandlersForTaggregate<TTaggregate, TTevent>(TypermediaHandlerRegistrarWithDependencyInjectionSupport typermediaRegistrar)
      where TTaggregate : class, ITaggregate<TTevent>
      where TTevent : ITaggregateTevent
   {
      TommandApi.SaveTaggregate<TTaggregate>.RegisterHandler(typermediaRegistrar);
      TueryApi.TaggregateLink<TTaggregate>.RegisterHandler(typermediaRegistrar);
      TueryApi.GetReadonlyCopyOfTaggregate<TTaggregate>.RegisterHandler(typermediaRegistrar);
      TueryApi.GetReadonlyCopyOfTaggregateVersion<TTaggregate>.RegisterHandler(typermediaRegistrar);
      TueryApi.GetTaggregateHistory<TTevent>.RegisterHandler(typermediaRegistrar);
   }
}
