using Compze.Must;
using Compze.Threading.Interprocess;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.IProcessLock_;

public class Making_a_process_lock : ProcessLockSpecification
{
   [XF] public void named_with_a_forward_slash_in_the_name_throws_an_ArgumentException() =>
      Invoking(() => IProcessLock.New("a/lock", LockDirectory)).Must().Throw<ArgumentException>();

   [XF] public void named_with_a_backslash_in_the_name_throws_an_ArgumentException() =>
      Invoking(() => IProcessLock.New(@"a\lock", LockDirectory)).Must().Throw<ArgumentException>();

   [XF] public void named_with_only_whitespace_throws_an_ArgumentException() =>
      Invoking(() => IProcessLock.New(" ", LockDirectory)).Must().Throw<ArgumentException>();

   [XF] public void in_a_directory_that_does_not_exist_throws_an_ArgumentException() =>
      Invoking(() => IProcessLock.New(LockName, new DirectoryInfo(Path.Combine(LockDirectory.FullName, Guid.NewGuid().ToString())))).Must().Throw<ArgumentException>();
}
