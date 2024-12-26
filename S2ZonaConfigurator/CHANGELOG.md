# Changelog 

## [0.0.6]
- Add JSON mods conflict detection feature. Can be changed by setting `DetectModConflicts` in the settings file. Turned on by default
- Add `PakModsDiff` mode to be able to generate a diff report between PAK mods and the original game files
- Add `DeleteOldMods` option to be able to clean old mod files before generating new ones
- Minor fixes and improvements

## [0.0.5]
- Add a diff report generation feature. Can be turned on by setting `GenerateDiffReport` to `true` in the settings file
- Add an ability specify the file path only once in JSON mods if the file is used in multiple actions
- Fix: Parsing `[*]` structures
- Fix: Some issues when last actions may not be applied in mods where multiple files are used

## [0.0.4]
- Speed up the mods processing
- Fix: Modify multiple values method may produce the wrong indentation in a modified line by duplicating a part of the key name

## [0.0.3]
- Add a feature to copy additional files (like assets) to the output directory
- Handle more cases when the mod should be marked as failed

## [0.0.2]
- Add compression to the generated .pak file
- AES key is optional
- If target structrue was not found, then mark the mod as failed
- Don't generate the .pak if there are no mods
- Fix: Some structs can start with ': struct.begin' instead of ' : struct.begin'. Handle both cases
- Fix: appsettings files weren't fully processed because of trimming. Use source generators to fix that