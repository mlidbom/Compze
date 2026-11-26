# Hacks and fragile ways in Compze

Where Compze's code depends on something that may change without notice, such as an operating system's interface that
its documentation reserves for itself. Each entry's code carries a `hack:` comment naming it, so that one search finds
every place: `grep -rn "hack:" src`.

### `windows-process-ids-using-a-file`: the processes using one file

- **Does:** `NtQueryInformationFile` with `FileProcessIdsUsingFileInformation` (47), which Microsoft's documentation calls
  "reserved for system use", through a handle opened only to read the file's attributes.
  `src/Compze.Internals.SystemCE/IOCE/IdsOfTheProcessesUsingAFile.cs`.
- **Depends on:** the information class's number and its layout: a count, then the process ids, each as long as a
  pointer.
- **Fails:** if Windows changes it, `IdsOfTheProcessesUsingAFile.Of` throws an `IOException` naming the NTSTATUS, and
  `FileReplacement`'s failures say that Windows didn't tell who uses the file, instead of naming the processes.
- **Could replace it:** Windows' Restart Manager, which is documented, at about 700 ms a file instead of about 80 ms,
  and which refuses a folder (measured 2026-11-24).
- **Also used by:** the tooling in `~/.claude`, whose entry of the same name is in its `dev_docs/hacks-and-fragile-ways.md`.
