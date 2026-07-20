Compze libraries explicitly and agressively minimize public and protected types

* Anything that is not required for a consumer of the library to be able to use it must be internal

The goals include but are not limited to: 
* Being able to keep changing how things are implemented without breaking client code.
* Minimizing the amount of documentation neded
* Minimizing the learning curve involved in adopting Compze libraries
* Minimizing the amount of testing necessary


# All types and members must have the minimal required visibility required to successfully compile and run

If Resharper reports that a type or member can be made internal,protected,private etc one of two things MUST happen
1. The visibility is minimized.
2. A black box test is written to verify the public behavior of the symbol making the inspection go away


