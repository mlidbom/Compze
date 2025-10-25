using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Threading.ResourceAccess;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tessaging.Implementation.TessageHandling;

partial class Inbox
{
   partial class HandlerExecutionEngine
   {
      //refactor: Consider moving all tessage type specific responsibilities into the tessage class or other class. Probably create more subtypes so that no type checking is required. See also inbox.
      partial class Coordinator(ITessagesInFlightTracker globalStateTracker, ITaskRunner taskRunner, ITessageStorage tessageStorage, IServiceLocator serviceLocator, ITessageHandlerRegistry tessageHandlerRegistry, EndpointId endpointId)
      {
         readonly ITaskRunner _taskRunner = taskRunner;
         readonly ITessageStorage _tessageStorage = tessageStorage;
         readonly IServiceLocator _serviceLocator = serviceLocator;
         readonly ITessageHandlerRegistry _tessageHandlerRegistry = tessageHandlerRegistry;
         readonly IThreadShared<NonThreadsafeImplementation> _implementation = IThreadShared.WithDefaultTimeout(new NonThreadsafeImplementation(globalStateTracker, endpointId));

         internal HandlerExecutionTask AwaitExecutableHandlerExecutionTask(IReadOnlyList<ITessageDispatchingRule> dispatchingRules)
         {
            HandlerExecutionTask? handlerExecutionTask = null;
            _implementation.Await(implementation => implementation.TryGetDispatchableTessage(dispatchingRules, out handlerExecutionTask));
            return Result.ReturnNotNull(handlerExecutionTask);
         }

         public Task<object?> EnqueueTessageTask(TransportTessage.InComing tessage) => _implementation.Update(implementation =>
         {
            var inflightTessage = new HandlerExecutionTask(tessage, this, _taskRunner, _tessageStorage, _serviceLocator, _tessageHandlerRegistry);
            implementation.EnqueueTessageTask(inflightTessage);
            return inflightTessage.Task;
         });

         void Succeeded(HandlerExecutionTask queuedTessageInformation) => _implementation.Update(implementation => implementation.Succeeded(queuedTessageInformation));

         void Failed(HandlerExecutionTask queuedTessageInformation, Exception exception) => _implementation.Update(implementation => implementation.Failed(queuedTessageInformation, exception));

         class NonThreadsafeImplementation(ITessagesInFlightTracker globalStateTracker, EndpointId endpointId) : IExecutingTessagesSnapshot
         {
            const int MaxConcurrentlyExecutingHandlers = 20;
            readonly ITessagesInFlightTracker _globalStateTracker = globalStateTracker;
            readonly EndpointId _endpointId = endpointId;


            //performance: Split waiting tessages into prioritized categories: Exactly once event/command, At most once event/command,  NonTransactional tuery
            //don't postpone checking if mutations are allowed to run because we have a ton of queries queued up. Also the queries are likely not allowed to run due to the commands and events!
            //performance: Use static type caching trick to ensure that we know which rules need to be applied to which tessages. Don't check rules that don't apply. (Double dispatching might be required.)
            public IReadOnlyList<TransportTessage.InComing> AtMostOnceCommands => _executingAtMostOnceCommands;
            public IReadOnlyList<TransportTessage.InComing> ExactlyOnceCommands => _executingExactlyOnceCommands;
            public IReadOnlyList<TransportTessage.InComing> ExactlyOnceEvents => _executingExactlyOnceEvents;
            public IReadOnlyList<TransportTessage.InComing> ExecutingNonTransactionalQueries => _executingNonTransactionalQueries;

            readonly List<HandlerExecutionTask> _tessagesWaitingToExecute = [];

            internal bool TryGetDispatchableTessage(IReadOnlyList<ITessageDispatchingRule> dispatchingRules, [NotNullWhen(true)] out HandlerExecutionTask? dispatchable)
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

            internal void Succeeded(HandlerExecutionTask queuedTessageInformation) => DoneDispatching(queuedTessageInformation);

            internal void Failed(HandlerExecutionTask queuedTessageInformation, Exception exception) => DoneDispatching(queuedTessageInformation, exception);

            //Refactor: Switching should not be necessary. See also inbox.
            void Dispatching(HandlerExecutionTask dispatchable)
            {
               _executingTessages++;

               switch(dispatchable.TransportTessage.TessageTypeEnum)
               {
                  case TransportTessage.TransportTessageType.ExactlyOnceEvent:
                     _executingExactlyOnceEvents.Add(dispatchable.TransportTessage);
                     break;
                  case TransportTessage.TransportTessageType.AtMostOnceCommandWithReturnValue:
                  case TransportTessage.TransportTessageType.AtMostOnceCommand:
                     _executingAtMostOnceCommands.Add(dispatchable.TransportTessage);
                     break;
                  case TransportTessage.TransportTessageType.ExactlyOnceCommand:
                     _executingExactlyOnceCommands.Add(dispatchable.TransportTessage);
                     break;
                  case TransportTessage.TransportTessageType.NonTransactionalTuery:
                     _executingNonTransactionalQueries.Add(dispatchable.TransportTessage);
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
                  case TransportTessage.TransportTessageType.ExactlyOnceEvent:
                     _executingExactlyOnceEvents.Remove(doneExecuting.TransportTessage);
                     break;
                  case TransportTessage.TransportTessageType.AtMostOnceCommandWithReturnValue:
                  case TransportTessage.TransportTessageType.AtMostOnceCommand:
                     _executingAtMostOnceCommands.Remove(doneExecuting.TransportTessage);
                     break;
                  case TransportTessage.TransportTessageType.ExactlyOnceCommand:
                     _executingExactlyOnceCommands.Remove(doneExecuting.TransportTessage);
                     break;
                  case TransportTessage.TransportTessageType.NonTransactionalTuery:
                     _executingNonTransactionalQueries.Remove(doneExecuting.TransportTessage);
                     break;
                  default:
                     throw new ArgumentOutOfRangeException();
               }

               _globalStateTracker.DoneWith(doneExecuting.TransportTessage, _endpointId, exception);
            }

            int _executingTessages;
            readonly List<TransportTessage.InComing> _executingExactlyOnceCommands = [];
            readonly List<TransportTessage.InComing> _executingAtMostOnceCommands = [];
            readonly List<TransportTessage.InComing> _executingExactlyOnceEvents = [];
            readonly List<TransportTessage.InComing> _executingNonTransactionalQueries = [];
         }
      }
   }
}