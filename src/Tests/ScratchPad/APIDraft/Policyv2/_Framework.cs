// ReSharper disable All
#pragma warning disable //Reviewed OK: This is API experimental code that is never ever used.

using System;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tests.ScratchPad.APIDraft.Policyv2;

interface IHandlerPolicyConfigurationBuilder
{
   void ExclusivelyLock(string resource);
   void InclusivelyLock(string resource);
   void Updates(Type updatedType);
   void Updates(Type updatedType, string id);
   void RequiresUpdtodate(Type required);
   void RequiresUpdtodate(Type required, string id);
   void TriggerWithinPublishingTransaction();
}

interface ITessageHandlerPolicy
{
   void Configure(IHandlerPolicyConfigurationBuilder builder, ITessage tessage);
}


static class Policy
{
   public static ITessageHandlerPolicy NoRestrictions => null;
   public static ITessageHandlerPolicy Publishes<T>() => null;
   public static ITessageHandlerPolicy Sends<T>() => null;

   public static class LockExclusively
   {
      public static ITessageHandlerPolicy ThisHandler;
      public static ITessageHandlerPolicy CurrentTessage;
      public static ITessageHandlerPolicy TaggregateRelatedToTessage;
      public static ITessageHandlerPolicy TessageProcessing => null;
      public static ITessageHandlerPolicy TommandProcessing => null;
      public static ITessageHandlerPolicy TeventProcessing => null;
   }

   public class Inclusivelock
   {
      public static ITessageHandlerPolicy TessageProcessing => null;
      public static ITessageHandlerPolicy TommandProcessing => null;
      public static ITessageHandlerPolicy TeventProcessing => null;
   }

   public static class Updates<T>
   {
      public static ITessageHandlerPolicy WithCurrentTessageTaggregateId() => null;
      public static ITessageHandlerPolicy WithId(ITessageDataExtractor extractEmailFromEmailUpdatedTevent) => null;
   }

   public static class RequiresUpToDate<T>
   {
      public static ITessageHandlerPolicy All => null;
      public static ITessageHandlerPolicy WithCurrentTessageTaggregateId => null;
   }

   public static class OnCascadedTessage
   {
      public static ITessageHandlerPolicy InvokeWithinTriggeringTransaction;
   }
}

interface ISomeDependency { }
interface ITessageDataExtractor { }
class ExtractEmailFromEmailUpdatedTevent : ITessageDataExtractor { }

interface ITessageHandler { }

class TessageHandler
{
   public static ITessageHandler For<T>(string uniqueId, Action<T> handler, params ITessageHandlerPolicy[] policies) => null;
   public static ITessageHandler For<T, D1>(string uniqueId, Action<T, D1> handler, params ITessageHandlerPolicy[] policies) => null;
   public static ITessageHandler For<T, D1, D2>(string uniqueId, Action<T, D1, D2> handler, params ITessageHandlerPolicy[] policies) => null;
}
class TeventHandler : TessageHandler
{
}

class TommandHandler : TessageHandler
{
}

class Endpoint
{
   public Endpoint(params ITessageHandler[] tessageHandlers) { }
}

class CompositePolicy : ITessageHandlerPolicy
{
   public CompositePolicy(params ITessageHandlerPolicy[] policies) { }
   public void Configure(IHandlerPolicyConfigurationBuilder builder, ITessage tessage) { throw new Exception(); }
}