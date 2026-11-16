using Compze.Must;
using Compze.Teventive.Taggregates;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.SnapshotRoundTrips;

///<summary><see cref="SnapshotRoundTrip.AssertLosesNothing{TTaggregate,TSnapshot}"/> on a book with state in itself, in a component and in a<br/>
/// shared tomponent: it passes when a copy made through a snapshot holds the book's whole state, and names each difference when it doesn't.</summary>
public abstract class Checking_that_a_book_loses_nothing_through_its_snapshot
{
   readonly BookServices _services;
   readonly Book _book;
   readonly Func<BookSnapshot, BookSnapshot, bool>? _snapshotsAreEqual;

   private protected Checking_that_a_book_loses_nothing_through_its_snapshot(SnapshotHandling handling) : this(handling, BookSnapshot.HoldTheSameState) {}

   private protected Checking_that_a_book_loses_nothing_through_its_snapshot(SnapshotHandling handling, Func<BookSnapshot, BookSnapshot, bool>? snapshotsAreEqual)
   {
      _services = new BookServices(handling);
      _book = ABookWithStateEverywhere(_services);
      _snapshotsAreEqual = snapshotsAreEqual;
   }

   void CheckTheBook() => SnapshotRoundTrip.AssertLosesNothing(_book, () => new Book(_services), _snapshotsAreEqual);

   IReadOnlyList<string> DifferencesOfTheBook => SnapshotRoundTrip.DifferencesOf(_book, () => new Book(_services), _snapshotsAreEqual);

   string FailureOfTheCheck => Invoking(CheckTheBook).Must().Throw<Exception>().Which.Message;

   public class when_the_book_takes_and_restores_its_snapshot_correctly() : Checking_that_a_book_loses_nothing_through_its_snapshot(SnapshotHandling.Correctly)
   {
      [XF] public void the_check_passes_though_only_the_original_has_created_its_heading_by_reading_it() => CheckTheBook();
      [XF] public void no_differences_are_listed() => DifferencesOfTheBook.Must().BeEmpty();
   }

   public class when_restoring_adds_the_tags_of_its_shared_tomponent_in_reverse() : Checking_that_a_book_loses_nothing_through_its_snapshot(SnapshotHandling.AddingTheTagsInReverseWhenRestoring)
   {
      [XF] public void the_check_passes_since_a_set_has_no_order() => CheckTheBook();
   }

   public class when_taking_its_snapshot_leaves_out_its_author() : Checking_that_a_book_loses_nothing_through_its_snapshot(SnapshotHandling.LeavingOutTheAuthorWhenTakingIt)
   {
      [XF] public void the_check_fails_naming_the_author_with_its_value_in_the_original_and_in_the_copy() =>
         FailureOfTheCheck.Must().Contain("Book.Author: \"The author\" in the original, null in the copy");

      [XF] public void the_check_fails_naming_the_heading_derived_from_the_author_too() =>
         FailureOfTheCheck.Must().Contain("Book._heading: \"The title, by The author\" in the original, \"The title, by \" in the copy");
   }

   public class when_restoring_leaves_out_the_chapters_of_its_component() : Checking_that_a_book_loses_nothing_through_its_snapshot(SnapshotHandling.LeavingOutTheChaptersWhenRestoring)
   {
      [XF] public void the_check_fails_naming_the_chapters_and_how_many_items_each_holds() =>
         FailureOfTheCheck.Must().Contain("Book.Chapters._chapters: 2 items in the original, 0 in the copy");
   }

   public class when_restoring_reverses_the_chapters_of_its_component() : Checking_that_a_book_loses_nothing_through_its_snapshot(SnapshotHandling.ReversingTheChaptersWhenRestoring)
   {
      [XF] public void the_check_fails_naming_each_chapter_out_of_place() =>
         FailureOfTheCheck.Must().Contain("Book.Chapters._chapters[0]: \"first\" in the original, \"second\" in the copy")
                          .Contain("Book.Chapters._chapters[1]: \"second\" in the original, \"first\" in the copy");

      [XF] public void the_differences_listed_are_each_chapter_out_of_place_and_the_snapshots_differing() =>
         DifferencesOfTheBook.Must().SequenceEqual([
            "Book.Chapters._chapters[0]: \"first\" in the original, \"second\" in the copy",
            "Book.Chapters._chapters[1]: \"second\" in the original, \"first\" in the copy",
            "A snapshot of the copy doesn't equal a snapshot of the original, by the comparison of its snapshots given, so a state store's reload would take every Book for changed."
         ]);
   }

   public class when_restoring_leaves_out_the_tags_of_its_shared_tomponent() : Checking_that_a_book_loses_nothing_through_its_snapshot(SnapshotHandling.LeavingOutTheTagsWhenRestoring)
   {
      [XF] public void the_check_fails_naming_the_tags_and_how_many_items_each_holds() =>
         FailureOfTheCheck.Must().Contain("Book.Tags._tags: 3 items in the original, 0 in the copy");
   }

   public class when_restoring_adds_a_star_to_each_rating() : Checking_that_a_book_loses_nothing_through_its_snapshot(SnapshotHandling.AddingAStarToEachRatingWhenRestoring)
   {
      [XF] public void the_check_fails_naming_each_rating_the_copy_lacks() =>
         FailureOfTheCheck.Must().Contain("Book._starsByReader: the original holds [reader one, 4], which the copy doesn't")
                          .Contain("Book._starsByReader: the original holds [reader two, 5], which the copy doesn't");
   }

   public class when_restoring_adds_the_tags_in_reverse_and_the_comparison_of_its_snapshots_compares_the_tags_in_order()
      : Checking_that_a_book_loses_nothing_through_its_snapshot(SnapshotHandling.AddingTheTagsInReverseWhenRestoring, BookSnapshot.HoldTheSameStateComparingTheTagsInOrder)
   {
      [XF] public void the_check_fails_saying_that_a_reload_would_take_every_book_for_changed() =>
         FailureOfTheCheck.Must().Contain("A snapshot of the copy doesn't equal a snapshot of the original, by the comparison of its snapshots given, so a state store's reload would take every Book for changed.");
   }

   public class when_no_comparison_of_its_snapshots_is_given_and_the_snapshot_type_does_not_override_Equals()
      : Checking_that_a_book_loses_nothing_through_its_snapshot(SnapshotHandling.Correctly, snapshotsAreEqual: null)
   {
      [XF] public void the_check_fails_saying_so() =>
         FailureOfTheCheck.Must().Contain($"{typeof(BookSnapshot).FullName}, the snapshot type of {typeof(Book).FullName}, doesn't override Equals");
   }

   ///<summary>A book with state in itself, in its component and in its shared tomponent, whose heading has been read, so that only<br/>
   /// the original has created it.</summary>
   static Book ABookWithStateEverywhere(BookServices services)
   {
      var book = Book.Create(services, "The title", "The author");
      _ = book.Heading;
      book.Rate("reader one", 4);
      book.Rate("reader two", 5);
      book.Chapters.Add("first");
      book.Chapters.Add("second");
      book.Tags.Add("fiction");
      book.Tags.Add("classic");
      book.Tags.Add("long");
      return book;
   }
}
