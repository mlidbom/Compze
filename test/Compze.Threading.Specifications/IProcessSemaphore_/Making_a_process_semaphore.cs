using Compze.Must;
using Compze.Threading.Interprocess;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.IProcessSemaphore_;

public class Making_a_process_semaphore : ProcessSemaphoreSpecification
{
   [XF] public void named_with_a_forward_slash_in_the_name_throws_an_ArgumentException() =>
      Invoking(() => IProcessSemaphore.New("a/semaphore", UnitsOfTheSemaphore, SemaphoreDirectory)).Must().Throw<ArgumentException>();

   [XF] public void named_with_a_backslash_in_the_name_throws_an_ArgumentException() =>
      Invoking(() => IProcessSemaphore.New(@"a\semaphore", UnitsOfTheSemaphore, SemaphoreDirectory)).Must().Throw<ArgumentException>();

   [XF] public void named_with_only_whitespace_throws_an_ArgumentException() =>
      Invoking(() => IProcessSemaphore.New(" ", UnitsOfTheSemaphore, SemaphoreDirectory)).Must().Throw<ArgumentException>();

   [XF] public void in_a_directory_that_does_not_exist_throws_an_ArgumentException() =>
      Invoking(() => IProcessSemaphore.New(SemaphoreName, UnitsOfTheSemaphore, new DirectoryInfo(Path.Combine(SemaphoreDirectory.FullName, Guid.NewGuid().ToString())))).Must().Throw<ArgumentException>();

   [XF] public void with_no_units_throws_an_ArgumentException() =>
      Invoking(() => IProcessSemaphore.New(SemaphoreName, 0, SemaphoreDirectory)).Must().Throw<ArgumentException>();

   [XF] public void it_has_the_units_it_was_made_with() => Semaphore.Units.Must().Be(UnitsOfTheSemaphore);
}
