<div>

#### [Typermedia APIs](~/Compze/Typermedia/_docs/introduction.md)
The most popular API in the world is a Browsable API (REST to be exact). You are using it right now. It's called the world wide web. Can you imagine trying to use it without links and forms? Imagine reading this page and instead of a link you are presented with: 4375.

That is actually how we build most APIs today. Why?

Typermedia expands on hypermedia thinking by leveraging the .NET type system to create APIs which: 

* Can be fully explored using a `Navigator`, browsed much like a website, by
  * Getting Links
  * Posting Commands
  * All with full type safety and autocomplete in your IDE
* Can be in-memory, or remote
* Routes messages by .Net types giving 
  * Zero configuration routing
  * A simple already well known programming model.
* Further encapsulates your domain, exposing less implementation details than traditional services.
* Are excellently suited for building a Just-Beneath-The-UI-Rendering-Layer layer, ideal for black box testing.

Once you used APIs like that, how would you feel about an API that gives you an `int` instead of an `ILink<User>`?

</div>