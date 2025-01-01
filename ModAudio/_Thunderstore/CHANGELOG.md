# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2025-Jan-01

### Added

- You can now specify multiple audio files per source clip in __routes.txt. ModAudio will play one at random, based on their weights.
- Each clip in __routes.txt can now specify a weight using the format `source = replacement / weight` (where weight is a decimal number, i.e. `1.0`). This is used for random rolls when there are multiple clips present.
- You can now use the `___default___` identifier when replacing audio. This allows you to include the vanilla audio as a clip that should be played randomly.
- Added an `OverrideCustomAudio` option that can be used to override any custom audio from mods with whatever you specify in ModAudio itself.
- `__routes.txt` now supports line comments. Lines starting with `#` will be treated as a comment and ignored.

## [1.0.4] - 2024-Dec-28

### Changed

- Improved audio loading from root folder, it will now also load audio from root if there's at least one clip with a known vanilla name

## [1.0.3] - 2024-Dec-27

### Changed

- The mod will now load assets from mods that don't have a DLL plugin. It will load any audio from the "audio" folder (i.e. `BepInEx/plugins/Your-Mod/audio`) if it exists. It will also load audio from the root folder (`BepInEx/plugins/Your-Mod/`) if there is a `__routes.txt` file present

### Fixed

- Fixed an issue related to using the mod without any audio under `ModAudio/audio`

## [1.0.2] - 2024-Dec-23

### Changed

- Removed NAudio dependency in favor of UnityWebRequestMultimedia

### Fixed

- Audio played via `AudioSource.PlayOneShot` should now be replaced correctly

## [1.0.1] - 2024-Dec-23

### Added

- Option to enable verbose logging of audio sources that are being replaced, and audio sources that are loaded in

### Changed

- Improve reliability of audio replacement when scenes are loaded

## [1.0.0] - 2024-Dec-22

### Changed

**Initial mod release**