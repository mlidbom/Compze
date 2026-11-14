using System.Text.Json;
using System.Text.Json.Nodes;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Compze.Must.Specifications;

public class When_a_failing_assertion_shows_a_value_Must_cannot_fully_serialize : UniversalTestBase
{
   static string MessageOf(Action failingAssertion) => Invoking(failingAssertion).Must().Throw<AssertionFailedException>().Which.Message;

   public class that_is_a_value_tuple : When_a_failing_assertion_shows_a_value_Must_cannot_fully_serialize
   {
      readonly (long Id, string Text) _actual = (1, "one");

      [XF] public void the_message_shows_the_items_of_the_tuple() =>
         MessageOf(() => _actual.Must().Be((2L, "two"))).Must().Contain("\"Item2\": \"one\"");
   }

   public class that_is_a_JsonElement : When_a_failing_assertion_shows_a_value_Must_cannot_fully_serialize
   {
      readonly JsonElement _actual = JsonElement.Parse("""{"name":"value"}""");

      [XF] public void the_message_is_the_assertions_own() =>
         MessageOf(() => _actual.Must().Satisfy(_ => false)).Must().Contain("_actual.Must().Satisfy(_ => false)");
   }

   public class that_is_a_JsonObject : When_a_failing_assertion_shows_a_value_Must_cannot_fully_serialize
   {
      readonly JsonNode _actual = JsonNode.Parse("""{"name":"value"}""")!;

      [XF] public void the_message_is_the_assertions_own() =>
         MessageOf(() => _actual.Must().Satisfy(_ => false)).Must().Contain("_actual.Must().Satisfy(_ => false)");
   }

   public class with_a_getter_that_throws : When_a_failing_assertion_shows_a_value_Must_cannot_fully_serialize
   {
      readonly HasAGetterThatThrows _actual = new();

      [XF] public void the_message_is_the_assertions_own_and_says_what_failed_and_how_to_leave_it_out() =>
         MessageOf(() => _actual.Must().Satisfy(it => it.Name == "other")).Must().Be(
            """

            --------------------------------------------------
            Failing assertion:
            --------------------------------------------------
            _actual.Must().Satisfy(it => it.Name == "other")
            --------------------------------------------------
            _actual was a Compze.Must.Specifications.When_a_failing_assertion_shows_a_value_Must_cannot_fully_serialize.HasAGetterThatThrows that Must could not serialize:
            --------------------------------------------------
            System.InvalidOperationException: Not computed yet
            while reading Compze.Must.Specifications.When_a_failing_assertion_shows_a_value_Must_cannot_fully_serialize.HasAGetterThatThrows.Computed, at the path: Computed
            To leave it out of this assertion, configure the Must call:
               .Must(serialization: it => it.ExcludeMember(Type.GetType("Compze.Must.Specifications.When_a_failing_assertion_shows_a_value_Must_cannot_fully_serialize+HasAGetterThatThrows, Compze.Must.Specifications")!, "Computed"))
            To leave it out of every assertion, once when the test assembly starts:
               MustSerialization.ExcludeMember(Type.GetType("Compze.Must.Specifications.When_a_failing_assertion_shows_a_value_Must_cannot_fully_serialize+HasAGetterThatThrows, Compze.Must.Specifications")!, "Computed");
            --------------------------------------------------
            ToString():
            --------------------------------------------------
            Compze.Must.Specifications.When_a_failing_assertion_shows_a_value_Must_cannot_fully_serialize+HasAGetterThatThrows
            --------------------------------------------------
            """);

      [XF] public void DeepEqual_fails_instead_of_comparing_values_that_are_equal() =>
         MessageOf(() => _actual.Must().DeepEqual(new HasAGetterThatThrows())).Must().Contain("DeepEqual could not compare the values, because Must could not serialize them both.");

      public class and_the_Must_call_excludes_the_member : with_a_getter_that_throws
      {
         [XF] public void the_message_shows_the_rest_of_the_value() =>
            MessageOf(() => _actual.Must(serialization: it => it.ExcludeMember(typeof(HasAGetterThatThrows), nameof(HasAGetterThatThrows.Computed)))
                                   .Satisfy(it => it.Name == "other"))
              .Must().Contain("\"Name\": \"name\"");

         [XF] public void DeepEqual_compares_the_rest_of_the_values() =>
            _actual.Must(serialization: it => it.ExcludeMember(typeof(HasAGetterThatThrows), nameof(HasAGetterThatThrows.Computed)))
                   .DeepEqual(new HasAGetterThatThrows());
      }

      public class and_the_Must_call_excludes_the_type : with_a_getter_that_throws
      {
         [XF] public void the_message_shows_a_marker_in_its_place() =>
            MessageOf(() => _actual.Must(serialization: it => it.ExcludeType<HasAGetterThatThrows>()).Satisfy(it => it.Name == "other"))
              .Must().Contain($"(excluded {typeof(HasAGetterThatThrows).FullName!.Replace('+', '.')})");
      }
   }

   public class with_a_member_the_Must_call_serializes_as_another_value : When_a_failing_assertion_shows_a_value_Must_cannot_fully_serialize
   {
      readonly HasAGetterThatThrows _actual = new();

      [XF] public void the_message_shows_the_other_value() =>
         MessageOf(() => _actual.Must(serialization: it => it.SerializeAs<HasAGetterThatThrows>(value => $"the one named {value.Name}")).Satisfy(_ => false))
           .Must().Contain("\"the one named name\"");
   }

   public class of_a_type_MustSerialization_excludes_for_every_assertion : When_a_failing_assertion_shows_a_value_Must_cannot_fully_serialize
   {
      static of_a_type_MustSerialization_excludes_for_every_assertion() => MustSerialization.ExcludeType<ExcludedForEveryAssertion>();

      readonly ContainsAValueExcludedForEveryAssertion _actual = new();

      [XF] public void the_message_shows_a_marker_in_its_place() =>
         MessageOf(() => _actual.Must().Satisfy(_ => false)).Must().Contain($"(excluded {typeof(ExcludedForEveryAssertion).FullName!.Replace('+', '.')})");
   }

   public class too_long_to_show_in_full : When_a_failing_assertion_shows_a_value_Must_cannot_fully_serialize
   {
      readonly List<string> _actual = [..1.Through(10_000).Select(it => $"the item numbered {it}")];

      string Message() => MessageOf(() => _actual.Must().Satisfy(_ => false));

      [XF] public void the_message_says_where_all_of_it_is() => Message().Must().Contain("... shortened: all ");

      [XF] public void the_file_holds_all_of_it() =>
         File.ReadAllText(PathOfTheFileIn(Message())).Must().Contain("\"the item numbered 10000\"");

      static string PathOfTheFileIn(string message)
      {
         var lines = message.Split(Environment.NewLine);
         return lines[Array.FindIndex(lines, it => it.StartsWith("... shortened: all ", StringComparison.Ordinal)) + 1];
      }
   }

   class HasAGetterThatThrows
   {
      public string Name { get; } = "name";
      public string Computed => throw new InvalidOperationException("Not computed yet");
   }

   class ExcludedForEveryAssertion
   {
      public string Secret { get; } = "secret";
   }

   class ContainsAValueExcludedForEveryAssertion
   {
      public ExcludedForEveryAssertion Excluded { get; } = new();
   }
}
