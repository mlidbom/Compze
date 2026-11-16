// Runs, against Compze.Teventive 0.6.0-alpha from nuget.org, what dev_docs/taggregate-improvements/fable claims from reading the code:
// each check prints the claim and then what was observed.
using System.Diagnostics;
using Compze.Abstractions;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses.Shared;
using Compze.Teventive.Taggregates.Tevents;

const string anObserverThatReactsEveryTime = "--an-observer-that-reacts-every-time";
if(args is [anObserverThatReactsEveryTime])
{
   var note = Note.Create("a");
   ((ITaggregate)note).TeventStream.Subscribe(new Observer(wrapped =>
   {
      if(wrapped.Tevent is INoteTevent.TextChanged { Text: "b" }) note.ChangeText("c");
   }));
   note.ChangeText("b");
   return;
}

Check("1. An observer of the tevent stream that publishes on the same taggregate: tevents are notified twice, then Publish throws",
      () =>
      {
         var note = Note.Create("a");
         Commit(note);
         var notified = new List<string>();
         var hasPublishedFromTheObserver = false;
         ((ITaggregate)note).TeventStream.Subscribe(new Observer(wrapped =>
         {
            notified.Add(Describe(wrapped));
            if(hasPublishedFromTheObserver) return;
            hasPublishedFromTheObserver = true;
            note.ChangeText("c");
         }));

         var thrown = Thrown(() => note.ChangeText("b"));
         return $"notified in this order: [{string.Join(", ", notified)}]; thrown: {thrown}; the note's text is now \"{note.Text}\", its version {note.Version}";
      });

Check("1b. An observer that publishes every time it is notified of the tevent it reacts to (found by the JAStudio session): run in a process of its own, since nothing can catch a stack overflow",
      () =>
      {
         using var process = Process.Start(new ProcessStartInfo(Environment.ProcessPath!, anObserverThatReactsEveryTime) { RedirectStandardError = true, RedirectStandardOutput = true })!;
         process.ErrorDataReceived += (_, _) => {};
         process.OutputDataReceived += (_, _) => {};
         process.BeginErrorReadLine();
         process.BeginOutputReadLine();
         process.WaitForExit();
         return $"the process exited with code {process.ExitCode} (0x{process.ExitCode:X8}; 0xC00000FD is a stack overflow)";
      });

Check("2a. A tevent published from inside Commit's callback, while the callback enumerates the tevents: the callback's enumeration throws",
      () =>
      {
         var note = Note.Create("a");
         var thrown = Thrown(() => ((ITaggregate)note).Commit(uncommitted =>
         {
            foreach(var _ in uncommitted) note.ChangeText("b");
         }));
         return $"thrown: {thrown}";
      });

Check("2b. A tevent published from inside Commit's callback, after the callback has copied the tevents: it is never committed",
      () =>
      {
         var note = Note.Create("a");
         var committed = new List<string>();
         ((ITaggregate)note).Commit(uncommitted =>
         {
            committed.AddRange(uncommitted.ToArray().Select(Describe));
            note.ChangeText("b");
         });
         ((ITaggregate)note).Commit(uncommitted => committed.AddRange(uncommitted.Select(Describe)));
         return $"committed over two calls to Commit: [{string.Join(", ", committed)}]; the note's text is \"{note.Text}\", its version {note.Version}";
      });

Check("3. A tevent handler that throws: the tevent stays applied and uncommitted, and is notified with the next publish",
      () =>
      {
         var note = NoteWithAThrowingTeventHandler.Create("a");
         Commit(note);
         var notified = new List<string>();
         ((ITaggregate)note).TeventStream.Subscribe(new Observer(wrapped => notified.Add(Describe(wrapped))));

         var thrown = Thrown(() => note.ChangeText("throw"));
         var afterTheException = $"text \"{note.Text}\", version {note.Version}, notified [{string.Join(", ", notified)}]";
         note.ChangeText("next");
         var committed = new List<string>();
         ((ITaggregate)note).Commit(uncommitted => committed.AddRange(uncommitted.Select(Describe)));
         return $"thrown: {thrown}; after it: {afterTheException}; after the next publish: notified [{string.Join(", ", notified)}], committed [{string.Join(", ", committed)}]";
      });

