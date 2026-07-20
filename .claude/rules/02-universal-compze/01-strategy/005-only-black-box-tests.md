Anything and everything we test should be tested through black box testing of the public API only.

If, for instance you are about to write tests for an internal implementation details such as a serializer used by some library. Stop and instead test what is exernally observable by a normal consumer of the library. 
* A message reaches the receiver with the correct data intact
* A taggregate is roundtripped to the tevent store with no data lost, etc. 
 
Unless we are building a public serialization library, the serializer MUST stay internal, and should NOT be tested in isolation.
The same goes for any and all implementation details of that kind. 

If it is not possible for a consumer to drive the library into the state we imagine we need to test, it is literally pointless to test it.

# When tests supposedly need a capability the public API lacks
1. Figure out how to test it using the public API. If that is impossible, seriously question if the test is needed.
2. A shipped public testing package (the `Compze.Tessaging.Hosting.Testing` pattern) — a first-class testing surface, not a hole into the library.
3. `*.InternalSpecifications` (below) as the last resort.

# Do NOT make the internals of a library visible to a test project using InternalsVisibleTo
If doing so seems unavoidable for some reason, use a separate test project dedicated to internals. Name it *.InternalSpecifications. Do so as rarely as possible. The vast majority of tests should only touch the public API and the need for any InternalSpecifications projects should be periodically re-evaluated. They are probably only of worth as scaffolding while building up new functionality based on the internal abstractions. Once the public functionality is in place and has its own tests, the value of the test of the internal stuff will usually have evaporated since all the actually possible usages of them should already be tested by the tests of the public API.

# Do use InternalsVisibleTo between library projects in order to keep things out of the public API. 