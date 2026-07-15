using Compze.Internals.SystemCE.DiagnosticsCE;

namespace Compze.Hosting.SameMachine;

///<summary>The OS process that announced an endpoint address into an <see cref="InterprocessEndpointRegistry"/>.<br/>
/// The registry records it so that a crashed announcer's stale addresses are recognized as dead and never routed to.</summary>
///<remarks>Recognizing a process across process boundaries — and telling whether it is still running — is a<br/>
/// <see cref="ProcessIdentity"/> concern. This type is the hosting-domain name for "the process that announced",<br/>
/// wrapping the <see cref="Identity"/> that does that work.</remarks>
public class AnnouncingProcess
{
   ///<summary>The process this code runs in — what a process announcing its own endpoints' addresses passes.</summary>
   public static AnnouncingProcess Current => new(ProcessIdentity.OfCurrentProcess);

   ///<summary>Identifies the announcing process across process boundaries and answers whether it is still running.</summary>
   public ProcessIdentity Identity { get; }

   public AnnouncingProcess(ProcessIdentity identity) => Identity = identity;

   ///<summary>Reconstructs an announcing process from its serialized <paramref name="processId"/> and UTC-tick <paramref name="startTimeTicks"/>.</summary>
   public AnnouncingProcess(int processId, long startTimeTicks)
      : this(new ProcessIdentity(processId, new DateTime(startTimeTicks, DateTimeKind.Utc))) { }

   ///<summary>True while the process that announced is still running — the liveness filter that hides a crashed announcer's addresses.</summary>
   public bool IsStillRunning => Identity.IsCurrentlyRunning;
}
