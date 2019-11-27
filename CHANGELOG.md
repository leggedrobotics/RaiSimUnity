# Changelog

All notable changes to this project will be documented in this file.

This log file is following the guideline of [keep a changelog](https://keepachangelog.com).

## [Unreleased]

## [0.2.2] - 2019-11-27

### Added
- Added body frame markers (r/g/b arrow). Frame markers are scaled by UI sliders.
- Server hibernating mode added. If the server is hibernating, wait until server awake.

### Changed
- Configuration number for visuals. If configuration number does not match, reinitialize visuals. This only works with RaiSim commit >= 0455fb54e54c3d3fa471ef40e67b97a228b31d35 
- If the screen size is changed while recording, terminate video recording 
- Video is now saved in Screenshot directory.
- Erased redundant default materials in order to reduce git lfs bandwidth.      

### Fixed 
- We do not need configuration number for contact update. Line for getting configuration number is removed. This only works with RaiSim commit >= 0455fb54e54c3d3fa471ef40e67b97a228b31d35
- Screen size now can be changed.

## [0.2.1] - 2019-11-22

### Added
- Contact point markers (red sphere) and contact force arrows (blue arrow) are now scaled by UI sliders.

### Changed
- Empty root objects are renamed to start with _ (underbar). 
- Sidebar UI is aligned by Vertical Layout Group. It is easier to add more UI elements now. 

### Fixed
- Contact force visualization bug fixed. Contact frame was not considered. This works together with RaiSimServer bug fix 62192fbf67502cc563a338643340ff665a9b536f. 

## [0.2.0] - 2019-11-20

### Added 
- New CHANGELOG.md for tracking release versions.  
- Orbiting around selected objects by mouse left-click.

### Changed
- Mesh import from AssimpNet-based [com.donghok.meshimporter](https://github.com/eastskykang/UnityMeshImporter) Unity package.  
- Initialize in multiple Update() iteration. To prevent freezing GUI.

### Removed
- Previous runtime mesh importers (obj, dae, stl) are ditched.

## [0.1.0] - 2019-11-14

### Added 
- First test release. Visualizing RaiSim simulation scene by TCP/IP communication.