Check("4. A tevent constructed outside Publish and loaded with LoadFromHistory leaves the taggregate at version 0, and its next tevent is refused",
      () =>
      {
         var created = new NoteTevent.Created(new TaggregateId(), "a");
         var note = new Note();
         ((ITaggregate)note).LoadFromHistory([TaggregateIdentifyingTevent.WrapIn(typeof(NoteTevent<>), created)]);
         var thrown = Thrown(() => note.ChangeText("b"));
         return $"after loading: text \"{note.Text}\", version {note.Version}; the next publish throws: {thrown}";
      });

Check("5. A slot in a component (JAStudio's usual case): publishing through it, and loading from the committed history",
      () =>
      {
         var note = Note.Create("a");
         note.Related.Synonyms.Add("synonym");
         note.Related.Antonyms.Add("antonym");
         var committed = new List<ITaggregateTevent<ITaggregateTevent>>();
         ((ITaggregate)note).Commit(committed.AddRange);

         var loaded = new Note();
         ((ITaggregate)loaded).LoadFromHistory(committed);
         return $"published: synonyms [{string.Join(", ", note.Related.Synonyms.Words)}], antonyms [{string.Join(", ", note.Related.Antonyms.Words)}]; "
              + $"loaded: synonyms [{string.Join(", ", loaded.Related.Synonyms.Words)}], antonyms [{string.Join(", ", loaded.Related.Antonyms.Words)}], version {loaded.Version}; "
              + $"committed types: [{string.Join(", ", committed.Select(Describe))}]";
      });

Check("6. What a subscriber can subscribe to, for a tevent adopted through a slot",
      () =>
      {
         var note = Note.Create("a");
         Commit(note);
         note.Related.Synonyms.Add("synonym");
         note.Related.Antonyms.Add("antonym");
         var committed = new List<ITaggregateTevent<ITaggregateTevent>>();
         ((ITaggregate)note).Commit(committed.AddRange);

         var byOwnersInterface = new List<string>();
         var byTheTomponentsOwnTeventType = new List<string>();
         var byPublicationPath = new List<string>();
         var dispatcher = IMutableTeventDispatcher<ITevent>.New(TeventDispatcherConfig.IgnoreAllUnhandled);
         dispatcher.Register()
                   .For<INoteTevent.IChangeToWhatItMatches>(tevent => byOwnersInterface.Add(tevent.GetType().Name))
                   .For<IWordSetTevent.Added>(tevent => byTheTomponentsOwnTeventType.Add(tevent.Word))
                   .ForWrapped<INoteTevent<ISynonymsTevent<IWordSetTevent.Added>>>(wrapped => byPublicationPath.Add(wrapped.Tevent.Tevent.Word));
         foreach(var wrapped in committed) dispatcher.Dispatch(wrapped);

         return $"For<the owner's interface that only the synonyms' adopting wrapper tevent implements> received [{string.Join(", ", byOwnersInterface)}]; "
              + $"For<the tomponent's own tevent type> received [{string.Join(", ", byTheTomponentsOwnTeventType)}]; "
              + $"ForWrapped<the publication path of the synonyms> received [{string.Join(", ", byPublicationPath)}]";
      });

