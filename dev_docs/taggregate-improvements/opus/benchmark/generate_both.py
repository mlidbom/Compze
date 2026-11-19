"""Generates Program.cs: the same note-like taggregate in two styles, to compare what instances retain and what loading costs.

- Registered: today's per-instance registration, RegisterTeventAppliers() in the constructors.
- Declared: the prototype's per-class TeventRouting, declared once in static fields.

- Convention, when the fourth argument is "convention": Apply methods found by reflection, in the convention prototype.

Usage: python generate_both.py <components> <properties per component> <output Program.cs> [convention]
"""
import sys

components = int(sys.argv[1])
properties_per_component = int(sys.argv[2])
output = sys.argv[3]

lines = []
add = lines.append
add("using System;")
add("using System.Collections.Generic;")
add("using System.Diagnostics;")
add("using System.Linq;")
add("using Compze.Abstractions;")
add("using Compze.Teventive;")
add("using Compze.Teventive.Taggregates.BaseClasses;")
add("using Compze.Teventive.Taggregates.Tevents;")
add("#pragma warning disable")
add("namespace Bench;")
add("")
add("public interface INoteTevent : ITaggregateTevent")
add("{")
add("   interface Loaded : INoteTevent, ITaggregateCreatedTevent { string[] Values { get; } }")
for c in range(components):
    add(f"   interface Part{c} : INoteTevent")
    add("   {")
    for p in range(properties_per_component):
        add(f"      interface Property{p}Updated : Part{c} {{ string Value {{ get; }} }}")
    add("   }")
add("}")
add("public interface INoteTevent<out T> : ITaggregateTevent<T> where T : INoteTevent;")
add("public class NoteTevent<T>(T tevent) : TaggregateTevent<T>(tevent), INoteTevent<T> where T : INoteTevent;")
add("public abstract class NoteTevent : TaggregateTevent, INoteTevent")
add("{")
add("   protected NoteTevent() {}")
add("   protected NoteTevent(TaggregateId id) : base(id) {}")
add("   public class Loaded(TaggregateId id, string[] values) : NoteTevent(id), INoteTevent.Loaded { public string[] Values { get; } = values; }")
for c in range(components):
    add(f"   public abstract class Part{c} : NoteTevent, INoteTevent.Part{c}")
    add("   {")
    for p in range(properties_per_component):
        add(f"      public class Property{p}Updated(string value) : Part{c}, INoteTevent.Part{c}.Property{p}Updated {{ public string Value {{ get; }} = value; }}")
    add("   }")
add("}")
add("")
add("public interface IBenchNote { void Edit(); }")
add("public static class Loading")
add("{")
add("   public static NoteTevent<NoteTevent.Loaded> LoadedTevent(string[] values)")
add("   {")
add("      var loaded = new NoteTevent.Loaded(new TaggregateId(), values);")
add("      ((IMutableTaggregateTevent)loaded).SetTaggregateVersionInternal(1);")
add("      return new NoteTevent<NoteTevent.Loaded>(loaded);")
add("   }")
add("}")

# Registered style
add("public class RegisteredNote : Taggregate<RegisteredNote, INoteTevent, NoteTevent, INoteTevent<INoteTevent>, NoteTevent<NoteTevent>>, IBenchNote")
add("{")
for c in range(components):
    add(f"   public RegisteredPart{c} Part{c} {{ get; }}")
add("   public RegisteredNote()")
add("   {")
add("      RegisterTeventAppliers().For<INoteTevent.Loaded>(_ => {});")
for c in range(components):
    add(f"      Part{c} = new RegisteredPart{c}(this);")
add("   }")
add("   public static RegisteredNote Load(string[] values) { var note = new RegisteredNote(); ((ITaggregate)note).LoadFromHistory([Loading.LoadedTevent(values)]); return note; }")
add("   public void Edit()")
add("   {")
for c in range(components):
    add(f"      Part{c}.SetProperty0(\"b\");")
add("      ((ITaggregate)this).Commit(_ => {});")
add("   }")
add("}")
for c in range(components):
    add(f"public class RegisteredPart{c} : RegisteredNote.Component<RegisteredPart{c}, INoteTevent.Part{c}, NoteTevent.Part{c}>")
    add("{")
    for p in range(properties_per_component):
        add(f"   public string Property{p} {{ get; private set; }} = \"\";")
    add(f"   public RegisteredPart{c}(RegisteredNote note) : base(note)")
    add("   {")
    add("      RegisterTeventAppliers()")
    for p in range(properties_per_component):
        add(f"        .For<INoteTevent.Part{c}.Property{p}Updated>(it => Property{p} = it.Value)")
    add("      ;")
    add("   }")
    add(f"   public void SetProperty0(string value) => Publish(new NoteTevent.Part{c}.Property0Updated(value));")
    add("}")

