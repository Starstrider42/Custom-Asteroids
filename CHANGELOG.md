Version History                         {#changelog}
============

Custom Biomes conforms to version 2.0.0 of the [Semantic Versioning specification](http://semver.org/spec/v2.0.0.html). 
All version numbers are to be interpreted as described therein. Since Custom Biomes does not expose public functions, the [format of asteroid population files](@ref newbelts) will be considered the API for the purpose of versioning.

Next Version (in development)
------------

### Changes 

### New Features 

* Some asteroids will now stay on the stock KSP trajectory, which intercepts Kerbin's sphere of influence. The fraction of such asteroids may be changed by modifying or removing the `DEFAULT` clause in `Basic Asteroids.cfg`.
* Asteroids can now be labeled by the group they belong to. This option is on by default, but may be disabled by setting `RenameAsteroids = False` in `GameData/Starstrider42/CustomAsteroids/PluginData/Custom Asteroids Settings.cfg`.
* Asteroid orbital elements can now be expressed in terms of the orbital elements of planets or moons. This reduces the amount of math the config-writer has to do, and makes config files more compatible with other solar system mods.

### Bug Fixes 

Version 0.2.0
------------

### Changes 

* Added units to documentation
* Reorganized code to support more general asteroid modifications in future releases.

### New Features 

* Custom Asteroids will now scan the KSP install for asteroid configuration files. This should make it easier to personalize asteroid sets without conflicting with the mod install.
* Completely new configuration file format. The distributions assumed for each orbital element are no longer hardcoded.
* Custom Asteroids can now control all six orbital elements.
* Orbit size can be set by constraining semimajor axis, periapsis, or apoapsis. Orbit phase can be set by constraining mean anomaly or mean longitude. These two options give configuration-writers more control over where asteroids will and won't appear.

Version 0.1.0
------------
* Initial Release
