using Compze.Contracts;

namespace Compze.Threading.Interprocess._private.ProcessLocks;

///<summary>The assertions on the arguments of what is made of lock files in a directory, such as an <see cref="IProcessLock"/>: its name, its directory,<br/>
/// and the purpose a take of it gives. <c>kind</c> names what is made, such as "process lock", in what an assertion that fails says.</summary>
static class ProcessLockArguments
{
   internal static void AssertIsAName(string name, string kind) =>
      Argument.Assert(IsAFileNameWithNoPathSeparator(name), () => $@"A {kind}'s name must be a file name, with neither '/' nor '\' in it: '{name}'.");

   internal static void AssertExists(DirectoryInfo directory, string name, string kind) =>
      Argument.Assert(Directory.Exists(directory.FullName), () => $"The directory of the {kind} '{name}' must exist: {directory.FullName}.");

   internal static void AssertIsAPurpose(string purpose, string kind) =>
      Argument.Assert(!string.IsNullOrWhiteSpace(purpose), () => $"Taking a {kind} takes a purpose: what it is to be held for, to those who wait for it meanwhile.");

   ///<summary>True if <paramref name="name"/> can name a file on the system this runs on, and has neither Windows' path separator nor Unix's in it,<br/>
   /// which would make it a path on one system or the other.</summary>
   static bool IsAFileNameWithNoPathSeparator(string name) => !string.IsNullOrWhiteSpace(name) && name.IndexOfAny([..Path.GetInvalidFileNameChars(), '\\', '/']) < 0;
}
