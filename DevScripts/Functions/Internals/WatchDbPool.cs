#:package MemoryPack@1.21.4
#:property NuGetAuditMode=direct
#:property Nullable=enable
#:property PublishAot=false

// Watches the reservations in one of the DbPool's machine-wide states while tests run: how many of its databases are
// reserved, by how many pools, in which test processes, and how long each reservation was held. It reads the state's file
// without taking the state's lock, so that watching changes nothing in what it watches, and takes a sample only when two
// reads in a row agree. It loads the Compze.DbPool.dll it is given from its bytes, so that a build can replace the file
// while it watches. Run by C-Watch-DbPool, which explains the arguments:
//   <Compze.DbPool.dll> <SQL layer type's full name> <reservation length in seconds> <milliseconds between samples>
//   <log path> <seconds to watch, 0 for until Ctrl+C> <seconds without reservations to stop after, 0 for never>

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using MemoryPack;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var dbPoolAssemblyPath = Path.GetFullPath(args[0]);
var sqlLayerTypeName = args[1];
var reservationLength = TimeSpan.FromSeconds(double.Parse(args[2], CultureInfo.InvariantCulture));
var sampleInterval = TimeSpan.FromMilliseconds(int.Parse(args[3], CultureInfo.InvariantCulture));
var logPath = args[4];
var watchFor = TimeSpan.FromSeconds(int.Parse(args[5], CultureInfo.InvariantCulture));
var stopAfterIdleFor = TimeSpan.FromSeconds(int.Parse(args[6], CultureInfo.InvariantCulture));

var dbPool = DbPoolAssembly.Load(dbPoolAssemblyPath);
var stateFile = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                          "Compze", "SharedFiles", $"{sqlLayerTypeName}{dbPool.StateNameSuffix}.mmf"));
if(!stateFile.Exists)
{
   Console.Error.WriteLine($"No machine-wide state at {stateFile.FullName}: no pool of this version has used the SQL layer {sqlLayerTypeName} on this machine.");
   return 2;
}

var stopRequested = false;
Console.CancelKeyPress += (_, eventArgs) =>
{
   eventArgs.Cancel = true;
   stopRequested = true;
};

using var log = new StreamWriter(logPath, append: false) { AutoFlush = true };
var watch = new Watch(reservationLength, log);
log.WriteLine($"Watching {stateFile.FullName}, a sample every {sampleInterval.TotalMilliseconds} ms.");
Console.WriteLine($"Watching {stateFile.Name}; the log: {logPath}");

var startedAt = DateTime.UtcNow;
var lastReportAt = DateTime.MinValue;
DateTime? lastReservationSeenAt = null;
while(!stopRequested)
{
   var now = DateTime.UtcNow;
   if(watchFor > TimeSpan.Zero && now - startedAt > watchFor) break;
   if(stopAfterIdleFor > TimeSpan.Zero && lastReservationSeenAt is {} lastSeen && now - lastSeen > stopAfterIdleFor) break;

   if(StateFileContent.TryRead(stateFile) is {} content)
   {
      var sample = dbPool.SampleOf(content);
      watch.Take(now, sample);
      if(Watch.AnyIsForARunningProcess(sample.Reservations)) lastReservationSeenAt = now;
      if(now - lastReportAt >= TimeSpan.FromSeconds(1))
      {
         Console.WriteLine(watch.DescribeNow(now));
         lastReportAt = now;
      }
   }

   Thread.Sleep(sampleInterval);
}

var summary = watch.Summary(DateTime.UtcNow);
log.WriteLine(summary);
Console.WriteLine(summary);
return 0;

///<summary>The reservation of one pool database, as the machine-wide state records it. Two are equal when they are the same reservation:<br/>
/// of the same database, by the same pool, ending at the same time.</summary>
sealed class Reservation : IEquatable<Reservation>
{
   internal Reservation(int databaseId, Guid poolId, int processId, string name, DateTime expiresAt)
   {
      DatabaseId = databaseId;
      PoolId = poolId;
      ProcessId = processId;
      Name = name;
      ExpiresAt = expiresAt;
   }

   ///<summary>What <see cref="ProcessId"/> is for a reservation made by a DbPool from before 0.4.0, which didn't record its process.</summary>
   internal const int UnknownProcessId = 0;

