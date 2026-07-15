using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Must;

using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Typermedia.Specifications;

///<summary>Specifies how a <see cref="NavigationSpecification"/> composes navigation steps: each step receives the previous step's result and the chain executes against whichever navigator it is run on.</summary>
public class NavigationSpecification_specification
{
   readonly HandlingNavigator _navigator = new();

   [XF] public void Get_returns_the_navigators_answer_to_the_tuery() =>
      NavigationSpecification.Get(new TheAnswerTuery())
                             .NavigateOn(_navigator)
                             .Must().Be(42);

   [XF] public void Select_transforms_the_previous_steps_result() =>
      NavigationSpecification.Get(new TheAnswerTuery())
                             .Select(answer => answer * 2)
                             .NavigateOn(_navigator)
                             .Must().Be(84);

   [XF] public void a_chained_Get_builds_the_next_tuery_from_the_previous_steps_result() =>
      NavigationSpecification.Get(new TheAnswerTuery())
                             .Get(answer => new AddTuery { Left = answer, Right = 8 })
                             .NavigateOn(_navigator)
                             .Must().Be(50);

   [XF] public void a_chained_Post_posts_the_tommand_built_from_the_previous_steps_result()
   {
      NavigationSpecification.Get(new TheAnswerTuery())
                             .Post(answer => RememberNumberTommand.Create(answer))
                             .NavigateOn(_navigator);

      ((RememberNumberTommand)_navigator.PostedTommands.Single()).Number.Must().Be(42);
   }

   class TheAnswerTuery : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<int>;

   class AddTuery : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<int>
   {
      public int Left { get; set; }
      public int Right { get; set; }
   }

   class RememberNumberTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand
   {
      RememberNumberTommand() {}
      public static RememberNumberTommand Create(int number) => new() { Id = new TessageId(), Number = number };
      public int Number { get; private init; }
   }

   ///<summary>Stands in for the remote side: answers the spec's tueries and remembers the tommands posted to it.</summary>
   class HandlingNavigator : IRemoteTypermediaNavigator
   {
      public List<IAtMostOnceTypermediaTommand> PostedTommands { get; } = [];

      public Task PostAsync(IAtMostOnceTypermediaTommand tommand)
      {
         PostedTommands.Add(tommand);
         return Task.CompletedTask;
      }

      public void Post(IAtMostOnceTypermediaTommand tommand) => PostedTommands.Add(tommand);

      public Task<TResult> PostAsync<TResult>(IAtMostOnceTypermediaTommand<TResult> typermediaTommand) => Task.FromResult(Post(typermediaTommand));
      public TResult Post<TResult>(IAtMostOnceTypermediaTommand<TResult> typermediaTommand) => throw new NotSupportedException($"No spec posts {typermediaTommand.GetType().Name}");

      public Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery) => Task.FromResult(Get(tuery));

      public TResult Get<TResult>(IRemotableTuery<TResult> tuery) => tuery switch
      {
         TheAnswerTuery => (TResult)(object)42,
         AddTuery add   => (TResult)(object)(add.Left + add.Right),
         _              => throw new NotSupportedException($"No spec gets {tuery.GetType().Name}")
      };
   }
}
