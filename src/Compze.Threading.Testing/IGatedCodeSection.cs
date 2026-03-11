namespace Compze.Threading.Testing;

///<summary>A block of code with <see cref="IThreadGate"/>s for <see cref="EntranceGate"/> and <see cref="ExitGate"/>. Useful for controlling multithreaded code for testing purposes.</summary>
public partial interface IGatedCodeSection
{
   static IGatedCodeSection NewClosed(WaitTimeout timeout, string name) => new Implementation(timeout, name);
   static IGatedCodeSection NewOpen(WaitTimeout timeout, string name) => NewClosed(timeout, name).Open();

   IThreadGate EntranceGate { get; }
   IThreadGate ExitGate { get; }

   TReturn Execute<TReturn>(Func<TReturn> func);

   ///<summary>Executes <paramref name="action"/> while holding the shared lock that guards both gates.</summary>
   TReturn ExecuteWithExclusiveLock<TReturn>(Func<IGatedCodeSection, TReturn> action);
}
