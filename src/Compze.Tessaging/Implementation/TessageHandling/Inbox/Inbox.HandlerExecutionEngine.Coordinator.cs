using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using Compze.Contracts;
using static Compze.Contracts.Contract;

namespace Compze.Tessaging.Implementation.TessageHandling.Inbox;

public partial class Inbox
{
   public partial class HandlerExecutionEngine
   {
      //refactor: Consider moving all tessage type specific responsibilities into the tessage class or other class. Probably create more subtypes so that no type checking is required. See also inbox.
      public partial class Coordinator(ITessagesInFlightTracker globalStateTracker, ITaskRunner taskRunner, ITessageStorage tessageStorage, IServiceLocator serviceLocator, ITessageHandlerRegistry tessageHandlerRegistry, EndpointId endpointId)
      {
         readonly ITaskRunner _taskRunner = taskRunner;
         readonly ITessageStorage _tessageStorage = tessageStorage;
         readonly IServiceLocator _serviceLocator = serviceLocator;
         readonly ITessageHandlerRegistry _tessageHandlerRegistry = tessageHandlerRegistry;
         readonly IThreadShared<NonThreadsafeImplementation> _implementation = IThreadShared.WithDefaultTimeouts(new NonThreadsafeImplementation(globalStateTracker, endpointId));

         public HandlerExecutionTask AwaitExecutableHandlerExecutionTask(IReadOnlyList<ITessageDispatchingRule> dispatchingRules)
         {
            HandlerExecutionTask? handlerExecutionTask = null;
            _implementation.Await(implementation => implementation.TryGetDispatchableTessage(dispatchingRules, out handlerExecutionTask));
            return handlerExecutionTask._assert().NotNull();
         }

         public Task<object?> EnqueueTessageTask(TransportTessage.InComing tessage) => _implementation.Update(implementation =>
         {
            var inflightTessage = new HandlerExecutionTask(tessage, this, _taskRunner, _tessageStorage, _serviceLocator, _tessageHandlerRegistry);
            implementation.EnqueueTessageTask(inflightTessage);
            return inflightTessage.Task;
         });

         void Succeeded(HandlerExecutionTask queuedTessageInformation) => _implementation.Update(implementation => implementation.Succeeded(queuedTessageInformation));

         void Failed(HandlerExecutionTask queuedTessageInformation, Exception exception) => _implementation.Update(implementation => implementation.Failed(queuedTessageInformation, exception));

         public class NonThreadsafeImplementation(ITessagesInFlightTracker globalStateTracker, EndpointId endpointId) : IExecutingTessagesSnapshot
         {
            const int MaxConcurrentlyExecutingHandlers = 20;
            readonly ITessagesInFlightTracker _globalStateTracker = globalStateTracker;
            readonly EndpointId _endpointId = endpointId;


            //performance: Split waiting tessages into prioritized categories: Exactly once tevent/tommand, At most once tevent/tommand,  NonTransactional tuery
            //don't postpone checking if mutations are allowed to run because we have a ton of tueries queued up. Also the tueries are likely not allowed to run due to the tommands and tevents!
            //performance: Use static type caching trick to ensure that we know which rules need to be applied to which tessages. Don't check rules that don't apply. (Double dispatching might be required.)
            public IReadOnlyList<TransportTessage.InComing> AtMostOnceTommands => _executingAtMostOnceTommands;
            public IReadOnlyList<TransportTessage.InComing> ExactlyOnceTommands => _executingExactlyOnceTommands;
            public IReadOnlyList<TransportTessage.InComing> ExactlyOnceTevents => _executingExactlyOnceTevents;
            public IReadOnlyList<TransportTessage.InComing> ExecutingNonTransactionalTueries => _executingNonTransactionalTueries;

            readonly List<HandlerExecutionTask> _tessagesWaitingToExecute = [];

            public bool TryGetDispatchableTessage(IReadOnlyList<ITessageDispatchingRule> dispatchingRules, [NotNullWhen(true)] out HandlerExecutionTask? dispatchable)
            {
               dispatchable = null!;
               if(_executingTessages >= MaxConcurrentlyExecutingHandlers)
               {
                  return false;
               }

               dispatchable = _tessagesWaitingToExecute
                 .FirstOrDefault(queuedTask => dispatchingRules.All(rule => rule.CanBeDispatched(this, queuedTask.TransportTessage)));

               if (dispatchable == null)
               {
                  return false;
               }

               Dispatching(dispatchable);
               return true;
            }

            public void EnqueueTessageTask(HandlerExecutionTask tessage) => _tessagesWaitingToExecute.Add(tessage);

            public void Succeeded(HandlerExecutionTask queuedTessageInformation) => DoneDispatching(queuedTessageInformation);

            public void Failed(HandlerExecutionTask queuedTessageInformation, Exception exception) => DoneDispatching(queuedTessageInformation, exception);

            //Refactor: Switching should not be necessary. See also inbox.
            void Dispatching(HandlerExecutionTask dispatchable)
            {
               _executingTessages++;

               switch(dispatchable.TransportTessage.TessageTypeEnum)
               {
                  case TransportTessageType.ExactlyOnceTevent:
                     _executingExactlyOnceTevents.Add(dispatchable.TransportTessage);
                     break;
                  case TransportTessageType.TypermediaAtMostOnceTommandWithReturnValue:
                  case TransportTessageType.TypermediaAtMostOnceTommand:
                     _executingAtMostOnceTommands.Add(dispatchable.TransportTessage);
                     break;
                  case TransportTessageType.ExactlyOnceTommand:
                     _executingExactlyOnceTommands.Add(dispatchable.TransportTessage);
                     break;
                  case TransportTessageType.TyperMediaTuery:
                     _executingNonTransactionalTueries.Add(dispatchable.TransportTessage);
                     break;
                  default:
                     throw new ArgumentOutOfRangeException();
               }

               _tessagesWaitingToExecute.Remove(dispatchable);
            }

            //Refactor: Switching should not be necessary. See also inbox.
            void DoneDispatching(HandlerExecutionTask doneExecuting, Exception? exception = null)
            {
               _executingTessages--;

               switch(doneExecuting.TransportTessage.TessageTypeEnum)
               {
                  case TransportTessageType.ExactlyOnceTevent:
                     _executingExactlyOnceTevents.Remove(doneExecuting.TransportTessage);
                     break;
                  case TransportTessageType.TypermediaAtMostOnceTommandWithReturnValue:
                  case TransportTessageType.TypermediaAtMostOnceTommand:
                     _executingAtMostOnceTommands.Remove(doneExecuting.TransportTessage);
                     break;
                  case TransportTessageType.ExactlyOnceTommand:
                     _executingExactlyOnceTommands.Remove(doneExecuting.TransportTessage);
                     break;
                  case TransportTessageType.TyperMediaTuery:
                     _executingNonTransactionalTueries.Remove(doneExecuting.TransportTessage);
                     break;
                  default:
                     throw new ArgumentOutOfRangeException();
               }

               _globalStateTracker.DoneWith(doneExecuting.TransportTessage, _endpointId, exception);
            }

            int _executingTessages;
            readonly List<TransportTessage.InComing> _executingExactlyOnceTommands = [];
            readonly List<TransportTessage.InComing> _executingAtMostOnceTommands = [];
            readonly List<TransportTessage.InComing> _executingExactlyOnceTevents = [];
            readonly List<TransportTessage.InComing> _executingNonTransactionalTueries = [];
         }
      }
   }
}