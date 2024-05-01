# FlyByWire SAS Mode

This Kerbal Space Program plugin adds four new SAS modes and a new more relaxing way to control your vessel, especially useful for managing your gravity turns on launch : 

![Navball](https://raw.githubusercontent.com/gotmachine/FlyByWireSASMode/master/ReadMeImages/NavballScreenshot.png)

| **Modes** | |
|:---:|---|
|![](https://raw.githubusercontent.com/gotmachine/FlyByWireSASMode/master/ReadMeImages/FlyByWire.png)  | **Fly by wire**<br/> When enabled, pitch and yaw WASD inputs don't control the vessel directly anymore, but instead control the position of a custom navball direction marker that the SAS will follow automatically. You can switch to precision mode (`Caps lock` key) for finer control.|
|![](https://raw.githubusercontent.com/gotmachine/FlyByWireSASMode/master/ReadMeImages/FlyByWirePlane.png)  | **Fly by wire (plane mode)**<br/>Identical to the fly by wire mode, but the navball marker stays at a constant position relative to the horizon.|
|![](https://raw.githubusercontent.com/gotmachine/FlyByWireSASMode/master/ReadMeImages/ParallelNeg.png)  | **AntiParallel**<br/>Available when a target is selected, will keep the vessel control part in the opposite orientation as the target. Quite useful for docking !|
|![](https://raw.githubusercontent.com/gotmachine/FlyByWireSASMode/master/ReadMeImages/ParallelPos.png)  | **Parallel**<br/>Available when a target is selected, will keep the vessel control part in the same orientation as the target.|

These new modes are available for pilots and probe cores at the same SAS level as the target and maneuver modes, but this is configurable in the ```settings.cfg``` file (doing that with a ModuleManager patch is recommended).

### Download and installation

Compatible with **KSP 1.12.3** to **1.12.5** - Available on CKAN

**Installation**

- Go to the **[GitHub release page](https://github.com/gotmachine/FlyByWireSASMode/releases)** and download the file named `FlyByWireSASMode_x.x.x.zip`
- Open the downloaded *.zip archive
- Open the `GameData` folder of your KSP installation
- Delete any existing `FlyByWireSASMode` folder in your `GameData` folder
- Copy the `FlyByWireSASMode` folder found in the archive into your `GameData` folder

### License

MIT

### Changelog

#### 1.2.0 - 01/05/2024
- Added plane mode
- Fixed a bug where the SAS would keep going toward the fly by wire direction after switching back to the stock stability assist mode.
- Put all icons in a single texture atlas

#### 1.1.0 - 28/04/2024
- Added parallel / antiparallel modes

#### 1.0.0 - 27/04/2024
- Inital release
