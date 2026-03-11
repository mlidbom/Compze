namespace Compze.Threading.Testing;

///<summary>A block of code with <see cref="IThreadGate"/>s for <see cref="EntranceGate"/> and <see cref="ExitGate"/>. Useful for controlling multithreaded code for testing purposes.</summary>
public partial interface IGatedCodeSection
{
   ///<summary>Returns a new Closed <see cref="IGatedCodeSection"/> with both gates closed.</summary>
   static IGatedCodeSection NewClosed(WaitTimeout timeout, string name) => new Implementation(timeout, name);
   ///<summary>Returns a new Open <see cref="IGatedCodeSection"/> with both gates open.</summary>
   static IGatedCodeSection NewOpen(WaitTimeout timeout, string name) => NewClosed(timeout, name).Open();

   ///<summary>The <see cref="IThreadGate"/> that threads must pass through to enter the gated code section.</summary>
   IThreadGate EntranceGate { get; }
   ///<summary>The <see cref="IThreadGate"/> that threads must pass through to exit the gated code section.</summary>
   IThreadGate ExitGate { get; }

   ///<summary>Passes through <see cref="EntranceGate"/>, executes <paramref name="func"/>, then passes through <see cref="ExitGate"/> before returning the result.</summary>
   TReturn Execute<TReturn>(Func<TReturn> func);

   ///<summary>Executes <paramref name="action"/> while holding the shared lock that guards both gates.</summary>
   TReturn ExecuteWithExclusiveLock<TReturn>(Func<IGatedCodeSection, TReturn> action);
}
