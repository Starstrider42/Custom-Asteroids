Customizing Custom Asteroids                         {#newbelts}
============

Look for a file named `asteroids.cfg` in `GameData/Starstrider42/CustomAsteroids`. If it is missing, it will be automatically generated the next time you play KSP.

`asteroids.cfg` defines where asteroids (or comets, or other small bodies) appear in the game. Each `POPULATION` block represents a single group of orbits. At present, you can't force a population to only give you certain sizes or types of asteroids. There is no limit to the number of `POPULATION` blocks you can add.

The current fields in each `POPULATION` block are the following:

* `name`: a descriptive name. At present, this is not used in-game, but this may change in future versions.
* `spawnRate`: the relative odds that a newly spawned asteroid will belong to that population. At present, 
    the absolute scale does not matter. For example, in the default file an asteroid has a 23% chance 
    (0.3/(0.3 + 1.0)) of belonging to the Near-Kerbin population, and a 77% chance of belonging to the Main 
    Belt population; replacing the numbers with 3 and 10 won't change those odds. In the future, spawnRate 
    may control the number of asteroids discovered per day.
* `smaMin`: the smallest semimajor axis an asteroid from this population may have. Currently a hard limit.
* `smaMax`: the largest semimajor axis an asteroid may have.
* `eccAvg`: the average eccentricity of an asteroid in this population. Any specific asteroid may have 
    any eccentricity; it is possible, though very unlikely, that an asteroid will even appear on an 
    unbound orbit.
* `incAvg`: the average inclination of an asteroid in this population. As with eccentricities, you 
    may occasionally get some extremely inclined orbits.
