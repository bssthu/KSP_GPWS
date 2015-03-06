# KSP_GPWS
A mod for Kerbal Space Program.

Inspired from 
http://forum.kerbalspaceprogram.com/threads/55408-0-24-Dev-slowdown-Nagging-Nadia-v0-13-beta by SolarLiner, 
and http://forum.kerbalspaceprogram.com/threads/43134-0-22-GPWS-for-Jets-Planes-v1-2-Hear-yourself-land!-or-crash!
by Cryphonus.

------

Add warning sounds for Kerbal Space Program.

GPWS means "ground proximity warning system", a terrain awareness and alerting system.
The mod also adds TCAS ("traffic collision avoidance system") warning.

ref:
- http://www51.honeywell.com/aero/common/documents/Mk_V_VII_EGPWS.pdf
- http://www.boeing-727.com/Data/systems/infogpws.html

------

Development thread: http://forum.kerbalspaceprogram.com/threads/112420-WIP-0-90-GPWS-Warning-System-for-Planes

![UI](http://i.imgur.com/t980Na2.png)

#### How to Use

Fly an aircraft with landing gear(s).
(It should have a ModuleLandingGear, FSwheel or ModuleWheel module.)
If you enabled system in GPWS Settings,
You should hear sounds when you are landing/crashing.

You can edit GPWS.cfg to add more types of landing gear.

This mod adds a button on blizzy78's toolbar / applaunch toolbar.
Click it to open GUI to edit settings or view current warning status.
You are free to turn off warnings you don't want to hear.

You can edit settings.cfg for more accurate adjustment.

P.S.1. This mod uses feet for altitude.

P.S.2. Change "Descent Rate Factor" to allow faster/slower sink rate. (Set to 2 means you are allowed to sink 2 times faster than default.)

#### Supported Warning List

- sink rate
- sink rate, whoop whoop pull up
- terrain, terrain
- terrain, terrain, whoop whoop pull up
- don't sink
- too low gear
- too low terrain
- bank angle
- altitude callout (1000, 500, approaching minimus, minimus, 100, 50, 40, 30, 20, 10)
- traffic

#### Download

https://github.com/bssthu/KSP_GPWS/releases

#### Installation

Just copy the contents of the archive to KSP's root folder.

You need ModuleManager to make it work.
If you don't have one, you can get it from
http://forum.kerbalspaceprogram.com/threads/55219-0-90-Module-Manager-2-5-10-%28Feb-13%29-Including-faster-cats

#### Credits

- sarbian for ModuleManager
- cybutek for KSP-AVC
- blizzy78 for Toolbar

------

![CC-BY-NC-SA 4.0](https://i.creativecommons.org/l/by-nc-sa/4.0/88x31.png)

This work is licensed under a Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
http://creativecommons.org/licenses/by-nc-sa/4.0/
