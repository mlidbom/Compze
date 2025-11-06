using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Compze.Core.Tessaging.Public;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.SystemCE.ThreadingCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Core.Tessaging.Teventive.Infrastructure.Validation;

partial class TessageTypeInspector
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

   static readonly ILock Monitor = LockCE.WithDefaultTimeout();

   static IReadOnlySet<Type> _successfullyInspectedSubscribableTypes = new HashSet<Type>();
   internal static void AssertValidForSubscription(Type type)
   {
      if(_successfullyInspectedSubscribableTypes.Contains(type)) return;

      Monitor.Update(() =>
      {
         if(!type.Is<ITevent>()) throw new Exception($"You can only subscribe to subtypes of {typeof(ITevent).GetFullNameCompilable()}");
         if(!type.IsInterface) throw new Exception($"{type.GetFullNameCompilable()} is not an interface. You can only subscribe to tevent interfaces because as soon as you subscribe to classes you loose the guarantees of semantic routing since classes do not support multiple inheritance.");
         AssertTypeIsValidInternal(type);
         OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _successfullyInspectedSubscribableTypes, type);
      });
   }

   static IReadOnlySet<Type> _successfullyInspectedTypes = new HashSet<Type>();
   internal static void AssertValid(Type type)
   {
      if(_successfullyInspectedTypes.Contains(type)) return;

      Monitor.Update(() =>
      {
         if(_successfullyInspectedTypes.Contains(type)) return;

         AssertTypeIsValidInternal(type);

         OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _successfullyInspectedTypes, type);
      });
   }

   static void AssertTypeIsValidInternal(Type type) =>
      TessageTypeDesignRules.ForEach(rule => rule.AssertFulfilledBy(type));

   abstract class TessageTypeDesignRule
   {
      internal abstract void AssertFulfilledBy(Type type);
   }

   abstract class SimpleTessageTypeDesignRule : TessageTypeDesignRule
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

   class CannotForbidAndRequireTransactionalSender : MutuallyExclusiveInterfaces<IMustBeSentTransactionally, ICannotBeSentRemotelyFromWithinTransaction>;

   class WrapperTeventInterfaceMustBeGenericAndDeclareTypeParameterAsAsOutParameter : TessageTypeDesignRule
   {
      internal override void AssertFulfilledBy(Type type)
      {
         if(type.Is<IPublisherIdentifyingTevent<ITevent>>())
         {
            var allInterfaces = type.GetInterfaces().ToList();
            if(type.IsInterface) allInterfaces.Add(type);

            var wrapperInterfacesImplemented = allInterfaces.Where(@interface => @interface.Is<IPublisherIdentifyingTevent<ITevent>>()).ToArray();
            var nonGeneric = wrapperInterfacesImplemented.FirstOrDefault(@interface => !@interface.IsGenericType);
            if(nonGeneric != null) throw new TessageTypeDesignViolationException($"{nonGeneric.GetFullNameCompilable()} implements {typeof(IPublisherIdentifyingTevent<>).GetFullNameCompilable()} but is not generic. This means that routing based on the covariance of the wrapping type is impossible and thus semantic routing breaks down.");

            var typeParameterIsNotOut = wrapperInterfacesImplemented.FirstOrDefault(@interface => !@interface.GetGenericTypeDefinition().GetGenericArguments()[0].GenericParameterAttributes.HasFlag(GenericParameterAttributes.Covariant));
            if(typeParameterIsNotOut != null) throw new TessageTypeDesignViolationException($"{typeParameterIsNotOut.GetFullNameCompilable()} implements {typeof(IPublisherIdentifyingTevent<>).GetFullNameCompilable()} but does not declare the type parameter as covariant(out). If the type parameter is not covariant routing to derived types does not work because they are not assignable to the base interface type");
         }
      }
   }
}