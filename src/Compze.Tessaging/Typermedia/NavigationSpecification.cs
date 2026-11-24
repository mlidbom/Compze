using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia;

///<remarks>refactor: <see cref="Get{TResult}"/> is public but Post is internal, so a navigation that starts at a tommand starts from<br/>
/// the <c>tommand.Post()</c> extension instead: the two starts are found in different places. (website-and-docs, 2026-11-24)</remarks>
///<remarks>refactor: This non-generic specification, which ends in a post that returns nothing, can be navigated only<br/>
/// synchronously: <see cref="NavigateOnAsync"/> is protected, and the navigator's <c>NavigateAsync</c> extension takes<br/>
/// only <see cref="NavigationSpecification{TResult}"/>. (website-and-docs, 2026-11-24)</remarks>
///<remarks>review: Specifications navigate only <see cref="IRemoteTypermediaNavigator"/>: there is no in-process way to compose<br/>
/// navigations, and each tuery or tommand type is either strictly local or remotable, so moving an API out of its process<br/>
/// changes its types. Is one API meant to be servable both in-process and remotely? (website-and-docs, 2026-11-24)</remarks>
public abstract class NavigationSpecification
{
   internal static NavigationSpecification Post(IAtMostOnceTypermediaTommand tommand) => new VoidTommand(tommand);

   public static NavigationSpecification<TResult> Get<TResult>(IRemotableTuery<TResult> tuery) => NavigationSpecification<TResult>.Get(tuery);
   internal static NavigationSpecification<TResult> Post<TResult>(IAtMostOnceTypermediaTommand<TResult> typermediaTommand) => NavigationSpecification<TResult>.Post(typermediaTommand);

   public void NavigateOn(IRemoteTypermediaNavigator busSession) => NavigateOnAsync(busSession).WaitUnwrappingException();
   protected abstract Task NavigateOnAsync(IRemoteTypermediaNavigator busSession);

   class VoidTommand(IAtMostOnceTypermediaTommand tommand) : NavigationSpecification
   {
      readonly IAtMostOnceTypermediaTommand _tommand = tommand;

      protected override async Task NavigateOnAsync(IRemoteTypermediaNavigator busSession) => await busSession.PostAsync(_tommand).caf();
   }
}

public abstract class NavigationSpecification<TResult>
{
   public TResult NavigateOn(IRemoteTypermediaNavigator busSession) => NavigateOnAsync(busSession).ResultUnwrappingException();
   internal abstract Task<TResult> NavigateOnAsync(IRemoteTypermediaNavigator busSession);

   public NavigationSpecification<TNext> Select<TNext>(Func<TResult, TNext> select) => new NavigationSpecification<TNext>.SelectTuery<TResult>(this, select);

   public NavigationSpecification Post(Func<TResult, IAtMostOnceTypermediaTommand> next) => new PostVoidTommand<TResult>(this, next);
   public NavigationSpecification<TNext> Get<TNext>(Func<TResult, IRemotableTuery<TNext>> next) => new NavigationSpecification<TNext>.ContinuationTuery<TResult>(this, next);
   public NavigationSpecification<TNext> Post<TNext>(Func<TResult, IAtMostOnceTypermediaTommand<TNext>> next) => new NavigationSpecification<TNext>.PostTommand<TResult>(this, next);

   internal static NavigationSpecification<TResult> Get(IRemotableTuery<TResult> tuery) => new StartTuery(tuery);
   internal static NavigationSpecification<TResult> Post(IAtMostOnceTypermediaTommand<TResult> typermediaTommand) => new StartTommand(typermediaTommand);

   class SelectTuery<TPrevious> : NavigationSpecification<TResult>
   {
      readonly NavigationSpecification<TPrevious> _previous;
      readonly Func<TPrevious, TResult> _select;

      internal SelectTuery(NavigationSpecification<TPrevious> previous, Func<TPrevious, TResult> select)
      {
         _previous = previous;
         _select = select;
      }

      internal override async Task<TResult> NavigateOnAsync(IRemoteTypermediaNavigator busSession)
      {
         var previousResult = await _previous.NavigateOnAsync(busSession).caf();
         return _select(previousResult);
      }
   }

   class StartTuery : NavigationSpecification<TResult>
   {
      readonly IRemotableTuery<TResult> _start;

      internal StartTuery(IRemotableTuery<TResult> start) => _start = start;

      internal override async Task<TResult> NavigateOnAsync(IRemoteTypermediaNavigator busSession) => await busSession.GetAsync(_start).caf();
   }

   class StartTommand : NavigationSpecification<TResult>
   {
      readonly IAtMostOnceTypermediaTommand<TResult> _start;

      internal StartTommand(IAtMostOnceTypermediaTommand<TResult> start) => _start = start;

      internal override async Task<TResult> NavigateOnAsync(IRemoteTypermediaNavigator busSession) => await busSession.PostAsync(_start).caf();
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

      internal override async Task<TResult> NavigateOnAsync(IRemoteTypermediaNavigator busSession)
      {
         var previousResult = await _previous.NavigateOnAsync(busSession).caf();
         var currentTuery = _nextTuery(previousResult);
         return await busSession.GetAsync(currentTuery).caf();
      }
   }

   class PostTommand<TPrevious> : NavigationSpecification<TResult>
   {
      readonly NavigationSpecification<TPrevious> _previous;
      readonly Func<TPrevious, IAtMostOnceTypermediaTommand<TResult>> _next;
      internal PostTommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, IAtMostOnceTypermediaTommand<TResult>> next)
      {
         _previous = previous;
         _next = next;
      }

      internal override async Task<TResult> NavigateOnAsync(IRemoteTypermediaNavigator busSession)
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

      protected override async Task NavigateOnAsync(IRemoteTypermediaNavigator busSession)
      {
         var previousResult = await _previous.NavigateOnAsync(busSession).caf();
         var currentTommand = _next(previousResult);
         await busSession.PostAsync(currentTommand).caf();
      }
   }
}
