Custom(izing) Asteroids                         {#newbelts}
============

Asteroid definition files declare where asteroids (or comets, or other small bodies) appear in the game. The only such file included in the Custom Asteroids download is `GameData/Starstrider42/CustomAsteroids/config/Basic Asteroids.cfg`. However, any .cfg file that follows the same format will be parsed by Custom Asteroids.

Within each file, each `ASTEROIDGROUP` block represents a single group of orbits. At present, you can't force a group to only give you certain sizes or types of asteroids. There is no limit to the number of `ASTEROIDGROUP` blocks you can place in a file.

Basic Usage
------------

The most frequently used fields in each `ASTEROIDGROUP` block are the following:

* `name`: a descriptive name. At present, this is not used in-game, but this may change in future versions.
* `centralBody`: the name of the object the asteroids will orbit. Must exactly match the name of an 
    in-game celestial body.
* `spawnRate`: the relative odds that a newly spawned asteroid will belong to that group. Must be a 
    nonnegative number. For forward compatibility, this number should be given as asteroids detected per 
    day, but, at present, only the ratio to all the other `spawnRate` values matters.
* `orbitSize`: a block describing how far from `centralBody` the asteroid's orbit is found. Parameters:
    - `type`: Describes which orbital element is constrained by `min` and `max`. Allowed values are 
        SemimajorAxis, Periapsis, or Apoapsis.
    - `min`: The smallest value an asteroid from this group may have, in meters.
    - `max`: The largest value an asteroid from this group may have.
* `eccentricity`: a block describing what eccentricities an asteroid from the group may have.
    - `avg`: the average eccentricity of an asteroid in this population. Must be a nonnegative number. 
        Any specific asteroid may have any eccentricity; it is even possible, though very unlikely, 
        that an asteroid will even appear on an unbound orbit.
* `inclination`: a block describing what inclinations an asteroid from the group may have.
    - `avg`: the average inclination of an asteroid in this population, in degrees. Should be a 
        nonnegative number. As with eccentricities, you may occasionally get some extreme values. 
        Should be a nonnegative number.

Advanced Usage
------------

Each `ASTEROIDGROUP` block has six subfields corresponding to orbital parameters. Each orbital parameter has a block describing the distribution of that parameter:
* `dist`: the distribution from which the parameter will be drawn. Allowed values are Uniform, 
    LogUniform, or Rayleigh.
* `min`: the minimum value of the parameter. Currently used by Uniform and LogUniform.
* `max`: the maximum value of the parameter. Currently used by Uniform and LogUniform.
* `avg`: the average value of the parameter. Currently used by Rayleigh.
* `stddev`: the standard deviation of the parameter. Currently unused.

The six orbital elements are:
* `orbitSize`: one of three parameters describing the size of the orbit, in meters. This is the 
    only orbital element that must *always* be given. Distribution defaults to LogUniform if 
    unspecified. The `orbitSize` node also has an additional option:
    - `type`: may be SemimajorAxis, Periapsis, or Apoapsis. Defaults to SemimajorAxis.
* `eccentricity`: the eccentricity of the orbit. IF omitted, defaults to circular orbits. Distribution 
    defaults to Rayleigh if unspecified. If the distribution is changed to one that uses `min` and 
    `max`, these values default to the 0-1 range.
* `inclination`: the inclination of the orbit, in degrees. If omitted, defaults to uninclined orbits. 
    Distribution defaults to Rayleigh if unspecified.
* `periapsis`: the argument of periapsis, in degrees. If omitted, allows any angle. Distribution 
    defaults to Uniform if unspecified.
* `ascNode`: the longitude of the ascending node, in degrees. If omitted, allows any angle. 
    Distribution defaults to Uniform if unspecified.
* `orbitPhase`: one of two parameters describing the asteroid's position along its orbit, in degrees. 
    If omitted, allows any angle. Distribution defaults to Uniform if unspecified. The `orbitPhase` 
    node also has two additional options:
    - `type`: the convention for measuring the asteroid's progress along its orbit. May be MeanAnomaly 
        (value proportional to time since periapsis) or MeanLongitude (value proportional to time since 
        zero phase angle). Defaults to MeanAnomaly.
    - `epoch`: the time at which the mean anomaly or mean longitude is measured. May be GameStart or 
        Now. Defaults to GameStart. **WARNING:** GameStart does not work correctly with Real Solar 
        System. This issue will hopefully be fixed in the next version.
