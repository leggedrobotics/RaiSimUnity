# Changelog

All notable changes to this project will be documented in this file.

This log file is following the guideline of [keep a changelog](https://keepachangelog.com).

## [Unreleased]

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