   internal int DatabaseId { get; }
   internal Guid PoolId { get; }
   internal int ProcessId { get; }
   internal string Name { get; }
   DateTime ExpiresAt { get; }

   internal DateTime ReservedAt(TimeSpan reservationLength) => ExpiresAt - reservationLength;

   ///<summary>The kind of database the reservation is for, by the end of its name: an endpoint's domain database, its peer registry's or its<br/>
   /// type-id interner's.</summary>
   internal string Kind => Name.EndsWith(".PeerRegistry", StringComparison.Ordinal) ? "peer registry"
                         : Name.EndsWith(".TypeIdInterner", StringComparison.Ordinal) ? "type-id interner"
                         : "domain";

   public bool Equals(Reservation? other) => other is not null && other.DatabaseId == DatabaseId && other.PoolId == PoolId && other.ExpiresAt == ExpiresAt;
   public override bool Equals(object? obj) => obj is Reservation other && Equals(other);
   public override int GetHashCode() => HashCode.Combine(DatabaseId, PoolId, ExpiresAt);
}

///<summary>A reservation that has ended, and how long it was held.</summary>
sealed class EndedReservation
{
   internal EndedReservation(Reservation reservation, string process, TimeSpan heldFor)
   {
      Reservation = reservation;
      Process = process;
      HeldFor = heldFor;
   }

   internal Reservation Reservation { get; }
   internal string Process { get; }
   internal TimeSpan HeldFor { get; }
}

///<summary>The Compze.DbPool.dll of a checkout, loaded from its bytes, and what the watch reads with it.</summary>
sealed class DbPoolAssembly
{
   readonly Type _stateType;
   readonly PropertyInfo _databases;

   DbPoolAssembly(Assembly assembly)
   {
      _stateType = assembly.GetType("Compze.DbPool.DbPoolState", throwOnError: true)!;
      _databases = _stateType.GetProperty("Databases")!;
      var formatVersion = assembly.GetType("Compze.DbPool._private.DbPoolMachineWideState", throwOnError: true)!
                                  .GetField("FormatVersion", BindingFlags.NonPublic | BindingFlags.Static)
                                 ?.GetRawConstantValue();
      StateNameSuffix = formatVersion is null ? "" : $"_{formatVersion}";
   }

