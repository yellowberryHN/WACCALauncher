# WACCALauncher

Allows you to run multiple versions of WACCA from the same drive.
Also allows you to run arbitrary programs as well.

## Recommended Folder Structure

You'll want a directory structure something like this:

```
WACCA/
├── _profiles/
│   ├── WACCA.json
│   ├── Reverse.json
│   ├── OSD.json
│   └── ...
├── WACCALauncher.exe
├── launcher.json
├── ...
└── Versions/
    ├── WACCA/
    │   ├── bin/
    │   └── ...
    ├── WACCA S
    ├── Lily
    ├── Lily R
    ├── Reverse
    ├── Offline
    └── Omega Supermix Deluxe
```

## Profiles

A profile is an entry in the list of profiles displayed in the launcher.
It lets you define launch options for whatever you're launching.

Profiles are .json files, you can have as many profiles in your profiles folder as you want.
One limitation to be aware of is if you have a profile that shares a name with another one,
weird things happen. But I'm not sure why you would want that anyway.

Profiles are validated when the launcher starts, and will throw an error if the configuration is invalid,
or if the configured files or folders do not exist.

The filename of a profile is only used for sorting, so you can sort your profiles however you wish.

There are currently only 2 valid types of profiles:
- `WACCA`, meant for launching WACCA versions
- `Generic`, meant for launching arbitrary programs

### WACCA Profiles

A WACCA profile contains information about where key game files are, and how to launch the game.

Each WACCA profile must have segatools already configured in the `bin` folder.

Here's an example of what a WACCA Reverse profile would look like:

```json
{
	"Name": "Reverse",
	"Type": "WACCA",
	"BasePath": "C:\\WACCA\\Versions\\Reverse",
	"GamePath": "WindowsNoEditor\\Mercury\\Binaries\\Win64\\Mercury-Win64-Shipping.exe",
	"Configs": [
		"config.json",
		"config_lan_install_server.json",
		"config_region_jpn.json"
	]
}
```

### Generic Profiles

A generic profile can be used to launch pretty much any program.

Here's an example of a profile you can use to launch Portal 2, for some reason:

```json
{
	"Name": "Portal 2 (why?)",
	"Type": "Generic",
	"BasePath": "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Portal 2",
	"GamePath": "portal2.exe"
}
```

### Updaters

Profiles have optional support for an update executable/script to be run before running the game itself.

Updaters can be added to any type of profile, and have a few specific behaviors to look out for. Updaters
are run from the directory they live in, not from the `BasePath` directory, for consistency reasons.

To add an updater to a profile, simply provide the full path to the updater in the `UpdaterPath` key.
If you need to pass arguments to the updater, you may use the `UpdaterArgs` key, but this may be omitted
if not needed.

```json
{
	"Name": "Omega Supermix Deluxe",
	"Type": "WACCA",
	"BasePath": "C:\\WACCA\\Versions\\Omega Supermix Deluxe",
	"GamePath": "WindowsNoEditor\\Mercury\\Binaries\\Win64\\Mercury-Win64-Shipping.exe",
	"Configs": [
		"config.json",
		"config_lan_install_server.json",
		"config_region_jpn.json"
	],
	"UpdaterPath": "C:\\WACCA\\Versions\\Omega Supermix Deluxe\\bin\\updater\\wupdate.exe",
	"UpdaterArgs": "-t OSD"
}
```

You may skip the updater for the default profile by holding the Volume Up button while launching.
The `...` will turn into `!!!` while you are holding it.

## Usage

When you start the launcher, you will be presented with a loading prompt.
You may press the `TEST` button or <kbd>Esc</kbd> to open the launcher menu.
This menu allows you to change the default startup profile, as well as launch a profile manually.
It is controlled in the same way you would navigate the in-game test menu.

You can also perform various tasks helpful for cab management, like open File Explorer, reload profiles, 
or reboot your cab without a hard power cycle.

If you do not interfere with the loading prompt, the selected default profile
will be launched within 5 seconds.

## Watchdog

By default, the process watchdog is enabled. WACCALauncher runs in the background while you are playing,
and if it detects the game closed in some way, it will clean up and attempt to open the game again,
with the same 5-second grace period as the initial launch. You may interrupt this by going into the
launcher menu, as before.

If this behavior is undesired, it can be disabled by setting `UseWatchdog` to false in `launcher.json`.

## Auto-launch

You can configure your cab to start the launcher instead of the Windows Desktop when you log in.
**This should only be done on a cab. You have been warned.**

If you've installed the launcher in the suggested location of `C:\WACCA`,
you can run `SetShell.reg` to reconfigure your registry accordingly. 

If you have not installed the launcher in the suggested location, you will need to edit the
registry file before you apply it. **This is important, if the path is invalid, you may get
softlocked on a black screen at next boot!**

## Notice

This code was originally never intended to be public, as such, it is a bit of a dumpster fire.

As of version 0.12, I've cleaned it up a bit, but there's still a lot of work to be done.
If you want to help make the code a little less awful, issues and pull requests are welcome.

## License

[MIT License](https://choosealicense.com/licenses/mit/)