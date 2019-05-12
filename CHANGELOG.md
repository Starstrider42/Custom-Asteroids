Version History                         {#changelog}
============

Custom Asteroids conforms to version 2.0.0 of the [Semantic Versioning specification](http://semver.org/spec/v2.0.0.html). 
All version numbers are to be interpreted as described therein. In addition to the actual [public API](http://starstrider42.github.io/Custom-Asteroids/), the [format of asteroid population files](http://starstrider42.github.io/Custom-Asteroids/newbelts.html) and the information stored in KSP save files will be considered part of the API for the purpose of versioning.

Version 1.7.0
------------

### New Features

* Asteroid groups can now have limits on the number of asteroids that spawn at once.

### Bug Fixes

* Solar day (<planet>.psol in configs) is now correctly calculated for bodies other than Kerbin.

Version 1.6.0
------------

### New Features

* Custom Asteroids is fully localizable.
* Asteroid group titles can now use the string "<<1>>" to say where the asteroid ID should go. Names without this string will continue to use the old behavior (where the title is a prefix for the asteroid ID).

### Bug Fixes

* Intercept orbits that enter the target planet's SOI at very low speeds are now handled correctly.
* Identical asteroids are much less likely to appear in the same game.
* Asteroids renamed through the right-click menu will no longer be deleted.

Version 1.5.0
------------

### Changes

* Support for KSP 1.3.
* Silicates, Hydrates, Gypsum, ExoticMinerals, and RareMetals added to asteroid configs. Stony and carbonaceous asteroids now have Hydrates *instead of* Water.
* Options format and file location have changed to let mods force options (e.g., asteroid lifetime) using ModuleManager. Old options files will be migrated automatically.
* All stock configs now explicitly say which probability distributions they are using. This change is purely for self-documentation; the defaults for each orbital element are still the same.

Version 1.4.1
------------

### Bug Fixes

* Exception messages are more specific about which config node is responsible for failures.

Version 1.4.0
------------

### Changes

* Support for KSP 1.2.
* Asteroid spawn rates have been roughly halved, so that there should be 10-15 asteroids if running for a long time without tracking.

### Bug Fixes

* EL compatibility patch no longer processes non-asteroid parts

Version 1.3.1
------------

### Changes

* Cleaner handling of PotatoRoid resources. Requires ModuleManager 2.6.23 or later.
* Adjusted resource amounts to make asteroid types more distinct. Completely reworked Substrate and Karborundum resources based on better understanding.

### Bug Fixes

* `CustomAsteroidPlanes` blocks now work the same in all save games.
* Asteroids will now spawn if Custom Barn Kit is installed.
* The `MetalOre` resource is now properly handled.

Version 1.3.0
------------

### New Features

* Support for KSP 1.1.
* Limited support for custom asteroid types.
* Can now customize asteroids on intercept trajectories.
* Asteroids can now appear only under certain conditions.
* Asteroid population blocks now support the log-normal, (rescaled) beta, and gamma distributions.
* Asteroid .value syntax now supports several characteristic periods and speeds.
* Asteroid orbits can now be given relative to an inclined plane. Useful for mods like RSS and Harder Solar System.

### Changes

* `DEFAULT` config blocks are now deprecated. They will be removed in version 2.0.0.
* Stockalike asteroids have been split off into their own config file.
* Some tweaks to asteroid spawn rates.

### Bug Fixes

* Invalid populations will no longer stop other populations from loading.
* Near-Kerbin asteroids will no longer appear on unbound orbits, and are much less likely to appear in the main belt.
* A large number of asteroids will no longer appear when the tracking station is upgraded while using the fixed-rate spawner.
* Mean anomaly and mean longitude of celestial bodies are now calculated properly.

Version 1.2.0
------------

### New Features

* Support for KSP 1.0.
* A prominent warning will now be displayed in-game if CustomAsteroids is installed without any asteroid configs.

### Changes

* Configs updated to reflect KSP 1.0 and popular solar system mods.
* Logs now follow the standard convention (prefixed by "[CustomAsteroids]").
* More consistent feedback for bad population definitions.
* The setting "UseCustomSpawner" has been replaced by a more flexible setting, "Spawner". Old settings files are supported and will be migrated automatically.
* The "Stock" value of "Spawner" will no longer use the KSP spawner, but an internal emulation. This change will make future improvements to the mod much easier to implement.
* The previous public API has been marked as deprecated (though it is unlikely that anyone was using it, as it was never finished). It will be removed in version 2.0.0.

### Bug Fixes

* Asteroids will now be properly removed at very high (larger than 100,000×) time warps. Players may see substantial lag as the despawner works, however.
* Asteroid groups using `orbitPhase {epoch = GameStart}` will now work correctly in RealSolarSystem.
* Asteroids will now be randomized between different save games.
* More graceful handling of invalid options files.

Version 1.1.0
------------

### New Features

* Support for KSP 0.25.
* Failed asteroid spawns will now print a brief error message to the screen. This feature is intended for troubleshooting custom asteroid configs or mod compatibility issues, and may be disabled in the [settings file](http://starstrider42.github.io/Custom-Asteroids/options.html).

Version 1.0.0
------------

### New Features

* Some asteroids will now stay on the stock KSP trajectory, which intercepts Kerbin's sphere of influence. The number of such asteroids may be changed by modifying or removing the `DEFAULT` clause in `Basic Asteroids.cfg`.
* Asteroids can now be labeled by the group they belong to. This option is on by default, but may be disabled in the [settings file](http://starstrider42.github.io/Custom-Asteroids/options.html).
* Asteroid orbital elements can now be expressed in terms of the properties of planets or moons. This reduces the amount of math the config-writer has to do, and makes config files more compatible with other solar system mods.
* Support for version checkers using Tyrope and cybutek's `.version` file format.
* Support for using Module Manager to customize downloaded configs.

### Changes

* Asteroid groups now have a unique `name` field and a human-readable `title` field, consistent with the format of most other KSP configs. **THIS BREAKS COMPATIBILITY** with the version 0.2 format.
* The position of an orbit's periapsis can now be set by constraining either the argument of periapsis or longitude of periapsis.
* Added support for Gaussian, isotropic, and exponential distributions.

Version 0.2.1
------------

### Bug Fixes

* Neither asteroids nor vessels will be corrupted when undocking from an asteroid.

Version 0.2.0
------------

### New Features

* Custom Asteroids will now scan the KSP install for asteroid configuration files. This should make it easier to personalize asteroid sets without conflicting with the mod install.
* Completely new configuration file format. The new format makes much smarter use of default settings, and the distributions assumed for each orbital element are no longer hardcoded.
* Custom Asteroids can now control all six orbital elements.
* Orbit size can be set by constraining semimajor axis, periapsis, or apoapsis. Orbit phase can be set by constraining mean anomaly or mean longitude. These two options give configuration-writers more control over where asteroids will and won't appear.

### Changes

* Stock configs now have many more minor planet groups
* Added units to config file documentation
* Reorganized code to support asteroid modifications other than orbits in future releases.

Version 0.1.0
------------
* Initial Release
