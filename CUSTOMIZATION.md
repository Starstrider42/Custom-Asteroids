Custom(izing) Asteroids                         {#newbelts}
============

Asteroid definition files declare where asteroids (or comets, or other small bodies) appear in the game. There are two example files in the Custom Asteroids download, both in `GameData/CustomAsteroids/config/`. However, any .cfg file anywhere in `GameData` that follows the same format will be parsed by Custom Asteroids. If you are distributing asteroid definition files with your mod, please put them in your mod directory rather than the CustomAsteroids directory so that players know where the files came from. If you need to suppress or modify the default asteroid definitions, they are fully ModuleManager-compatible.

Within each file, each `ASTEROIDGROUP` block represents a single group of stable orbits. Each `INTERCEPT` block represents asteroids on a collision (or near-collision) course with a celestial body. There is no limit to the number of `ASTEROIDGROUP` or `INTERCEPT` blocks you can place in a file. `INTERCEPT` blocks are only available starting from Custom Asteroids 1.3.

Older config files may have `DEFAULT` blocks that represent asteroids on stock orbits. With the introduction of `INTERCEPT` blocks in version 1.3, `DEFAULT` blocks are deprecated and should not be used in new config files. Support for `DEFAULT` blocks will be removed in Custom Asteroids 2.0.

Basic Usage
------------

The most frequently used fields in each `ASTEROIDGROUP` block are the following:
* `name`: a unique, machine-readable name. Must not contain spaces.
* `title`: a descriptive name. If `RenameAsteroids = True` is set in the [settings file](http://starstrider42.github.io/Custom-Asteroids/options.html), 
    this name will replace the generic "Ast." in the asteroids' name.
* `centralBody`: the name of the object the asteroids will orbit. Must exactly match the name of an 
    in-game celestial body.
* `spawnRate`: must be a nonnegative number. If `Spawner = FixedRate` is set in the 
    [settings file](http://starstrider42.github.io/Custom-Asteroids/options.html), this value gives 
    the number of asteroids detected per Earth day. If `Spawner = Stock`, only the ratio to all the 
    other `spawnRate` values matters.
* `orbitSize`: a block describing how far from `centralBody` the asteroid's orbit is found. Parameters:
    - `type`: Describes which orbital element is constrained by `min` and `max`. Allowed values are 
        SemimajorAxis, Periapsis, or Apoapsis. Default is SemimajorAxis.
    - `min`: The smallest value an asteroid from this group may have, in meters. Always measured from 
        the center of `centralBody`, regardless of the value of `type`.
    - `max`: The largest value an asteroid from this group may have.
* `eccentricity`: a block describing what eccentricities an asteroid from the group may have.
    - `avg`: the average eccentricity of an asteroid in this population. Must be a nonnegative number. 
        Any specific asteroid may have any eccentricity; it is even possible, though very unlikely, 
        that an asteroid will appear on an unbound orbit.
* `inclination`: a block describing what inclinations an asteroid from the group may have.
    - `avg`: the average inclination of an asteroid in this population, in degrees. Should be a 
        nonnegative number. As with eccentricities, you may occasionally get some extreme values.

The most frequently used fields in each `INTERCEPT` block are the following:
* `name`, `title`, `spawnRate`: have the same meanings as above
* `targetBody`: the name of the object the asteroids will approach. Must exactly match the name of an 
    in-game celestial body.
* `approach`: a block describing how far from `targetBody` the asteroid would pass if not for the 
    planet's gravity. Parameters:
    - `max`: the maximum approach distance any asteroid from this group will have, in meters. The special 
        value `Ratio(&lt;targetBody&gt;.soi, 1.0)` allows all trajectories that intercept the target's 
        sphere of influence, no matter where (fill in `&lt;targetBody&gt;` with the correct name).
* `warnTime`: a block describing how long before closest approach the asteroid may be detected. Negative values are allowed and represent detections _after_ closest approach. Parameters:
    - `min`: The minimum lead time (i.e., the latest moment) at which an asteroid may be detected, 
        in seconds.
    - `max`: The maximum lead time at which an asteroid may be detected.

Advanced Usage
------------

The average number of known asteroids in each group -- if none are tracked -- will equal `spawnRate` times the average of `Options.MinUntrackedTime` and `Options.MaxUntrackedTime`. Set the value for `spawnRate` accordingly.

Each `ASTEROIDGROUP` or `INTERCEPT` block has several subfields describing how asteroid parameters are generated. Each parameter is a block with the following values:
* `dist`: the distribution from which the parameter will be drawn. Allowed values are Uniform, 
    LogUniform, Gaussian (Normal also accepted), LogNormal, Rayleigh, Gamma, Beta, or Exponential. 
    Note that the Beta distribution is rescaled from its usual interval `(0, 1)` to `(min, max)`. 
    LogNormal, Gamma, and Beta are only available in Custom Asteroids 1.3 or later.
* `min`: the minimum value of the parameter. Currently used by Uniform, LogUniform, and Beta.
* `max`: the maximum value of the parameter. Currently used by Uniform, LogUniform, and Beta.
* `avg`: the average value of the parameter. Currently used by Gaussian, LogNormal, Rayleigh, Gamma, 
    Beta, and Exponential.
* `stddev`: the standard deviation of the parameter. Currently used by Gaussian, LogNormal, Gamma, 
    and Beta.

Allowed values of `min`, `max`, `avg`, and `stddev` are:
* A floating-point number, giving the exact value (in appropriate units) for the parameter
* A string of the form 'Ratio(&lt;planet&gt;.&lt;stat&gt;, &lt;value&gt;)'. Whitespace is 
    ignored. &lt;planet&gt; is the name of a celestial body, &lt;value&gt; is a floating-point 
    multiplier, and &lt;stat&gt; is one of 
    - rad: the radius of &lt;planet&gt;, in meters
    - soi: the sphere of influence of &lt;planet&gt;, in meters
    - sma: the semimajor axis of &lt;planet&gt;, in meters
    - per: the periapsis of &lt;planet&gt;, in meters
    - apo: the apoapsis of &lt;planet&gt;, in meters
    - ecc: the eccentricity of &lt;planet&gt;
    - inc: the inclination of &lt;planet&gt;, in degrees
    - ape: the argument of periapsis of &lt;planet&gt;, in degrees
    - lpe: the longitude of periapsis of &lt;planet&gt;, in degrees
    - lan: the longitude of ascending node of &lt;planet&gt;, in degrees
    - mna0: the mean anomaly (at game start) of &lt;planet&gt;, in degrees
    - mnl0: the mean longitude (at game start) of &lt;planet&gt;, in degrees
    - prot: the sidereal rotation period of &lt;planet&gt;, in seconds
    - psol: the solar day of &lt;planet&gt;, in seconds
    - porb: the orbital period of &lt;planet&gt;, in seconds
    - vesc: the escape speed of &lt;planet&gt;, in m/s
    - vorb: the mean orbital speed, relative to the body being orbited, of &lt;planet&gt;, in m/s
    - vmin: the apoapsis orbital speed of &lt;planet&gt;, in m/s
    - vmax: the periapsis orbital speed of &lt;planet&gt;, in m/s

  For example, the string `Ratio(Jool.sma, 0.5)` means "half of Jool's semimajor axis, in meters". Time 
  and velocity stats are only available in Custom Asteroids 1.3 or later.
* A string of the form 'Offset(&lt;planet&gt;.&lt;stat&gt;, &lt;value&gt;)', where &lt;planet&gt; 
    and &lt;stat&gt; have the same meanings as above, and &lt;value&gt; is the amount to add to 
    the celestial body's orbital element (units determined by &lt;stat&gt;). Again, whitespace is 
    ignored. For example, the string `Offset(Duna.per, -50000000)` means "50,000,000 meters less 
    than Duna's periapsis", or just beyond its sphere of influence.

Each `ASTEROIDGROUP` block can have up to six parameters, corresponding to the six orbital elements:
* `orbitSize`: one of three parameters describing the size of the orbit, in meters. This is the 
    only orbital element that must *always* be given. All distances are from the body's center. 
    Distribution defaults to LogUniform if unspecified. The `orbitSize` node also has two additional 
    options:
    - `type`: may be SemimajorAxis, Periapsis, or Apoapsis. Defaults to SemimajorAxis.
    - The `min`, `max`, or `avg` fields of `orbitSize` may take a string of the form 
    'Resonance(&lt;planet&gt;, &lt;m&gt;:&lt;n&gt;)', where &lt;planet&gt; is the name of a 
    celestial body, and &lt;m&gt; and &lt;n&gt; are positive integers. The string will be interpreted 
    as the semimajor axis needed to get an m:n mean-motion resonance with &lt;planet&gt;. For 
    example, the string `Resonance(Jool, 2:3)` gives the semimajor axis to complete 2 orbits for 
    every 3 orbits of Jool -- in other words, the semimajor axis of Eeloo.
* `eccentricity`: the eccentricity of the orbit. If omitted, defaults to circular orbits. Distribution 
    defaults to Rayleigh if unspecified. If the distribution is changed to one that uses `min` and 
    `max`, these values default to the 0-1 range.
* `inclination`: the inclination of the orbit, in degrees. If omitted, defaults to uninclined orbits. 
    Distribution defaults to Rayleigh if unspecified. The `inclination` node has one additional option:
    - `dist` may take the value Isotropic, which will randomly orient the orbital plane if 
        `ascNode` is kept at its default. `min`, `max`, `avg`, and `stddev` are ignored for an 
        Isotropic distribution.
* `periapsis`: the position of the periapsis, in degrees. If omitted, allows any angle. Distribution 
    defaults to Uniform if unspecified. The `periapsis` node also has one additional option:
    - `type`: the convention for placing the periapsis. May be Argument (angle from ascending 
        node) or Longitude (absolute position). Defaults to Argument.
* `ascNode`: the longitude of the ascending node, in degrees. If omitted, allows any angle. 
    Distribution defaults to Uniform if unspecified.
* `orbitPhase`: one of two parameters describing the asteroid's position along its orbit, in degrees. 
    If omitted, allows any angle. Distribution defaults to Uniform if unspecified. The `orbitPhase` 
    node also has two additional options:
    - `type`: the convention for measuring the asteroid's progress along its orbit. May be 
        MeanAnomaly (value proportional to time since periapsis) or MeanLongitude (value 
        proportional to time since zero phase angle). Defaults to MeanAnomaly.
    - `epoch`: the time at which the mean anomaly or mean longitude is measured. May be GameStart 
        or Now. Defaults to GameStart.

Each `INTERCEPT` block can have up to three parameters:
* `approach`: the closest approach distance, in meters. This is one of two parameters that must always be 
    given. Distribution defaults to Uniform if unspecified. `min` defaults to 0 if unspecified. The 
    `approach` node has one additional option:
    - `type`: the definition of closest approach used. May be ImpactParameter (distance ignoring the 
        planet's gravity) or Periapsis (distance after allowing for planet's gravity, but ignoring the 
        SoI boundary). Defaults to Periapsis. Both types are measured from the center of the target body.
* `warnTime`: the time before closest approach, in seconds, at which the asteroid is discovered; does not 
    account for the target planet's gravity. This is one of two parameters that must always be given. 
    Distribution defaults to Uniform if unspecified. Negative values of `min` or `max` are allowed and 
    represent discoveries after closest approach.
* `vSoi`: the speed, in meters per second, at which the asteroid enters the planet's sphere of influence. 
    This indirectly controls the asteroid's eccentricity and inclination (higher approach speeds 
    correspond to eccentric, inclined orbits). If omitted, a range of speeds that allows easy capture 
    is used. Distribution defaults to LogNormal if unspecified.
