using System.Diagnostics;

namespace Compze.Hosting.SameMachine;

///<summary>Identifies the OS process that announced an endpoint address into an <see cref="InterprocessEndpointRegistry"/>:<br/>
/// the process id plus the process's start time, because the OS recycles process ids — only the pair identifies a process uniquely.<br/>
/// The registry uses it to recognize addresses whose announcing process has exited, so a crashed process's stale addresses are never routed to.</summary>
public class AnnouncingProcess
{
   ///<summary>The process this code runs in — what a process announcing its own endpoints' addresses passes.</summary>
   public static AnnouncingProcess Current
   {
      get
      {
         using var currentProcess = Process.GetCurrentProcess();
         return new AnnouncingProcess(currentProcess.Id, currentProcess.StartTime.ToUniversalTime().Ticks);
      }
   }

   public int ProcessId { get; }

   ///<summary>The identified process's start time as UTC ticks — the disambiguator that detects process id reuse.</summary>
   public long StartTimeTicks { get; }

   public AnnouncingProcess(int processId, long startTimeTicks)
   {
      ProcessId = processId;
      StartTimeTicks = startTimeTicks;
   }

   ///<summary>True while the identified process is still running.</summary>
   public bool IsStillRunning
   {
      get
      {
         try
         {
            using var process = Process.GetProcessById(ProcessId);
            return process.StartTime.ToUniversalTime().Ticks == StartTimeTicks;
         }
         catch(ArgumentException) //Thrown if and only if no process with the id is running — the id's owner has exited. The OS offers no "is the process with id X that started at T alive" query; probing and catching IS the mechanism.
         {
            return false;
         }
         catch(InvalidOperationException) //The process exited between the probe above and reading its start time.
         {
            return false;
         }
      }
   }
}
