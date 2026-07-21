using System.Reflection;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.TessageTypes;
using Compze.Threading;

namespace Compze.Tessaging.Validation._internal;

static class TessageTypeInspector
{
   static readonly TessageTypeDesignRule[] TessageTypeDesignRules =
   [
      new MustBeITessage(),
      new CannotBeBothTommandAndTevent(),
      new CannotBeBothTommandAndTuery(),
      new CannotBeBothTeventAndTuery(),
      new CannotBeBothRemotableAndStrictlyLocal(),
      new CannotForbidAndRequireTransactionalSender(),
      new WrapperTeventInterfaceMustBeGenericAndDeclareTypeParameterAsAsOutParameter()
   ];

   static readonly IMonitor Monitor = IMonitor.New();

   static IReadOnlySet<Type> _successfullyInspectedSubscribableTypes = new HashSet<Type>();
   public static void AssertValidForSubscription(Type type)
   {
      if(_successfullyInspectedSubscribableTypes.Contains(type)) return;

      Monitor.Locked(() =>
      {
         if(!type.Is<ITevent>()) throw new Exception($"You can only subscribe to subtypes of {typeof(ITevent).GetFullNameCompilable()}");
         if(!type.IsInterface) throw new Exception($"{type.GetFullNameCompilable()} is not an interface. You can only subscribe to tevent interfaces because as soon as you subscribe to classes you loose the guarantees of semantic routing since classes do not support multiple inheritance.");
         AssertTypeIsValidInternal(type);
         Interlocked.Exchange(ref _successfullyInspectedSubscribableTypes, _successfullyInspectedSubscribableTypes.AddToCopy(type));
      });
   }

   static IReadOnlySet<Type> _successfullyInspectedTypes = new HashSet<Type>();
   public static void AssertValid(Type type)
   {
      if(_successfullyInspectedTypes.Contains(type)) return;

      Monitor.Locked(() =>
      {
         if(_successfullyInspectedTypes.Contains(type)) return;

         AssertTypeIsValidInternal(type);

         Interlocked.Exchange(ref _successfullyInspectedTypes, _successfullyInspectedTypes.AddToCopy(type));
      });
   }

   static void AssertTypeIsValidInternal(Type type)
   {
      foreach(var rule in TessageTypeDesignRules) rule.AssertFulfilledBy(type);
   }

   public abstract class TessageTypeDesignRule
   {
      internal abstract void AssertFulfilledBy(Type type);
   }

   public abstract class SimpleTessageTypeDesignRule : TessageTypeDesignRule
   {
      protected abstract bool IsInvalid(Type type);
      protected abstract string CreateTessage(Type type);

      internal override void AssertFulfilledBy(Type type)
      {
         if(IsInvalid(type))
         {
            throw new TessageTypeDesignViolationException(CreateTessage(type));
         }
      }
   }

   class MustBeITessage : SimpleTessageTypeDesignRule
   {
      protected override bool IsInvalid(Type type) => !type.Implements<ITessage>();
      protected override string CreateTessage(Type type) => $"{type.GetFullNameCompilable()} does not implement {typeof(ITessage).GetFullNameCompilable()}";
   }

   class MutuallyExclusiveInterfaces<TInterface1, TInterface2> : SimpleTessageTypeDesignRule
   {
      protected override bool IsInvalid(Type type) => typeof(TInterface1).IsAssignableFrom(type) && typeof(TInterface2).IsAssignableFrom(type);
      protected override string CreateTessage(Type type) => $"{type.GetFullNameCompilable()} implements both {typeof(TInterface1).GetFullNameCompilable()} and {typeof(TInterface2).GetFullNameCompilable()}";
   }

   class CannotBeBothTommandAndTevent : MutuallyExclusiveInterfaces<ITommand, ITevent>;

   class CannotBeBothTommandAndTuery : MutuallyExclusiveInterfaces<ITommand, ITuery<object>>;

   class CannotBeBothTeventAndTuery : MutuallyExclusiveInterfaces<ITevent, ITuery<object>>;

   class CannotBeBothRemotableAndStrictlyLocal : MutuallyExclusiveInterfaces<IRemotableTessage, IStrictlyLocalTessage>;

   //A strictly local tessage is exempt: the prohibition against remote sending from within a transaction is vacuous for a tessage that never travels remotely,
   //so requiring a transactional sender contradicts nothing. For every other tessage the combination forbids remote sending outright while claiming nothing about locality - a contradiction.
   class CannotForbidAndRequireTransactionalSender : SimpleTessageTypeDesignRule
   {
      protected override bool IsInvalid(Type type) => type.Is<IMustBeSentTransactionally>()
                                                   && type.Is<ICannotBeSentRemotelyFromWithinTransaction>()
                                                   && !type.Is<IStrictlyLocalTessage>();

      protected override string CreateTessage(Type type) =>
         $"{type.GetFullNameCompilable()} implements both {typeof(IMustBeSentTransactionally).GetFullNameCompilable()} and {typeof(ICannotBeSentRemotelyFromWithinTransaction).GetFullNameCompilable()} without implementing {typeof(IStrictlyLocalTessage).GetFullNameCompilable()}. For a tessage that may be sent remotely that is a contradiction: it must be sent from within a transaction, yet may never be sent remotely from within one. Declare the tessage {typeof(IStrictlyLocalTessage).GetFullNameCompilable()} if it never travels remotely, or drop one of the two transactional markers.";
   }

   class WrapperTeventInterfaceMustBeGenericAndDeclareTypeParameterAsAsOutParameter : TessageTypeDesignRule
   {
      internal override void AssertFulfilledBy(Type type)
      {
         if(type.Is<IPublisherTevent<ITevent>>())
         {
            var allInterfaces = type.GetInterfaces().ToList();
            if(type.IsInterface) allInterfaces.Add(type);

            var wrapperInterfacesImplemented = allInterfaces.Where(@interface => @interface.Is<IPublisherTevent<ITevent>>()).ToArray();
            var nonGeneric = wrapperInterfacesImplemented.FirstOrDefault(@interface => !@interface.IsGenericType);
            if(nonGeneric != null) throw new TessageTypeDesignViolationException($"{nonGeneric.GetFullNameCompilable()} implements {typeof(IPublisherTevent<>).GetFullNameCompilable()} but is not generic. This means that routing based on the covariance of the wrapping type is impossible and thus semantic routing breaks down.");

            var typeParameterIsNotOut = wrapperInterfacesImplemented.FirstOrDefault(@interface => !@interface.GetGenericTypeDefinition().GetGenericArguments()[0].GenericParameterAttributes.HasFlag(GenericParameterAttributes.Covariant));
            if(typeParameterIsNotOut != null) throw new TessageTypeDesignViolationException($"{typeParameterIsNotOut.GetFullNameCompilable()} implements {typeof(IPublisherTevent<>).GetFullNameCompilable()} but does not declare the type parameter as covariant(out). If the type parameter is not covariant routing to derived types does not work because they are not assignable to the base interface type");
         }
      }
   }
}
