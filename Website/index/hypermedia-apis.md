<div>

#### [Browsable APIs](../paradigms/hypermedia-apis/introduction.md)
The most popular API in the world is a Browsable API (REST to be exact). You are using it right now. It's called the world wide web. Can you imagine trying to use it without a browser with support for links and forms? Imagine reading this page and instead of a link you are presented with: 4375...

That is actually how we build most APIs today. Think about that for a minute.

What if we designed Browsable APIs instead? APIs which: 

* Are accessed using a `Navigator`, browsed much like a website, by
  * Following `Link`s to `Get` Objects
  * `Post`ing `Command`s
  * All with full type safety and autocomplete in your IDE
* Can be in-memory, or remote over HTTP
* Further encapsulates your domain, exposing less implementation details than traditional services.
* Are excellently suited for building a Just-Beneath-The-UI-Rendering-Layer layer, ideal for black box testing.

Once you used APIs like that, how would you feel about an API that gives you an `int` instead of an `ILink<User>`?

</div>