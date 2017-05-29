Custom Asteroids Options                         {#options}
============

When it is first run, Custom Asteroids creates a config file in `GameData/CustomAsteroids/Custom Asteroids Settings.cfg`. This file may be used to toggle the following options:

* `RenameAsteroids`: if True (the default), asteroids will be named by the group they belong to. If False, asteroids will keep their default names.
* `Spawner`: may be set to FixedRate (the default) or Stock. In FixedRate mode, asteroids will be discovered at a steady rate. In Stock mode, new asteroids will quickly appear any time there are no untracked asteroids.
* `ErrorsOnScreen`: if True (the default), any errors or incompatibilities in asteroid config files will be displayed on screen as soon as they are detected. If False, errors will be logged but otherwise unreported.

* `MinUntrackedTime`: the minimum number of Earth days an asteroid can stay untracked before it disappears. Must be nonnegative.
* `MaxUntrackedTime`: the maximum number of Earth days an asteroid can stay untracked before it disappears. Must be positive, and must be no less than `MinUntrackedTime`.

* `VersionNumber`: the plugin version for which the options were written. **DO NOT CHANGE THIS**. Custom Asteroids uses this field for backward compatibility support.