   ///<summary>Loads the assembly at <paramref name="path"/>, resolving the Compze assemblies it uses from its folder, from their bytes too.</summary>
   internal static DbPoolAssembly Load(string path)
   {
      var folder = Path.GetDirectoryName(path)!;
      AssemblyLoadContext.Default.Resolving += (context, name) =>
      {
         var candidate = Path.Combine(folder, $"{name.Name}.dll");
         return File.Exists(candidate) ? context.LoadFromStream(new MemoryStream(File.ReadAllBytes(candidate))) : null;
      };
      return new DbPoolAssembly(AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(File.ReadAllBytes(path))));
   }

   ///<summary>What follows the SQL layer's name in the machine-wide state's name: the format version, which DbPool 0.4.0 introduced, nothing<br/>
   /// before it.</summary>
   internal string StateNameSuffix { get; }

   internal Sample SampleOf(byte[] stateFileContent)
   {
      var state = MemoryPackSerializer.Deserialize(_stateType, stateFileContent)!;
      var databases = ((System.Collections.IEnumerable)_databases.GetValue(state)!).Cast<object>().ToList();
      return new Sample([..databases.Select(ReservationOf).OfType<Reservation>()], databases.Count);
   }

   static Reservation? ReservationOf(object database)
   {
      var type = database.GetType();
      if(!(bool)type.GetProperty("IsReserved")!.GetValue(database)!) return null;

      return new Reservation(databaseId: (int)type.GetProperty("Id")!.GetValue(database)!,
                             poolId: (Guid)type.GetProperty("ReservedByPoolId")!.GetValue(database)!,
                             processId: ReservingProcessIdOf(database),
                             name: (string)type.GetProperty("ReservationName")!.GetValue(database)!,
                             expiresAt: (DateTime)type.GetProperty("ReservationExpirationTime")!.GetValue(database)!);
   }

   ///<summary>The id of the process the database is reserved in, or <see cref="Reservation.UnknownProcessId"/> for a DbPool from before 0.4.0,<br/>
   /// which didn't record it.</summary>
   static int ReservingProcessIdOf(object database)
   {
      var process = database.GetType().GetProperty("ReservedInProcess", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(database);
      return process is null ? Reservation.UnknownProcessId : (int)process.GetType().GetProperty("Id", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(process)!;
   }
}

///<summary>What one reading of the state showed: its reservations, and how many databases the pools have created.</summary>
sealed class Sample
{
   internal Sample(IReadOnlyList<Reservation> reservations, int createdDatabases)
   {
      Reservations = reservations;
      CreatedDatabases = createdDatabases;
   }

   internal IReadOnlyList<Reservation> Reservations { get; }

   ///<summary>How many databases the pools have created: as many as were ever reserved at once, up to the SQL layer's maximum. The state keeps<br/>
   /// every one, so the pools may reserve as many as this, whatever the maximum of the code they run.</summary>
   internal int CreatedDatabases { get; }

   ///<summary>Whether a reservation would have to wait, unless the SQL layer allows it to create another database.</summary>
   internal bool EveryCreatedDatabaseIsReserved => CreatedDatabases > 0 && Reservations.Count >= CreatedDatabases;
}

///<summary>Reads the machine-wide state's file as the pools last wrote it, without its lock.</summary>
static class StateFileContent
{
   const int HeaderSize = 4;

   ///<summary>The state's serialized bytes, or null when two reads in a row disagree: a pool was writing it.</summary>
   internal static byte[]? TryRead(FileInfo stateFile)
   {
      var first = Read(stateFile);
      var second = Read(stateFile);
      return first.AsSpan().SequenceEqual(second) ? first : null;
   }

   static byte[] Read(FileInfo stateFile)
   {
      using var stream = new FileStream(stateFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
      Span<byte> header = stackalloc byte[HeaderSize];
      stream.ReadExactly(header);
      var content = new byte[BitConverter.ToInt32(header)];
      stream.ReadExactly(content);
      return content;
   }
}

///<summary>What the samples have shown so far: the reservations held now, and those that have ended.</summary>
sealed class Watch
{
   readonly TimeSpan _reservationLength;
   readonly StreamWriter _log;

   internal Watch(TimeSpan reservationLength, StreamWriter log)
   {
      _reservationLength = reservationLength;
      _log = log;
   }

   readonly Dictionary<Reservation, DateTime> _heldSince = [];
   readonly List<EndedReservation> _ended = [];
   readonly Dictionary<int, string> _processes = [];
   int _mostReserved;
   int _createdDatabases;
   TimeSpan _timeWithEveryDatabaseReserved;
   DateTime? _lastSampleAt;

   internal void Take(DateTime now, Sample sample)
   {
      var current = sample.Reservations.ToHashSet();
      foreach(var started in current.Where(it => !_heldSince.ContainsKey(it)))
      {
         _heldSince[started] = started.ReservedAt(_reservationLength);
         _log.WriteLine($"{Time(now)} reserved database {started.DatabaseId} for the {started.Kind} {started.Name}, pool {Short(started.PoolId)}, {ProcessName(started.ProcessId)}");
      }

      foreach(var ended in _heldSince.Keys.Where(it => !current.Contains(it)).ToList())
      {
         var heldFor = now - _heldSince[ended];
         _heldSince.Remove(ended);
         _ended.Add(new EndedReservation(ended, ProcessName(ended.ProcessId), heldFor));
         _log.WriteLine($"{Time(now)} released database {ended.DatabaseId} after {heldFor.TotalSeconds:0.0} s: the {ended.Kind} {ended.Name}, pool {Short(ended.PoolId)}, {ProcessName(ended.ProcessId)}");
      }

      _mostReserved = Math.Max(_mostReserved, current.Count);
      _createdDatabases = sample.CreatedDatabases;
      if(sample.EveryCreatedDatabaseIsReserved && _lastSampleAt is {} lastSampleAt) _timeWithEveryDatabaseReserved += now - lastSampleAt;
      _lastSampleAt = now;
   }

   internal string DescribeNow(DateTime now)
   {
      var held = _heldSince.Keys.ToList();
      var byProcess = held.GroupBy(it => ProcessName(it.ProcessId)).OrderByDescending(it => it.Count()).Select(it => $"{it.Key} {it.Count()}");
      var perPool = held.GroupBy(it => it.PoolId).GroupBy(it => it.Count()).OrderBy(it => it.Key).Select(it => $"{it.Key}:{it.Count()}");
      var oldest = held.Count == 0 ? "" : $"; oldest held {(now - held.Min(it => _heldSince[it])).TotalSeconds:0.0} s";
      var line = $"{Time(now)} reserved {held.Count} of {_createdDatabases} created by {held.Select(it => it.PoolId).Distinct().Count()} pools (databases per pool, pools: {string.Join(" ", perPool)}){oldest}; {string.Join(", ", byProcess)}";
      _log.WriteLine(line);
      return line;
   }

   internal string Summary(DateTime now)
   {
      var holds = _ended.Select(it => it.HeldFor.TotalSeconds).Order().ToList();
      var longest = _ended.OrderByDescending(it => it.HeldFor).Take(15)
                          .Select(it => $"   {it.HeldFor.TotalSeconds,6:0.0} s  database {it.Reservation.DatabaseId,3}  {it.Process}  the {it.Reservation.Kind} {it.Reservation.Name}");
      var stillHeld = _heldSince.Select(it => $"   held {(now - it.Value).TotalSeconds:0.0} s so far: database {it.Key.DatabaseId}, {ProcessName(it.Key.ProcessId)}, the {it.Key.Kind} {it.Key.Name}");
      return $"""
              Summary:
                 most reserved at once: {_mostReserved}; databases created: {_createdDatabases}; every database created reserved for {_timeWithEveryDatabaseReserved.TotalSeconds:0.0} s in all
                 reservations ended: {holds.Count}; held for: median {Percentile(holds, 0.5):0.0} s, 90th percentile {Percentile(holds, 0.9):0.0} s, 99th {Percentile(holds, 0.99):0.0} s, longest {holds.LastOrDefault():0.0} s
                 the longest held:
              {string.Join(Environment.NewLine, longest)}
                 still held at the end: {_heldSince.Count}
              {string.Join(Environment.NewLine, stillHeld)}
              """;
   }

   static double Percentile(List<double> sorted, double fraction) => sorted.Count == 0 ? 0 : sorted[(int)Math.Min(sorted.Count - 1, Math.Floor(sorted.Count * fraction))];

   string ProcessName(int processId)
   {
      if(processId == Reservation.UnknownProcessId) return "a process the pool didn't record";

      if(!_processes.TryGetValue(processId, out var name))
         _processes[processId] = name = $"{TestProjectOf(processId)} ({processId})";
      return name;
   }

   ///<summary>Whether any of <paramref name="reservations"/> is for a process that still runs, or one the pool didn't record: those of an exited<br/>
   /// process stay in the state until the next reservation releases them.</summary>
   internal static bool AnyIsForARunningProcess(IReadOnlyList<Reservation> reservations) =>
      reservations.Select(it => it.ProcessId).Distinct().Any(processId => processId == Reservation.UnknownProcessId || IsRunning(processId));

   static bool IsRunning(int processId)
   {
      try
      {
         using var process = Process.GetProcessById(processId);
         return !process.HasExited;
      }
      catch(ArgumentException) //What Process.GetProcessById throws when no process with the id runs.
      {
         return false;
      }
   }

   ///<summary>The test project whose bin folder the process runs from, as a test host does, or the process's name.</summary>
   static string TestProjectOf(int processId)
   {
      try
      {
         using var process = Process.GetProcessById(processId);
         var path = process.MainModule?.FileName ?? process.ProcessName;
         var parts = path.Split(Path.DirectorySeparatorChar);
         var binIndex = Array.LastIndexOf(parts, "bin");
         return binIndex > 0 ? parts[binIndex - 1] : process.ProcessName;
      }
      catch(ArgumentException) //What Process.GetProcessById throws when the process has exited.
      {
         return "an exited process";
      }
   }

   static string Short(Guid poolId) => poolId.ToString()[..8];
   static string Time(DateTime utc) => utc.ToLocalTime().ToString("HH:mm:ss.f", CultureInfo.InvariantCulture);
}
