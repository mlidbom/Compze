using System;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Public;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Core.Tessaging.Typermedia.Public;

public abstract class NavigationSpecification
{
   internal static NavigationSpecification Post(IAtMostOnceTypermediaTommand tommand) => new VoidTommand(tommand);

   public static NavigationSpecification<TResult> Get<TResult>(IRemotableTuery<TResult> tuery) => NavigationSpecification<TResult>.Get(tuery);
   internal static NavigationSpecification<TResult> Post<TResult>(IAtMostOnceTommand<TResult> typermediaTommand) => NavigationSpecification<TResult>.Post(typermediaTommand);

   public void NavigateOn(IRemoteTypermediaNavigator busSession) => NavigateOnAsync(busSession).WaitUnwrappingException();
   public abstract Task NavigateOnAsync(IRemoteTypermediaNavigator busSession);

   class VoidTommand(IAtMostOnceTypermediaTommand tommand) : NavigationSpecification
   {
      readonly IAtMostOnceTypermediaTommand _tommand = tommand;

      public override async Task NavigateOnAsync(IRemoteTypermediaNavigator busSession) => await busSession.PostAsync(_tommand).caf();
   }
}

public abstract class NavigationSpecification<TResult>
{
   public TResult NavigateOn(IRemoteTypermediaNavigator busSession) => NavigateOnAsync(busSession).ResultUnwrappingException();
   public abstract Task<TResult> NavigateOnAsync(IRemoteTypermediaNavigator busSession);

   public NavigationSpecification<TNext> Select<TNext>(Func<TResult, TNext> select) => new NavigationSpecification<TNext>.SelectTuery<TResult>(this, select);

   public NavigationSpecification Post(Func<TResult, IAtMostOnceTypermediaTommand> next) => new PostVoidTommand<TResult>(this, next);
   public NavigationSpecification<TNext> Get<TNext>(Func<TResult, IRemotableTuery<TNext>> next) => new NavigationSpecification<TNext>.ContinuationTuery<TResult>(this, next);
   public NavigationSpecification<TNext> Post<TNext>(Func<TResult, IAtMostOnceTommand<TNext>> next) => new NavigationSpecification<TNext>.PostTommand<TResult>(this, next);

   internal static NavigationSpecification<TResult> Get(IRemotableTuery<TResult> tuery) => new StartTuery(tuery);
   internal static NavigationSpecification<TResult> Post(IAtMostOnceTommand<TResult> typermediaTommand) => new StartTommand(typermediaTommand);

   class SelectTuery<TPrevious> : NavigationSpecification<TResult>
   {
      readonly NavigationSpecification<TPrevious> _previous;
      readonly Func<TPrevious, TResult> _select;

      internal SelectTuery(NavigationSpecification<TPrevious> previous, Func<TPrevious, TResult> select)
      {
         _previous = previous;
         _select = select;
      }

      public override async Task<TResult> NavigateOnAsync(IRemoteTypermediaNavigator busSession)
      {
         var previousResult = await _previous.NavigateOnAsync(busSession).caf();
         return _select(previousResult);
      }
   }

   class StartTuery : NavigationSpecification<TResult>
   {
      readonly IRemotableTuery<TResult> _start;

      internal StartTuery(IRemotableTuery<TResult> start) => _start = start;

      public override async Task<TResult> NavigateOnAsync(IRemoteTypermediaNavigator busSession) => await busSession.GetAsync(_start).caf();
   }

   class StartTommand : NavigationSpecification<TResult>
   {
      readonly IAtMostOnceTommand<TResult> _start;

      internal StartTommand(IAtMostOnceTommand<TResult> start) => _start = start;

      public override async Task<TResult> NavigateOnAsync(IRemoteTypermediaNavigator busSession) => await busSession.PostAsync(_start).caf();
   }

   class ContinuationTuery<TPrevious> : NavigationSpecification<TResult>
   {
      readonly NavigationSpecification<TPrevious> _previous;
      readonly Func<TPrevious, IRemotableTuery<TResult>> _nextTuery;

      internal ContinuationTuery(NavigationSpecification<TPrevious> previous, Func<TPrevious, IRemotableTuery<TResult>> nextTuery)
      {
         _previous = previous;
         _nextTuery = nextTuery;
      }

      public override async Task<TResult> NavigateOnAsync(IRemoteTypermediaNavigator busSession)
      {
         var previousResult = await _previous.NavigateOnAsync(busSession).caf();
         var currentTuery = _nextTuery(previousResult);
         return await busSession.GetAsync(currentTuery).caf();
      }
   }

   class PostTommand<TPrevious> : NavigationSpecification<TResult>
   {
      readonly NavigationSpecification<TPrevious> _previous;
      readonly Func<TPrevious, IAtMostOnceTommand<TResult>> _next;
      internal PostTommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, IAtMostOnceTommand<TResult>> next)
      {
         _previous = previous;
         _next = next;
      }

      public override async Task<TResult> NavigateOnAsync(IRemoteTypermediaNavigator busSession)
      {
         var previousResult = await _previous.NavigateOnAsync(busSession).caf();
         var currentTommand = _next(previousResult);
         return await busSession.PostAsync(currentTommand).caf();
      }
   }

   class PostVoidTommand<TPrevious> : NavigationSpecification
   {
      readonly NavigationSpecification<TPrevious> _previous;
      readonly Func<TPrevious, IAtMostOnceTypermediaTommand> _next;
      internal PostVoidTommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, IAtMostOnceTypermediaTommand> next)
      {
         _previous = previous;
         _next = next;
      }

      public override async Task NavigateOnAsync(IRemoteTypermediaNavigator busSession)
      {
         var previousResult = await _previous.NavigateOnAsync(busSession).caf();
         var currentTommand = _next(previousResult);
         await busSession.PostAsync(currentTommand).caf();
      }
   }
}