using System;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Public;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Abstractions.Tessaging.Typermedia.Public;

public abstract class NavigationSpecification
{
   public static NavigationSpecification Post(IAtMostOnceHypermediaTommand tommand) => new VoidCommand(tommand);

   public static NavigationSpecification<TResult> Get<TResult>(IRemotableTuery<TResult> tuery) => NavigationSpecification<TResult>.Get(tuery);
   public static NavigationSpecification<TResult> Post<TResult>(IAtMostOnceTommand<TResult> tommand) => NavigationSpecification<TResult>.Post(tommand);

   public void NavigateOn(IRemoteHypermediaNavigator busSession) => NavigateOnAsync(busSession).WaitUnwrappingException();
   public abstract Task NavigateOnAsync(IRemoteHypermediaNavigator busSession);

   class VoidCommand(IAtMostOnceHypermediaTommand tommand) : NavigationSpecification
   {
      readonly IAtMostOnceHypermediaTommand _tommand = tommand;

      public override async Task NavigateOnAsync(IRemoteHypermediaNavigator busSession) => await busSession.PostAsync(_tommand).caf();
   }
}

public abstract class NavigationSpecification<TResult>
{
   public TResult NavigateOn(IRemoteHypermediaNavigator busSession) => NavigateOnAsync(busSession).ResultUnwrappingException();
   public abstract Task<TResult> NavigateOnAsync(IRemoteHypermediaNavigator busSession);

   public NavigationSpecification<TNext> Select<TNext>(Func<TResult, TNext> select) => new NavigationSpecification<TNext>.SelectQuery<TResult>(this, select);

   public NavigationSpecification Post(Func<TResult, IAtMostOnceHypermediaTommand> next) => new PostVoidCommand<TResult>(this, next);
   public NavigationSpecification<TNext> Get<TNext>(Func<TResult, IRemotableTuery<TNext>> next) => new NavigationSpecification<TNext>.ContinuationQuery<TResult>(this, next);
   public NavigationSpecification<TNext> Post<TNext>(Func<TResult, IAtMostOnceTommand<TNext>> next) => new NavigationSpecification<TNext>.PostCommand<TResult>(this, next);

   internal static NavigationSpecification<TResult> Get(IRemotableTuery<TResult> tuery) => new StartQuery(tuery);
   internal static NavigationSpecification<TResult> Post(IAtMostOnceTommand<TResult> tommand) => new StartCommand(tommand);

   class SelectQuery<TPrevious> : NavigationSpecification<TResult>
   {
      readonly NavigationSpecification<TPrevious> _previous;
      readonly Func<TPrevious, TResult> _select;

      internal SelectQuery(NavigationSpecification<TPrevious> previous, Func<TPrevious, TResult> select)
      {
         _previous = previous;
         _select = select;
      }

      public override async Task<TResult> NavigateOnAsync(IRemoteHypermediaNavigator busSession)
      {
         var previousResult = await _previous.NavigateOnAsync(busSession).caf();
         return _select(previousResult);
      }
   }

   class StartQuery : NavigationSpecification<TResult>
   {
      readonly IRemotableTuery<TResult> _start;

      internal StartQuery(IRemotableTuery<TResult> start) => _start = start;

      public override async Task<TResult> NavigateOnAsync(IRemoteHypermediaNavigator busSession) => await busSession.GetAsync(_start).caf();
   }

   class StartCommand : NavigationSpecification<TResult>
   {
      readonly IAtMostOnceTommand<TResult> _start;

      internal StartCommand(IAtMostOnceTommand<TResult> start) => _start = start;

      public override async Task<TResult> NavigateOnAsync(IRemoteHypermediaNavigator busSession) => await busSession.PostAsync(_start).caf();
   }

   class ContinuationQuery<TPrevious> : NavigationSpecification<TResult>
   {
      readonly NavigationSpecification<TPrevious> _previous;
      readonly Func<TPrevious, IRemotableTuery<TResult>> _nextQuery;

      internal ContinuationQuery(NavigationSpecification<TPrevious> previous, Func<TPrevious, IRemotableTuery<TResult>> nextQuery)
      {
         _previous = previous;
         _nextQuery = nextQuery;
      }

      public override async Task<TResult> NavigateOnAsync(IRemoteHypermediaNavigator busSession)
      {
         var previousResult = await _previous.NavigateOnAsync(busSession).caf();
         var currentQuery = _nextQuery(previousResult);
         return await busSession.GetAsync(currentQuery).caf();
      }
   }

   class PostCommand<TPrevious> : NavigationSpecification<TResult>
   {
      readonly NavigationSpecification<TPrevious> _previous;
      readonly Func<TPrevious, IAtMostOnceTommand<TResult>> _next;
      internal PostCommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, IAtMostOnceTommand<TResult>> next)
      {
         _previous = previous;
         _next = next;
      }

      public override async Task<TResult> NavigateOnAsync(IRemoteHypermediaNavigator busSession)
      {
         var previousResult = await _previous.NavigateOnAsync(busSession).caf();
         var currentCommand = _next(previousResult);
         return await busSession.PostAsync(currentCommand).caf();
      }
   }

   class PostVoidCommand<TPrevious> : NavigationSpecification
   {
      readonly NavigationSpecification<TPrevious> _previous;
      readonly Func<TPrevious, IAtMostOnceHypermediaTommand> _next;
      internal PostVoidCommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, IAtMostOnceHypermediaTommand> next)
      {
         _previous = previous;
         _next = next;
      }

      public override async Task NavigateOnAsync(IRemoteHypermediaNavigator busSession)
      {
         var previousResult = await _previous.NavigateOnAsync(busSession).caf();
         var currentCommand = _next(previousResult);
         await busSession.PostAsync(currentCommand).caf();
      }
   }
}