# FlyByWireSASMode

This Kerbal Space Program plugin adds a new **fly by wire** SAS mode.

When enabled, pitch and yaw inputs don't control the vessel directly anymore, but instead control the position of a custom navball direction marker that the SAS will follow automatically.

Additionally, this also add two target related modes, **parallel** and **antiparallel**, especially useful for docking.

![Navball](https://raw.githubusercontent.com/gotmachine/FlyByWireSASMode/master/NavballScreenshot.png)


These addtional modes are available for pilots and probe cores at the same SAS level as the target and maneuver modes, but this is configurable in the ```settings.cfg``` file (doing that with a ModuleManager patch is recommended).

### Download and installation

Compatible with **KSP 1.12.0** to **1.12.5** - Available on [CKAN]

**Installation**

- Go to the **[GitHub release page](https://github.com/gotmachine/FlyByWireSASMode/releases)** and download the file named `FlyByWireSASMode_x.x.x.zip`
- Open the downloaded *.zip archive
- Open the `GameData` folder of your KSP installation
- Delete any existing `FlyByWireSASMode` folder in your `GameData` folder
- Copy the `FlyByWireSASMode` folder found in the archive into your `GameData` folder

### License

MIT

### Changelog

#### 1.1.0 - 28/04/2024
- Added parallel / antiparallel modes

#### 1.0.0 - 27/04/2024
- Inital release