<div>

#### [Browsable APIs](../paradigms/browsable-apis/browsable-apis-introduction.md)
The, by far, most popular API in the world is a Browsable API (specifically a Hypermedia API). You are using it right now. It's called the world wide web. Can you imagine trying to use it without a browser with support for links? Imagine reading a web page and instead of a links you are presented with `Guid`s, or `int`s with no hint as to what to do with them...

Well. That is, literally, how we build most APIs today!

What if we designed Browsable APIs instead? APIs which: 

* Are accessed using a browser, navigated much like a website.
  * Following links and queries
  * Posting commands and getting the results back
  * All with full type safety and autocomplete in your IDE
* Can be in-memory, or remote over HTTP
* Further encapsulates your domain, exposing less implementation details than traditional services.
* Are excellently suited for building a Just-Beneath-The-UI-Rendering-Layer layer, ideal for black box testing.

Once you used APIs like that, how would you feel about an API that gives you an `int` instead of an `ILink<User>`?
</div>