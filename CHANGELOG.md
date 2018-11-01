# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
### Changed 
### Deprecated
### Removed
### Fixed

## [0.8.5] - 2018-10-29
### Added
- Added experimental SpaceMouse provider (Currently only supports SpaceMouse Pro)
### Fixed
- Provider DLL loading improved - PluginLoader no longer loads all DLLs in folder into Container
- Tobii Eye Tracker Provider build events missing causing provider to be absent from builds

## [0.8.4] - 2018-10-14
### Changed
- Internal build, no changes

## [0.8.3] - 2018-10-08
### Fixed
- Do not exit XI poll thread if device disconnected
- XInput LT reporting wrong scale
- Axes not being processed if previous ones were unsubscribed