# Declared style
add("public class DeclaredNote : Taggregate<DeclaredNote, INoteTevent, NoteTevent, INoteTevent<INoteTevent>, NoteTevent<NoteTevent>>, IBenchNote")
add("{")
add("   static readonly TeventRouting<DeclaredNote> Appliers = TeventRouting<DeclaredNote>.DeclareAppliers(it => it")
add("     .For<INoteTevent.Loaded>(static (_, _) => {})")
for c in range(components):
    add(f"     .Component(static note => note.Part{c})")
add("   );")
add("   protected override TeventRouting TeventAppliers => Appliers;")
for c in range(components):
    add(f"   public DeclaredPart{c} Part{c} {{ get; }}")
add("   public DeclaredNote()")
add("   {")
for c in range(components):
    add(f"      Part{c} = new DeclaredPart{c}(this);")
add("   }")
add("   public static DeclaredNote Load(string[] values) { var note = new DeclaredNote(); ((ITaggregate)note).LoadFromHistory([Loading.LoadedTevent(values)]); return note; }")
add("   public void Edit()")
add("   {")
for c in range(components):
    add(f"      Part{c}.SetProperty0(\"b\");")
add("      ((ITaggregate)this).Commit(_ => {});")
add("   }")
add("}")
for c in range(components):
    add(f"public class DeclaredPart{c} : DeclaredNote.Component<DeclaredPart{c}, INoteTevent.Part{c}, NoteTevent.Part{c}>")
    add("{")
    add(f"   static readonly TeventRouting<DeclaredPart{c}> Appliers = TeventRouting<DeclaredPart{c}>.DeclareAppliers(it => it")
    for p in range(properties_per_component):
        add(f"     .For<INoteTevent.Part{c}.Property{p}Updated>(static (part, tevent) => part.Property{p} = tevent.Value)")
    add("   );")
    add("   protected override TeventRouting TeventAppliers => Appliers;")
    for p in range(properties_per_component):
        add(f"   public string Property{p} {{ get; private set; }} = \"\";")
    add(f"   public DeclaredPart{c}(DeclaredNote note) : base(note) {{}}")
    add(f"   public void SetProperty0(string value) => Publish(new NoteTevent.Part{c}.Property0Updated(value));")
    add("}")

# Convention style, generated only when asked for: it needs the convention prototype's Compze.Teventive.
if len(sys.argv) > 4 and sys.argv[4] == "convention":
    add("[TeventRoutingByConvention]")
    add("public class ConventionNote : Taggregate<ConventionNote, INoteTevent, NoteTevent, INoteTevent<INoteTevent>, NoteTevent<NoteTevent>>, IBenchNote")
    add("{")
    for c in range(components):
        add(f"   public ConventionPart{c} Part{c} {{ get; }}")
    add("   public ConventionNote()")
    add("   {")
    for c in range(components):
        add(f"      Part{c} = new ConventionPart{c}(this);")
    add("   }")
    add("   void Apply(INoteTevent.Loaded _) {}")
    add("   public static ConventionNote Load(string[] values) { var note = new ConventionNote(); ((ITaggregate)note).LoadFromHistory([Loading.LoadedTevent(values)]); return note; }")
    add("   public void Edit()")
    add("   {")
    for c in range(components):
        add(f"      Part{c}.SetProperty0(\"b\");")
    add("      ((ITaggregate)this).Commit(_ => {});")
    add("   }")
    add("}")
    for c in range(components):
        add("[TeventRoutingByConvention]")
        add(f"public class ConventionPart{c} : ConventionNote.Component<ConventionPart{c}, INoteTevent.Part{c}, NoteTevent.Part{c}>")
        add("{")
        for p in range(properties_per_component):
            add(f"   public string Property{p} {{ get; private set; }} = \"\";")
        add(f"   public ConventionPart{c}(ConventionNote note) : base(note) {{}}")
        for p in range(properties_per_component):
            add(f"   void Apply(INoteTevent.Part{c}.Property{p}Updated tevent) => Property{p} = tevent.Value;")
        add(f"   public void SetProperty0(string value) => Publish(new NoteTevent.Part{c}.Property0Updated(value));")
        add("}")
    add("public static partial class Program { static void LoadConvention() => ConventionNote.Load([\"a\"]).Edit(); }")
else:
    add("public static partial class Program { static void LoadConvention() {} }")

# Plain objects
add("public class PlainNote")
add("{")
for c in range(components):
    add(f"   public PlainPart{c} Part{c} {{ get; }} = new();")
