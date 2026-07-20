using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Typermedia;

public static class NavigationSpecificationTessageExtensions
{
   public static NavigationSpecification Post(this IAtMostOnceTypermediaTommand tommand) => NavigationSpecification.Post(tommand);

   public static NavigationSpecification<TResult> Post<TResult>(this IAtMostOnceTypermediaTommand<TResult> typermediaTommand) => NavigationSpecification.Post(typermediaTommand);

   public static NavigationSpecification<TResult> Get<TResult>(this IRemotableTuery<TResult> tuery) => NavigationSpecification.Get(tuery);


   public static TResult PostOn<TResult>(this IAtMostOnceTypermediaTommand<TResult> typermediaTommand, IRemoteTypermediaNavigator bus) => NavigationSpecification.Post(typermediaTommand).NavigateOn(bus);

   public static TResult GetOn<TResult>(this IRemotableTuery<TResult> tuery, IRemoteTypermediaNavigator bus) => NavigationSpecification.Get(tuery).NavigateOn(bus);

   extension(IRemoteTypermediaNavigator navigator)
   {
      public TResult Navigate<TResult>(NavigationSpecification<TResult> navigationSpecification) => navigationSpecification.NavigateOn(navigator);
      public async Task<TResult> NavigateAsync<TResult>(NavigationSpecification<TResult> navigationSpecification) => await navigationSpecification.NavigateOnAsync(navigator).caf();
   }
}
