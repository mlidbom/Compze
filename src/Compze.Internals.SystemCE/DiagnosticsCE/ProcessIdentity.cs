using System.Diagnostics;

namespace Compze.Internals.SystemCE.DiagnosticsCE;

///<summary>Identifies a specific OS process well enough that any process on the machine — not just the one being<br/>
/// identified — can recognize it later and tell whether it is still running. The identity is the process id paired<br/>
/// with the process's start time, because the OS recycles process ids: only the pair distinguishes a running process<br/>
/// from a different process that has since been given the same, recycled id.</summary>
///<remarks>Liveness tolerates a small difference in start time rather than demanding exact equality, because<br/>
/// <see cref="Process.StartTime"/> is not read-stable across processes on every OS — see<br/>
/// <see cref="StartTimeReaderSkewTolerance"/>. Exact tick-equality is therefore the wrong test, the same way<br/>
/// <c>==</c> is the wrong test for two floating-point measurements.</remarks>
///<remarks>On Windows <see cref="Process.StartTime"/> is the kernel's absolute process-creation time, identical for<br/>
/// every reader. On Unix it is reconstructed as <c>bootTime + timeSinceBoot</c>, where <c>bootTime</c> is each<br/>
/// process's own <c>now - uptime</c> sample taken on a coarse clock and cached per process. Two processes reading the<br/>
/// same target's start time therefore get values that differ by their sampling skew — enough to break exact equality,<br/>
/// which is exactly what <see cref="StartTimeReaderSkewTolerance"/> absorbs.</remarks>
public sealed class ProcessIdentity : IEquatable<ProcessIdentity>
{
   ///<summary>The identity of the process this code is running in — what a process identifying itself passes.</summary>
   public static ProcessIdentity OfCurrentProcess
   {
      get
      {
         using var currentProcess = Process.GetCurrentProcess();
         return new ProcessIdentity(currentProcess.Id, currentProcess.StartTime);
      }
   }

   public int ProcessId { get; }

   ///<summary>The identified process's start time, in UTC — the disambiguator that detects process id reuse.</summary>
   public DateTime StartTime { get; }

   public ProcessIdentity(int processId, DateTime startTime)
   {
      ProcessId = processId;
      StartTime = startTime.ToUniversalTimeSafely();
   }

   ///<summary>True if the process this identifies is still running.</summary>
   public bool IsCurrentlyRunning => ProcessCurrentlyHavingMyId() == this;

   ///<summary>The identity of whatever process currently holds this identity's id, or <c>null</c> if no process does.<br/>
   /// Probing and catching is the mechanism: the OS offers no "is the process that started at time T with id X alive" query.</summary>
   ProcessIdentity? ProcessCurrentlyHavingMyId()
   {
      try
      {
         using var process = Process.GetProcessById(ProcessId);
         return process.Identity;
      }
      catch(ArgumentException) //Process.GetProcessById throws this if and only if no process with the id is running — the id's owner has exited.
      {
         return null;
      }
   }

   ///<summary>How far apart two readings of the same process's <see cref="Process.StartTime"/> — taken by two different<br/>
   /// processes — may fall and still be treated as the same process. Comfortably above the observed cross-reader skew on<br/>
   /// every supported OS, yet far below any interval in which the OS could recycle a process id AND the new owner could<br/>
   /// start within this window of the original's start time, so it never conflates two genuinely different processes.</summary>
   static readonly TimeSpan StartTimeReaderSkewTolerance = TimeSpan.FromSeconds(1);

   public bool Equals(ProcessIdentity? other) => other != null
                                              && other.ProcessId == ProcessId
                                              && (StartTime - other.StartTime).Duration() <= StartTimeReaderSkewTolerance;

   public override bool Equals(object? obj) => Equals(obj as ProcessIdentity);
   public override int GetHashCode() => ProcessId.GetHashCode();
   public static bool operator ==(ProcessIdentity? left, ProcessIdentity? right) => Equals(left, right);
   public static bool operator !=(ProcessIdentity? left, ProcessIdentity? right) => !Equals(left, right);
}
