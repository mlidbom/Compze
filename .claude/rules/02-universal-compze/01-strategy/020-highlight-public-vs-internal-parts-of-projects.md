In order to make it easy for a reader to follow the structure of the code we use namespace and project name conventions

## _internal and _private namespaces

* Code that should never be used directly by a consumer of a library goes in namespaces called:
  * _internal: For code that IS shared with other parts of compze using InternalsVisibleTo — a type no other assembly consumes does not belong here; it goes under _private.
  * _private: For code that must never be used by any other project even if it may be technically visible due to InternalsVisibleTo.

Why the lowercase underscore form: these sections are not domain concepts — they are visibility machinery, in effect language extensions making up for the access modifier C# lacks. The `_lowercase` spelling makes them look like what they are (keyword-like markers, not PascalCase domain words), keeps them visually distinct from real namespaces, and sorts them before normal namespaces in IDE project views and file explorers — the same signal `_docs` folders already carry.

Both directions are enforced by Compze.Tests.CodePolicies' PrivateNamespaceIsolationPolicy, which scans the compiled assemblies' type references: no assembly may reference a type in another assembly's _private namespace, and every type in an _internal namespace must actually be referenced by another assembly. This makes the classification self-maintaining: a foreign consumer appearing forces promotion _private→_internal; the last consumer disappearing forces demotion _internal→_private.

Where the _internal/_private section sits:
* A concept with a public side keeps its machinery in an _internal/_private namespace nested BELOW the concept — `Compze.Tessaging.TessageBus._internal` — at whatever depth the public aspect lives.
* Machinery with no public face at all nests below a root _internal/_private namespace of the project.

These namespaces are obligatory. A project that has only _internal and/or _private types should have NO code outside of namespaces with _private or _internal as a section. 

For many of our project, most of the code in our projects should be in _internal or _private namespaces. 

It is FORBIDDEN to have ANY public types in any namespace where a section of the namespace is named _internal or _private. The inverse also holds: no top-level internal type lives outside an _internal/_private namespace section. Both invariants are enforced by Compze.Tests.CodePolicies (the allowlists have burned to zero and stay there).


## Internals projects and namespaces
The projects (and their associated namespaces) called Internals are NOT the same as those with sections named _internal or _private

Internals is a signal that while these projects may expose public types, these are special. They are not truly designed for public use, but as utilities for use by the rest of Compze. We do not put the same effort and thought into the design of the types in these projects that we do into our other projects. Each such project will contain in its description that it is not recommended for consumers to take a direct dependency on these project, nor for them to use the types in them. The APIs may change frequently, types and/or members may be removed. Semantic versioning will be followed, but they may race through major versions, each breaking compatibility.


It is both possible and natural for an Internals project to have _internal and/or _private sections within it.

Note: The Internals projects were reviewed 2026-07-21. SystemCE and Logging (+Logging.Serilog) are deliberately publishing-worthy — external consumers use them accepting the churn. Serialization.Newtonsoft and the Sql family were found to be consumer-facing backend packages wearing the wrong label and were promoted to Compze.Serialization.Newtonsoft and Compze.Sql.* (Sql.Common fully dark behind InternalsVisibleTo). Compze.Internals.Testing's fate (promote the performance-testing harness vs go dark) is still an open decision.

Note: The name Internals is also up for debate and may well be changed to make the meaning clearer.

## Public namespaces are gone — public is the default face
An old strategy marked the public side with namespaces named Public. That is inverted now: what you see in a project's root namespace is expected to be public, with the non-public implementation details hidden below _internal/_private sections or nested as private/internal types inside the public types. No namespace may contain a Public section — enforced by Compze.Tests.CodePolicies' NamespaceVisibilityPolicy, tests included.