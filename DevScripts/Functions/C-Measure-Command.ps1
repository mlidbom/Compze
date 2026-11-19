# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Measure-Command {
    <#
    .SYNOPSIS
    Runs a command line and measures its wall time, the processor time it used, and the processor time the rest of the machine used meanwhile

    .DESCRIPTION
    For timing on a machine that other work shares, such as other sessions' builds and test runs: a run during which the
    rest of the machine used more than a little processor time was disturbed, and is to be repeated.

    The command line runs under cmd.exe /c, in a Windows job object of its own, with its output written to OutputPath. The
    job counts the processor time of every process the command starts, those that exit before it ends included. The
    machine's processor time comes from GetSystemTimes. What the rest of the machine used is the difference, so it also
    holds the kernel's own work, such as the network's.

    MSBuild's node reuse is turned off for the command, so that a dotnet build or test neither borrows the nodes of another
    build, whose time would count as the rest of the machine's, nor leaves nodes of its own behind for others to use.

    .PARAMETER CommandLine
    The command line to run, as cmd.exe /c runs it

    .PARAMETER OutputPath
    The file the command's standard output and standard error are written to

    .PARAMETER WorkingDirectory
    The directory the command runs in. The current location if left out.

    .OUTPUTS
    PSCustomObject with properties:
    - ElapsedSeconds: the wall time
    - CommandProcessorSeconds: the processor time of the command and every process it started
    - OtherProcessorSeconds: the processor time the rest of the machine used meanwhile
    - OtherAverageCoresBusy: OtherProcessorSeconds divided by ElapsedSeconds: how many cores the rest of the machine kept busy, on average
    - LogicalProcessors: the machine's logical processors
    - ExitCode: the command's exit code

    .EXAMPLE
    C-Measure-Command -CommandLine 'dotnet test test\Compze.Tests.Integration --no-build' -OutputPath $env:TEMP\run.log
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory)]
        [string]$CommandLine,

        [Parameter(Mandatory)]
        [string]$OutputPath,

        [string]$WorkingDirectory = (Get-Location).ProviderPath
    )

    Initialize-JobMeasuredCommandType

    $outputFullPath = [IO.Path]::GetFullPath($OutputPath, $WorkingDirectory)
    $previousNodeReuseSetting = $env:MSBUILDDISABLENODEREUSE
    $env:MSBUILDDISABLENODEREUSE = '1'
    try {
        $measurement = [CompzeDevScripts.JobMeasuredCommand]::Run("cmd.exe /c `"$CommandLine > `"$outputFullPath`" 2>&1`"", $WorkingDirectory)
    }
    finally {
        $env:MSBUILDDISABLENODEREUSE = $previousNodeReuseSetting
    }

    [pscustomobject]@{
        ElapsedSeconds          = [math]::Round($measurement.ElapsedSeconds, 1)
        CommandProcessorSeconds = [math]::Round($measurement.CommandProcessorSeconds, 1)
        OtherProcessorSeconds   = [math]::Round($measurement.OtherProcessorSeconds, 1)
        OtherAverageCoresBusy   = [math]::Round($measurement.OtherProcessorSeconds / $measurement.ElapsedSeconds, 2)
        LogicalProcessors       = [Environment]::ProcessorCount
        ExitCode                = $measurement.ExitCode
    }
}

