using System.Threading.Tasks;
using Compze.Core.Tessaging.Public;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Core.Tessaging.Typermedia.Public;

public static class NavigationSpecificationTessageExtensions
{
   public static NavigationSpecification Post(this IAtMostOnceHypermediaTommand tommand) => NavigationSpecification.Post(tommand);

   public static NavigationSpecification<TResult> Post<TResult>(this IAtMostOnceTommand<TResult> tommand) => NavigationSpecification.Post(tommand);

   public static NavigationSpecification<TResult> Get<TResult>(this IRemotableTuery<TResult> tuery) => NavigationSpecification.Get(tuery);


   public static TResult PostOn<TResult>(this IAtMostOnceTommand<TResult> tommand, IRemoteHypermediaNavigator bus) => NavigationSpecification.Post(tommand).NavigateOn(bus);

   public static TResult GetOn<TResult>(this IRemotableTuery<TResult> tuery, IRemoteHypermediaNavigator bus) => NavigationSpecification.Get(tuery).NavigateOn(bus);

   public static TResult Navigate<TResult>(this IRemoteHypermediaNavigator navigator, NavigationSpecification<TResult> navigationSpecification) => navigationSpecification.NavigateOn(navigator);

   public static async Task<TResult> NavigateAsync<TResult>(this IRemoteHypermediaNavigator navigator, NavigationSpecification<TResult> navigationSpecification) => await navigationSpecification.NavigateOnAsync(navigator).caf();
}