Check("7. A generic, covariant tevent interface that is not a wrapper: matched by covariance for a class, not for a struct",
      () =>
      {
         var asMediaReference = new List<string>();
         var asObject = new List<string>();
         var dispatcher = IMutableTeventDispatcher<ITevent>.New(TeventDispatcherConfig.IgnoreAllUnhandled);
         dispatcher.Register()
                   .For<IMediaReferenceAdded<MediaReference>>(tevent => asMediaReference.Add(tevent.Reference.GetType().Name))
                   .For<IMediaReferenceAdded<object>>(tevent => asObject.Add(tevent.Reference.GetType().Name));
         dispatcher.Dispatch(new MediaReferenceAdded<ImageReference>(new ImageReference()));
         dispatcher.Dispatch(new MediaReferenceAdded<StructReference>(new StructReference()));

         return $"For<IMediaReferenceAdded<MediaReference>> received [{string.Join(", ", asMediaReference)}]; For<IMediaReferenceAdded<object>> received [{string.Join(", ", asObject)}], of an ImageReference and a StructReference dispatched";
      });

return;

static void Check(string claim, Func<string> observe)
{
   Console.WriteLine(claim);
   Console.WriteLine($"   observed: {observe()}");
   Console.WriteLine();
}

static string Thrown(Action action)
{
   try
   {
      action();
      return "nothing";
   }
   catch(Exception exception)
   {
      return $"{exception.GetType().Name} (\"{exception.Message.Split('\n')[0].Trim()}\")";
   }
}

static void Commit(ITaggregate taggregate) => taggregate.Commit(_ => {});

static string Describe(ITaggregateTevent<ITaggregateTevent> wrapped) => wrapped.Tevent switch
{
   INoteTevent.PropertyUpdated.Text text => $"{TypeName(wrapped.Tevent.GetType())}(\"{text.Text}\") v{wrapped.Tevent.TaggregateVersion}",
   _                                     => $"{TypeName(wrapped.Tevent.GetType())} v{wrapped.Tevent.TaggregateVersion}"
};

static string TypeName(Type type) => type.IsGenericType
                                        ? $"{type.Name.Split('`')[0]}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>"
                                        : type.Name;

sealed class Observer(Action<ITaggregateTevent<ITaggregateTevent>> onNext) : IObserver<ITaggregateTevent<ITaggregateTevent>>
{
   public void OnCompleted() {}
   public void OnError(Exception error) {}
   public void OnNext(ITaggregateTevent<ITaggregateTevent> value) => onNext(value);
}

// ---- A note's tevents ----

interface INoteTevent<out T> : ITaggregateTevent<T> where T : INoteTevent;

interface INoteTevent : ITaggregateTevent
{
   interface Created : ITaggregateCreatedTevent, PropertyUpdated.Text;
   interface TextChanged : PropertyUpdated.Text;

   interface PropertyUpdated : INoteTevent
   {
      interface Text : PropertyUpdated { string Text { get; } }
   }

   /// An interface of the owner's that a slot's adopting wrapper tevent implements: what JAStudio's subscribers would mostly subscribe to.
   interface IChangeToWhatItMatches : INoteTevent;

   /// The tevents of the note's component, which holds two shared tomponents of one class.
   interface Related : INoteTevent;
}

class NoteTevent<T>(T tevent) : TaggregateTevent<T>(tevent), INoteTevent<T> where T : INoteTevent;

abstract class NoteTevent : TaggregateTevent, INoteTevent
{
   protected NoteTevent() {}
   protected NoteTevent(TaggregateId id) : base(id) {}

   internal class Created(TaggregateId id, string text) : NoteTevent(id), INoteTevent.Created
   {
      public string Text { get; } = text;
   }

   internal class TextChanged(string text) : NoteTevent, INoteTevent.TextChanged
   {
      public string Text { get; } = text;
   }

   internal abstract class Related : NoteTevent, INoteTevent.Related;
}

// ---- A shared tomponent, and the adopting wrapper tevents of its two slots in the note's component ----

interface IWordSetTevent : ITevent
{
   interface Added : IWordSetTevent { string Word { get; } }
}

class WordSetTevent : IWordSetTevent
{
   internal class Added(string word) : WordSetTevent, IWordSetTevent.Added
   {
      public string Word { get; } = word;
   }
}

