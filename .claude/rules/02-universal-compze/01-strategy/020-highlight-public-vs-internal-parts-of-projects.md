In order to make it easy for a reader to follow the structure of the code we use namespace and project name conventions

## _internal and _private namespaces

* Code that should never be used directly by a consumer of a library goes in namespaces called:
  * _internal: For code that IS shared with another Compze *library* using InternalsVisibleTo — a type no other library consumes does not belong here; it goes under _private.
  * _private: For code that no other library may use, even where InternalsVisibleTo makes it technically visible.

**The line both sections draw is what shipped code can see.** A white-box specification project — the `*.InternalSpecifications` and `*.Internals` families — is not shipped code, so it sits on neither side of that line: it may reach into `_private`, and reaching in never makes a type `_internal`. The rare specification that must reach past a library's public API says so by living in a project whose *name* says so — see [only-black-box-tests](005-only-black-box-tests.md) — rather than by bending the production code's shape around it.

Both directions are enforced by Compze.Tests.CodePolicies' PrivateNamespaceIsolationPolicy, which scans the compiled assemblies' type references: no library may reference a type in another assembly's _private namespace, and every type in an _internal namespace must actually be referenced by another library. Both tests ignore white-box specification projects, per the paragraph above. This makes the classification self-maintaining: a library consumer appearing forces promotion _private→_internal; the last library consumer disappearing forces demotion _internal→_private.

Where the _internal/_private section sits:
* A concept with a public side keeps its machinery in an _internal/_private namespace nested BELOW the concept — `Compze.Tessaging.TessageBus._internal` — at whatever depth the public aspect lives.
* Machinery with no public face at all nests below a root _internal/_private namespace of the project.

These namespaces are obligatory. A project that has only _internal and/or _private types should have NO code outside of namespaces with _private or _internal as a section. 

For many of our project, most of the code in our projects should be in _internal or _private namespaces. 

It is FORBIDDEN to have ANY public types in any namespace where a section of the namespace is named _internal or _private. The inverse also holds: no top-level internal type lives outside an _internal/_private namespace section. Both invariants are enforced by Compze.Tests.CodePolicies.


## Internals projects and namespaces
The projects (and their associated namespaces) called Internals are NOT the same as those with sections named _internal or _private

Internals is a signal that while these projects may expose public types, these are special. They exist as utilities for use by the rest of Compze, and their APIs change faster than those of our other projects. But they are published, so what they make public is designed to the same standard as any public Compze type: see [public-types-are-designed-for-every-consumer](012-public-types-are-designed-for-every-consumer.md). Each such project will contain in its description that it is not recommended for consumers to take a direct dependency on these project, nor for them to use the types in them. The APIs may change frequently, types and/or members may be removed. Semantic versioning will be followed, but they may race through major versions, each breaking compatibility.


It is both possible and natural for an Internals project to have _internal and/or _private sections within it.

## Public is the default face
What you see in a project's root namespace is expected to be public, with the non-public implementation details hidden below _internal/_private sections or nested as private/internal types inside the public types. No namespace may contain a Public section — enforced by Compze.Tests.CodePolicies' NamespaceVisibilityPolicy, tests included.