using System.Threading.Tasks;
using Compze.Core.Tessaging.Public;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Core.Tessaging.Typermedia.Public;

public static class NavigationSpecificationTessageExtensions
{
   public static NavigationSpecification Post(this IAtMostOnceTypermediaTommand tommand) => NavigationSpecification.Post(tommand);

   public static NavigationSpecification<TResult> Post<TResult>(this IAtMostOnceTommand<TResult> typermediaTommand) => NavigationSpecification.Post(typermediaTommand);

   public static NavigationSpecification<TResult> Get<TResult>(this IRemotableTuery<TResult> tuery) => NavigationSpecification.Get(tuery);


   public static TResult PostOn<TResult>(this IAtMostOnceTommand<TResult> typermediaTommand, IRemoteTypermediaNavigator bus) => NavigationSpecification.Post(typermediaTommand).NavigateOn(bus);

   public static TResult GetOn<TResult>(this IRemotableTuery<TResult> tuery, IRemoteTypermediaNavigator bus) => NavigationSpecification.Get(tuery).NavigateOn(bus);

   public static TResult Navigate<TResult>(this IRemoteTypermediaNavigator navigator, NavigationSpecification<TResult> navigationSpecification) => navigationSpecification.NavigateOn(navigator);

   public static async Task<TResult> NavigateAsync<TResult>(this IRemoteTypermediaNavigator navigator, NavigationSpecification<TResult> navigationSpecification) => await navigationSpecification.NavigateOnAsync(navigator).caf();
}