function Initialize-JobMeasuredCommandType {
    if ('CompzeDevScripts.JobMeasuredCommand' -as [type]) { return }

    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CompzeDevScripts
{
    public sealed class CommandMeasurement
    {
        public double ElapsedSeconds { get; set; }
        public double CommandProcessorSeconds { get; set; }
        public double OtherProcessorSeconds { get; set; }
        public uint ExitCode { get; set; }
    }

    public static class JobMeasuredCommand
    {
        const uint CreateSuspended = 0x4;
        const uint CreateNoWindow = 0x08000000;
        const uint Infinite = 0xFFFFFFFF;
        const int JobObjectBasicAccountingInformationClass = 1;
        const double SecondsPerTick = 1e-7;

        public static CommandMeasurement Run(string commandLine, string workingDirectory)
        {
            var job = CreateJobObject(IntPtr.Zero, null);
            if (job == IntPtr.Zero) throw new Win32Exception();
            try
            {
                var startupInfo = new StartupInfo { cb = Marshal.SizeOf<StartupInfo>() };
                var machineBusyTicksAtStart = MachineBusyTicks();
                var stopwatch = Stopwatch.StartNew();
                if (!CreateProcess(null, new StringBuilder(commandLine), IntPtr.Zero, IntPtr.Zero, false, CreateSuspended | CreateNoWindow, IntPtr.Zero, workingDirectory, ref startupInfo, out var process))
                    throw new Win32Exception();
                try
                {
                    if (!AssignProcessToJobObject(job, process.hProcess)) throw new Win32Exception();
                    ResumeThread(process.hThread);
                    WaitForSingleObject(process.hProcess, Infinite);
                    stopwatch.Stop();
                    var machineBusyTicks = MachineBusyTicks() - machineBusyTicksAtStart;
                    if (!QueryInformationJobObject(job, JobObjectBasicAccountingInformationClass, out var accounting, Marshal.SizeOf<JobAccounting>(), IntPtr.Zero))
                        throw new Win32Exception();
                    GetExitCodeProcess(process.hProcess, out var exitCode);
                    var commandTicks = accounting.TotalUserTime + accounting.TotalKernelTime;
                    return new CommandMeasurement
                    {
                        ElapsedSeconds = stopwatch.Elapsed.TotalSeconds,
                        CommandProcessorSeconds = commandTicks * SecondsPerTick,
                        OtherProcessorSeconds = (machineBusyTicks - commandTicks) * SecondsPerTick,
                        ExitCode = exitCode
                    };
                }
                finally
                {
                    CloseHandle(process.hThread);
                    CloseHandle(process.hProcess);
                }
            }
            finally
            {
                CloseHandle(job);
            }
        }

        //GetSystemTimes' kernel time includes the idle time.
        static long MachineBusyTicks()
        {
            if (!GetSystemTimes(out var idle, out var kernel, out var user)) throw new Win32Exception();
            return kernel - idle + user;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct StartupInfo
        {
            public int cb;
            public string lpReserved, lpDesktop, lpTitle;
            public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
            public short wShowWindow, cbReserved2;
            public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct ProcessInformation
        {
            public IntPtr hProcess, hThread;
            public int dwProcessId, dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct JobAccounting
        {
            public long TotalUserTime, TotalKernelTime, ThisPeriodTotalUserTime, ThisPeriodTotalKernelTime;
            public int TotalPageFaultCount, TotalProcesses, ActiveProcesses, TotalTerminatedProcesses;
        }

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr CreateJobObject(IntPtr securityAttributes, string name);

        [DllImport("kernel32", SetLastError = true)]
        static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        [DllImport("kernel32", SetLastError = true)]
        static extern bool QueryInformationJobObject(IntPtr job, int informationClass, out JobAccounting information, int length, IntPtr returnLength);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool CreateProcess(string applicationName, StringBuilder commandLine, IntPtr processAttributes, IntPtr threadAttributes, bool inheritHandles, uint creationFlags, IntPtr environment, string currentDirectory, ref StartupInfo startupInfo, out ProcessInformation processInformation);

        [DllImport("kernel32", SetLastError = true)]
        static extern uint ResumeThread(IntPtr thread);

        [DllImport("kernel32", SetLastError = true)]
        static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32", SetLastError = true)]
        static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);

        [DllImport("kernel32", SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32", SetLastError = true)]
        static extern bool GetSystemTimes(out long idleTime, out long kernelTime, out long userTime);
    }
}
'@
}