add("}")
for c in range(components):
    add(f"public class PlainPart{c}")
    add("{")
    for p in range(properties_per_component):
        add(f"   public string Property{p} {{ get; set; }} = \"\";")
    add("}")

add("""
public static partial class Program
{
   static long RetainedPerInstance(Func<object> create, int count, out List<object> kept)
   {
      kept = new List<object>(count);
      GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
      var before = GC.GetTotalMemory(true);
      for(var i = 0; i < count; i++) kept.Add(create());
      var after = GC.GetTotalMemory(true);
      return (after - before) / count;
   }

   static long AllocatedPerCall(Action action, int count)
   {
      var before = GC.GetAllocatedBytesForCurrentThread();
      for(var i = 0; i < count; i++) action();
      return (GC.GetAllocatedBytesForCurrentThread() - before) / count;
   }

   static void Measure(string style, Func<IBenchNote> load, int count)
   {
      var retained = RetainedPerInstance(load, count, out var notes);
      GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
      var beforeEdits = GC.GetTotalMemory(true);
      var allocatedPerEdit = 0L;
      var benchNotes = notes.Cast<IBenchNote>().ToList();
      var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
      foreach(var note in benchNotes) note.Edit();
      allocatedPerEdit = (GC.GetAllocatedBytesForCurrentThread() - allocatedBefore) / count;
      var afterEdits = GC.GetTotalMemory(true);
      var allocatedPerLoad = AllocatedPerCall(() => load(), count);
      Console.WriteLine($"{style,-11} retained when loaded: {retained,7:N0} B   allocated by a load: {allocatedPerLoad,7:N0} B   retained more after an edit of each component: {(afterEdits - beforeEdits) / count,6:N0} B   allocated by those edits: {allocatedPerEdit,7:N0} B");
      GC.KeepAlive(notes);
   }

   static string ConventionFirst()
   {
      var stopwatch = Stopwatch.StartNew();
      LoadConvention();
      return $", {stopwatch.Elapsed.TotalMilliseconds:F1} ms by convention";
   }

   static double LoadSeconds(Func<IBenchNote> load, int count)
   {
      var kept = new List<IBenchNote>(count);
      GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
      var stopwatch = Stopwatch.StartNew();
      for(var i = 0; i < count; i++) kept.Add(load());
      var seconds = stopwatch.Elapsed.TotalSeconds;
      GC.KeepAlive(kept);
      return seconds;
   }

   public static void Main(string[] args)
   {
      const int count = 10_000;
      string[] values = ["a"];
      var firstLoad = Stopwatch.StartNew(); RegisteredNote.Load(values).Edit(); var registeredFirst = firstLoad.Elapsed.TotalMilliseconds;
      firstLoad.Restart(); DeclaredNote.Load(values).Edit(); var declaredFirst = firstLoad.Elapsed.TotalMilliseconds;
      new PlainNote(); //Warm up: static constructors, the validators' assembly scans, the routing's resolution.
      Console.WriteLine($"first load and edit, in a fresh process: {registeredFirst:F1} ms registered, {declaredFirst:F1} ms declared FIRST_CONVENTION");
      Console.WriteLine($"COMPONENTS components, PROPERTIES properties each, {count:N0} instances");
      Console.WriteLine($"plain       retained: {RetainedPerInstance(() => new PlainNote(), count, out var plain),7:N0} B"); GC.KeepAlive(plain);
      Measure("registered", () => RegisteredNote.Load(values), count);
      Measure("declared", () => DeclaredNote.Load(values), count);
CONVENTION_MEASURE
      if(args.Contains("--time"))
      {
         const int corpus = 80_000;
         for(var round = 1; round <= 3; round++)
            Console.WriteLine($"round {round}: loading {corpus:N0} notes took {LoadSeconds(() => RegisteredNote.Load(values), corpus):F2} s registered, {LoadSeconds(() => DeclaredNote.Load(values), corpus):F2} s declared CONVENTION_TIME");
      }
   }
}
""".replace("COMPONENTS", str(components)).replace("PROPERTIES", str(properties_per_component))
   .replace("FIRST_CONVENTION", "" if not (len(sys.argv) > 4 and sys.argv[4] == "convention") else "{ConventionFirst()}")
   .replace("CONVENTION_MEASURE", '      Measure("convention", () => ConventionNote.Load(values), count); ConventionNote.Load(values).Edit();' if len(sys.argv) > 4 and sys.argv[4] == "convention" else "")
   .replace("CONVENTION_TIME", ', {LoadSeconds(() => ConventionNote.Load(values), corpus):F2} s by convention' if len(sys.argv) > 4 and sys.argv[4] == "convention" else ""))

with open(output, "w", encoding="utf-8") as file:
    file.write("\n".join(lines))