interface ISynonymsTevent<out T> : INoteTevent.Related, INoteTevent.IChangeToWhatItMatches, IPublisherTevent<T> where T : IWordSetTevent;

class SynonymsTevent<T>(T tevent) : NoteTevent.Related, ISynonymsTevent<T> where T : IWordSetTevent
{
   public T Tevent { get; } = tevent;
}

interface IAntonymsTevent<out T> : INoteTevent.Related, IPublisherTevent<T> where T : IWordSetTevent;

class AntonymsTevent<T>(T tevent) : NoteTevent.Related, IAntonymsTevent<T> where T : IWordSetTevent
{
   public T Tevent { get; } = tevent;
}

class WordSet : SharedTomponent<IWordSetTevent>
{
   readonly List<string> _words = [];
   public IReadOnlyList<string> Words => _words;

   public WordSet(ISharedTomponentSlot<IWordSetTevent> slot) : base(slot) => RegisterTeventAppliers().For<IWordSetTevent.Added>(tevent => _words.Add(tevent.Word));

   public void Add(string word) => Publish(new WordSetTevent.Added(word));
}

// ---- The note ----

class Note : Taggregate<Note, INoteTevent, NoteTevent, INoteTevent<INoteTevent>, NoteTevent<NoteTevent>>
{
   public string Text { get; private set; } = "";
   public RelatedPart Related { get; }

   public Note()
   {
      RegisterTeventAppliers().For<INoteTevent.PropertyUpdated.Text>(tevent => Text = tevent.Text);
      Related = new RelatedPart(this);
   }

   public static Note Create(string text)
   {
      var note = new Note();
      note.Publish(new NoteTevent.Created(new TaggregateId(), text));
      return note;
   }

   public void ChangeText(string text) => Publish(new NoteTevent.TextChanged(text));

   internal class RelatedPart : Component<RelatedPart, INoteTevent.Related, NoteTevent.Related>
   {
      public WordSet Synonyms { get; }
      public WordSet Antonyms { get; }

      public RelatedPart(Note note) : base(note)
      {
         Synonyms = new WordSet(new SharedTomponentSlot<INoteTevent.Related, NoteTevent.Related, IWordSetTevent, ISynonymsTevent<IWordSetTevent>>(this, typeof(SynonymsTevent<IWordSetTevent>)));
         Antonyms = new WordSet(new SharedTomponentSlot<INoteTevent.Related, NoteTevent.Related, IWordSetTevent, IAntonymsTevent<IWordSetTevent>>(this, typeof(AntonymsTevent<IWordSetTevent>)));
      }
   }
}

class NoteWithAThrowingTeventHandler : Taggregate<NoteWithAThrowingTeventHandler, INoteTevent, NoteTevent, INoteTevent<INoteTevent>, NoteTevent<NoteTevent>>
{
   public string Text { get; private set; } = "";

   NoteWithAThrowingTeventHandler()
   {
      RegisterTeventAppliers().For<INoteTevent.PropertyUpdated.Text>(tevent => Text = tevent.Text);
      RegisterTeventHandlers().For<INoteTevent.TextChanged>(tevent =>
      {
         if(tevent.Text == "throw") throw new InvalidOperationException("the tevent handler failed");
      });
   }

   public static NoteWithAThrowingTeventHandler Create(string text)
   {
      var note = new NoteWithAThrowingTeventHandler();
      note.Publish(new NoteTevent.Created(new TaggregateId(), text));
      return note;
   }

   public void ChangeText(string text) => Publish(new NoteTevent.TextChanged(text));
}

// ---- A generic, covariant tevent interface that is not a wrapper ----

interface IMediaReferenceAdded<out TReference> : ITevent
{
   TReference Reference { get; }
}

class MediaReferenceAdded<TReference>(TReference reference) : IMediaReferenceAdded<TReference>
{
   public TReference Reference { get; } = reference;
}

class MediaReference;
class ImageReference : MediaReference;
struct StructReference;
