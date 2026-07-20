In order to make it easy for a reader to follow the structure of the code we use namespace and project name conventions

## Private and Internal namespaces

* Code that should never be used directly by a consumer of a library goes in namespaces called:
  * Internal: For code that may be shared with other parts of compze using InternalsVisibleTo
  * Private: For code that should never be used by any other project even if it may be technically visible due to InternalsVisibleTo. todo: consider enforcing this through nsdepcop or our CodePolicies project

These namespaces are obligatory. A project that has only Internal and/or Private types should have NO code outside of namespaces with Private or Internal as a section. 

For many of our project, most of the code in our projects should be in Internal or Private namespaces. 

It is FORBIDDEN to have ANY public types in any namespace where a section of the namespace is named Internal or Private: Enforce through our CodePolicies project


## Internals projects and namespaces
The projects (and their associated namespaces) called Internals are NOT the same as those with sections named Internal or Private

Internals is a signal that while these projects may expose public types, these are special. They are not truly designed for public use, but as utilities for use by the rest of Compze. We do not put the same effort and thought into the design of the types in these projects that we do into our other projects. Each such project will contain in its description that it is not recommended for consumers to take a direct dependency on these project, nor for them to use the types in them. The APIs may change frequently, types and/or members may be removed. Semantic versioning will be followed, but they may race through major versions, each breaking compatibility.


It is both possible and natural for an Internals project to have Internal and/or Private sections within it.

Note: It is possible that some existing "Internals" projects should simply be named Internal and not expose anything publicly. Review is required.

Note: The name Internals is also up for debate and may well be changed to make the meaning clearer and reduce potential confusion with Internal.

## Public namespaces
Public namespaces are a legacy of an old strategy. They should all be removed. Public is the default face of a project and what you see in it's root folder is expected to be public, with the non-public implementation details hidden away in other namespaces, or nested as private/internal types inside the public types