# Changelog 

## [0.0.2]
- Add compression to the generated .pak file
- AES key is optional
- If target structrue was not found, then mark the mod as failed
- Don't generate the .pak if there are no mods
- Fix: Some structs can start with ': struct.begin' instead of ' : struct.begin'. Handle both cases
- Fix: appsettings files weren't fully processed because of trimming. Use source generators to fix that