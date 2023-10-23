# WACCA Launcher

Allows you to run multiple versions of WACCA from the same drive.

## Configuration

You'll want a directory structure something like this:

```
WACCA/
├── WACCALauncher.exe
├── wacca.ini
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

Each version must have segatools already configured, with a `start.bat` file present.

You'll want to create a `wacca.ini` file next to the launcher that looks something like this:

```
[general]
default_ver = reverse

[versions]
wacca = C:\WACCA\Versions\WACCA
wacca_s = C:\WACCA\Versions\WACCA S
lily = C:\WACCA\Versions\Lily
lily_r = C:\WACCA\Versions\Lily R
reverse = C:\WACCA\Versions\Reverse
offline = C:\WACCA\Versions\Offline
```

Versions can be omitted, but all paths provided must be valid.

If you have a custom version of the game you wish to launch, you may do so by adding
`num_customs = 1` under `[general]`, and then adding a block at the end of the file
that looks something like this:

```
[custom_1]
name = WACCA Omega Supermix Deluxe
path = C:\WACCA\Versions\Omega Supermix Deluxe
type = reverse

```

`type` must match one of the versions listed above, it will likely be `reverse` unless specified.

## Usage

When you start the launcher, you will be presented with a loading prompt.
You may press `TEST` or <kbd>Esc</kbd> to open the configuration menu. This menu allows you
to change the default startup version, as well as launch a version manually.
This menu is controlled in the same way you would navigate the in-game test menu.

If you do not interfere with the loading prompt, the selected default version
will be launched within 5 seconds.

## Auto-launch

You can configure your cab to start the launcher instead of explorer.exe when you log in.
**This should only be done on a cab. You have been warned.**

If you've installed the launcher in the suggested location of `C:\WACCA`,
you can run `SetShell.reg` to reconfigure your registry accordingly. 

If you have not installed the launcher in the suggested location, you will need to edit the
registry file before you apply it. **This is important, if the path is invalid, you may get
softlocked on a black screen at next boot!**

## Notice

This code was never intended to be public, as such, it is a bit of a dumpster fire.
If you want to help make the code a little less awful, issues and pull requests are welcome.

## License

[MIT License](https://choosealicense.com/licenses/mit/)