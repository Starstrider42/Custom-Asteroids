Version History                         {#changelog}
============

Custom Biomes conforms to version 2.0.0 of the [Semantic Versioning specification](http://semver.org/spec/v2.0.0.html). 
All version numbers are to be interpreted as described therein. Since Custom Biomes does not expose public functions, the [format of `asteroids.cfg`](@ref newbelts) will be considered the API for the purpose of versioning.

Version 0.2.0 (in development)
------------

### Changes 

* Added units to documentation
* Reorganized code to support more general asteroid modifications in future releases.

### New Features 

* Custom Asteroids will now try to upgrade `asteroid.cfg` files belonging to a previous version, inserting default values for any new parameters. This may have unintended effects if the user has upgraded to a non-backwards-compatible version.

### Bug Fixes 

Version 0.1.0
------------
* Initial Release
