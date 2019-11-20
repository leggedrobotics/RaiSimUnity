# Changelog

All notable changes to this project will be documented in this file.

This log file is following the guideline of [keep a changelog](https://keepachangelog.com).

## [Unreleased]

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