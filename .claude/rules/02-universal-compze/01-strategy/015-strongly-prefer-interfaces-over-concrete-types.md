The moment we expose a class, we are locked in to that single implementation. If we expose only an interface the implementation can change freely without breaking client code.

-If an interface will do, do not expose a class
  - Instances can be created through static factory methods on the interfaces. Needing constructors is no longer an argument to use classes.

# Don't bend the design over backwards to use interfaces though

Value objects, exceptions, abstract composition roots etc. are good candidates for using classes, records etc.


