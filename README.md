# S.T.A.L.K.E.R. 2 Zona Configurator

**S.T.A.L.K.E.R. 2 Zona Configurator** is a tool to apply JSON based mods to **S.T.A.L.K.E.R. 2: Heart of Chernobyl** game.

The application's primary goal is to simplify the process of applying mods to the game. The main disadvantages of the current modding system are:
- One mod can overwrite the changes of another mod.
- After every game update, mod makers have to update their mods to work with the new version of the game.

This tool solves these problems by extracting fresh config files from the game and packing all the mods into a single PAK file.

## Features
- Support mods in JSON format
- Automatically unpack necessary config files from the game
- Eliminate the mods conflicts by packing them into a single PAK file
- You will be warned if JSON mods have conflicts with each other
- Generate a single PAK file with all the mods
- Copy additional files like assets to the **~mods** folder
- Generate a changelog file
- Generate a diff file with the changes made by the JSON mods in a format of your choice
- Generate a diif file with the changes in pak mods

## For Users

### Installation
1. Download the latest release from the [Releases](https://github.com/dmcooller/S2ZonaConfigurator/releases) page.
2. Extract the archive to a folder of your choice.
3. Create the file `appsettings.Mine.json` (if it's not created) in the root folder of the application with the following content:
 ```json
{
  "AppConfig": {
    "Game": {
      "GamePath": "D:\\Steam\\steamapps\\common\\S.T.A.L.K.E.R. 2 Heart of Chornobyl"
    }
  }
}
  ```
    `GamePath` - path to the game folder. All the backslashes should be escaped with another backslash. So you should write `\\` instead of `\`.

Notes:
- You can use the `appsettings.json` file as a template and update any value you want in `appsettings.Mine.json`. The application will use the values from `appsettings.Mine.json` if they are present.

### Usage

1. Place your mods in the `mods` folder. It should be in JSON format, not PAK files! Some mods can be found [here](https://github.com/dmcooller/S2ZonaMods).
2. Run the application by double-clicking on the `S2ZonaConfigurator.exe` file.

You will see the output in the console. If everything is successful, the output should be something like this:
```plaintext
╔═════════════════════════════════════════════════════════════════════════════════════════════════════════
║ Processing Summary
╠═════════════════════════════════════════════════════════════════════════════════════════════════════════
║ Total Mods Processed: 10
║ Successful: 10
║ Failed: 0
╚═════════════════════════════════════════════════════════════════════════════════════════════════════════

╔═════════════════════════════════════════════════════════════════════════════════════════════════════════
║ Pak Creation Status
╠═════════════════════════════════════════════════════════════════════════════════════════════════════════
║ Pak Path: D:\Games\Stalker2Dir\Stalker2\Content\Paks\~mods\ZonaBundle.pak
║ Status: Successfully created
╚═════════════════════════════════════════════════════════════════════════════════════════════════════════
```

That's it! You can now run the game with the mods applied.

### Advanced Usage

Just some more notes on the usage:
- See `appsettings.json` for more configuration options. It can be overridden by `appsettings.Mine.json`.
- You can turn off a mod by adding `$` at the beginning of the mod file name. For example, `$super_mod.json` will be ignored
- Mods can be placed in subfolders of the `mods` folder. The application will process them all
- The application produces a changelog file in the `~mods` folder. You can turn it off by setting `OutputChangelogFile` to `false` in the `appsettings.Mine.json` file.
- The application has a feature to detect mods conflicts and it's turned on by default. You can turn it off by setting `DetectModConflicts` to `false` in the `appsettings.Mine.json` file.
- The application can delete old mods before generating new ones. You can turn it on by setting `DeleteOldMods` to `true` in the `appsettings.Mine.json` file.
It will delete all the files in the `~mods` folder starting with value specified in `OutputPakName`. The default value is `ZonaBundle`. It can be useful if you use mods with additional files like assets.

#### Diff

The application can generate a `diff` file with the changes made by the mods. You can turn it on by setting `GenerateDiffReport` to `true` in the `appsettings.Mine.json` file.

Additionally, you can change:
- The diff output format in `DiffConfig->DiffFormat` in the `appsettings.Mine.json` file. Possible values are `GitHubMarkdown`, `SideBySideMarkdown`, `Unified`, `HTML`.
- The content size of the `diff` file in `DiffConfig->ContextLines` in the `appsettings.Mine.json` file. The default value is `3`. This will keep the `diff` file small and informative. But you can set it to `-1` to include the whole file content.

#### App Mode

The application currently supports two modes:
- `Main`. This is the default mode. It will process all the mods in the `mods` folder and create a PAK file.
- `PakModsDiff`. This mode is used to compare the mods in the `pak__mods` folder with the game files. It will generate a `diff` file with the changes made by the mods. 
It can be useful to see the changes made by the pak mods. If there is a new file in the pak mod (like some asset), the app will try to search through all the files in the game's Paks folder, so it will take some time.

You can change the mode in the `appsettings.Mine.json` file `AppConfig->Options->AppMode` property. `pak__mods` directory can be changed in the `AppConfig->Paths->PakModsDirectory` property.
## For Mod Makers

If anyone wants to create a mod supported by this application, please follow the instructions below.

### Mod Format

Always verify that the changes made by the application are correct. Use any diff tool or the built-in diff feature to compare the original and modified files. 
I recommend turning off `CleanWorkDirectory` in the `appsettings.Mine.json` file to keep the modified files for further analysis and turning on `GenerateDiffReport` to generate a diff file.`

An example of a simple mod can be found in the `mods/$Example.json`. Aloso check the mods in the [S2ZonaMods](https://github.com/dmcooller/S2ZonaMods) repository.

The mod should be in JSON format. Here is an example of a simple mod:
```json
{
  "version": "1.0",
  "description": "The day is 2 hours of real time",
  "author": "JakeKingsly",
  "actions": [
    {
      "type": "Modify",
      "file": "Stalker2/Content/GameLite/GameData/CoreVariables.cfg",
      "path": "DefaultConfig::RealToGameTimeCoef",
      "value": 12,
      "defaultValue": 24
    }
  ]
 }
```

This mod will change the `RealToGameTimeCoef` value in the `CoreVariables.cfg` file to 12.

The schema of the mod file lokks like that:
- `version` - version of the mod (required)
- `description` - description of the mod (required)
- `author` - author of the mod (optional)
- `actions` - list of actions to apply (required)
    - `type` - type of the action (required)
    - `file` - path to the file to modify (required). It can be specified only in the first action of the mod. If the file is used in multiple actions, then the `file` property can be omitted in the next actions. The application will use the file path from the first action. It can be specified again if the file path should be changed aftern n actions.
    - `path` - path to the value/structure to modify (required), but optional for the `Replace` action
    - `defaultValue` - default value (optional)
    - `comment` - comment for the action (optional)

Here is an example of re-using the `file` property in multiple actions and resetting it to `Stalker2/Content/GameLite/GameData/EffectPrototypes.cfg`:

```json
{
  "version": "1.0",
  "description": "Test file path",
  "author": "",
  "actions": [
    {
      "type": "Modify",
      "file": "Stalker2/Content/GameLite/GameData/CoreVariables.cfg",
      "path": "DefaultConfig::RealToGameTimeCoef",
      "value": 12,
      "defaultValue": 24
    },
    {
      "type": "Modify",
      "path": "DefaultConfig::StartYear",
      "value": 2024
    },
    {
      "type": "RemoveStruct",
      "file": "Stalker2/Content/GameLite/GameData/EffectPrototypes.cfg",
      "path": "RadiationMechanics::MechanicsEffect::ConditionEffects::LightRadiation::ApplicableEffects"
    }
  ]
}
```

There is a special case for `path` property. In the config files, structures like `[*]` can be presented in the same parent structure multiple times. For instance:

```plaintext
GeneralNPC_Neutral_Stormtrooper_ItemGenerator : struct.begin {refurl=../ItemGeneratorPrototypes.cfg;refkey=[0]}
   SID = GeneralNPC_Neutral_Stormtrooper_ItemGenerator
   RefreshTime = 1d
   ItemGenerator : struct.begin
      [*] : struct.begin
         Category = EItemGenerationCategory::SubItemGenerator
         PossibleItems : struct.begin
            [0] : struct.begin
               ItemGeneratorPrototypeSID = GeneralNPC_Neutral_WeaponPistol
               Chance = 0.4
            struct.end
            [1] : struct.begin
               ItemGeneratorPrototypeSID = GeneralNPC_Consumables_Stormtrooper
               Chance = 1
            struct.end
         struct.end           
      struct.end
      [*] : struct.begin
         ...
      struct.end
      [*] : struct.begin
         ...
      struct.end
   struct.end
struct.end
```

In this case, the `path` property should be specified like this:
```json
{
  "type": "Modify",
  "file": "Stalker2/Content/GameLite/GameData/ItemGeneratorPrototypes/NPC_ItemGenerators.cfg",
  "path": "GeneralNPC_Neutral_Stormtrooper_ItemGenerator::ItemGenerator::[*]:0::PossibleItems::[1]::Chance",
  "value": "3"
}
```
So because we can't distinguish structures `[*]` by name, we should add the index of the structure in the path. Indexes are zero-based.

Depending on the `type` of the action, the mod can have different properties. Here are the possible types of actions:
- Modify
- Add
- RemoveLine
- RemoveStruct
- AddStruct
- Replace

#### Modify
Changes the value of the property
- `value` - new value of the property (optional if `values` is specified). Example: **"value": 12**
- `values` - one can specify a `path` to the sturcture and then specify the values to change in that structure, so that wil simplify the mod file. Example: 
```json
    {
      "type": "Modify",
      "file": "Stalker2/Content/GameLite/GameData/CoreVariables.cfg",
      "path": "DefaultConfig",
      "values": {
        "ALifeGridUpdateDelay": 6.0,
        "StartYear": 2024
      }
    }
```
#### Add
Adds a new property to the structure
- `value` - value of the new property (required). The `path` should point to the parent structure where the new property should be added and end with the property name. Example: 
 ```json
     {
      "type": "Add",
      "file": "Stalker2/Content/GameLite/GameData/CoreVariables.cfg",
      "path": "DefaultConfig::NewStuff",
      "value": 1.6
    }
 ```
 This will add a new property `NewStuff` with the value `1.6` to the `DefaultConfig` structure.

#### RemoveLine
Removes a line from the file
- `path` - one needs to specify the path to the line to remove. Example:
 ```json
    {
      "type": "RemoveLine",
      "file": "Stalker2/Content/GameLite/GameData/CoreVariables.cfg",
      "path": "DefaultConfig::StartMinute"
    }
 ```
This will remove the `StartMinute` property from the `DefaultConfig` structure.

#### `RemoveStruct`
Removes a structure from the file
- `path` - one needs to specify the path to the structure to remove. Example:
 ```json
    {
      "type": "RemoveStruct",
      "file": "Stalker2/Content/GameLite/GameData/EffectPrototypes.cfg",
      "path": "RadiationMechanics::MechanicsEffect::ConditionEffects::LightRadiation::ApplicableEffects"
    }
 ```
This will remove the `ApplicableEffects` structure from the `LightRadiation` structure.

#### AddStruct
Adds a new structure to the file. There are different ways to add a new structure:
1. Add one structure at a time:
- `value` - value of the new structure (required). The `path` should point to the parent structure where the new structure should be added and end with the new structure name. `value` should be a JSON object. Example:
    ```json
    {
      "type": "AddStruct",
      "file": "Stalker2/Content/GameLite/GameData/ItemGeneratorPrototypes/Gamepass_ItemGenerators.cfg",
      "path": "GamePass_Stash_ItemGenerator_Cheap::ItemGenerator::[0]::PossibleItems::[5]",
      "value": {
        "ItemPrototypeSID": "EArtifactFlash",
        "Chance": 0.0018,
        "MinCount": 1,
        "MaxCount": 1
      }
    }
    ```
    This will add a new array structure element to the `PossibleItems` with index 5.

2. Add multiple structures at once with the same parent. Optimized for arrays:
- `structures` - list of structures to add (required). The `path` should point to the parent structure where the new structures should be added. All the structures in the list will receive automatically generated indexes based on the last index in the parent structure. Example:
    ```json
    {
      "type": "AddStruct",
      "file": "Stalker2/Content/GameLite/GameData/ItemGeneratorPrototypes/Gamepass_ItemGenerators.cfg",
      "path": "GamePass_Stash_ItemGenerator_Cheap::ItemGenerator::[1]::PossibleItems",
      "structures": [
        {
          "ItemPrototypeSID": "EArtifactFlash",
          "Chance": 0.0018,
          "MinCount": 1,
          "MaxCount": 1
        },
        {
          "ItemPrototypeSID": "EArtifactSnowflake",
          "Chance": 0.0018,
          "MinCount": 1,
          "MaxCount": 1
        },
      ]
    }
    ```
    This will add two new array structure elements to the `PossibleItems` with automatically generated indexes. The result will be something like this:

    ```plaintext
       [1] : struct.begin
         Category = EItemGenerationCategory::Consumable
         bAllowSameCategoryGeneration = true
         PossibleItems : struct.begin
            .....
            // Last element in the array
            [4] : struct.begin
               ItemPrototypeSID = Bandage
               Weight = 3
               MinCount = 1
               MaxCount = 1
            struct.end
            // New elements
            [5] : struct.begin
               ItemPrototypeSID = EArtifactFlash
               Chance = 0.0018
               MinCount = 1
               MaxCount = 1
            struct.end
            [6] : struct.begin
               ItemPrototypeSID = EArtifactSnowflake
               Chance = 0.0018
               MinCount = 1
               MaxCount = 1
            struct.end
    ```

3. Add multiple structures at once with the same parent. Named structures:

This is similar to the previous one, but the structures will have names instead of indexes. 
- `structures` - list of structures to add (required). The `path` should point to the parent structure where the new structures should be added. The names of the structures are specified in the JSON object. Example:
    ```json
    {
      "type": "AddStruct",
      "file": "Stalker2/Content/GameLite/GameData/ItemGeneratorPrototypes/Gamepass_ItemGenerators.cfg",
      "path": "GamePass_Stash_ItemGenerator_Cheap::ItemGenerator::[1]",
      "structures": [
        {
          "ItemPrototypeFactor": {
            "ItemTag": "EItemTag::Helmet",
            "Factor": 1.5
          }
        },
        {
          "ItemPrototypeFilter": {
            "ItemTag": "EItemTag::Weapon",
            "Filter": true
          }
        }
      ]
    }
    ```
    This will add new structures with names `ItemPrototypeFactor` and `ItemPrototypeFilter` to the `ItemGenerator` with index 1.

#### Replace

Can be used to replace a certain substring in the file. This method should be used with caution. Choose the values to replace wisely, so it wont replace something that should not be replaced.
There are two ways to use it:
1. Replace by a single value:
- `value` - is a JSON object with two properties: `old` and `new`. `old` is the value to find and replace and `new` is the value to replace with. Example:
    ```json
    {
      "type": "Replace",
      "file": "Stalker2/Content/GameLite/GameData/ItemPrototypes/ArtifactPrototypes.cfg",
      "value": {
        "old": "EEffectDisplayType::EffectLevel",
        "new": "EEffectDisplayType::Value"
      }
      "comment": "Replace all EEffectDisplayType::EffectLevel with EEffectDisplayType::Value"
    }
    ```

2. Replace using regex:
- `value` - is a JSON object with two properties: `old` and `new`. `old` is the regex pattern to find and replace and `new` is the value to replace with. 
- `isRegex` - should be set to `true` to use regex. Example:
    ```json
    {
      "type": "Replace",
      "file": "Stalker2/Content/GameLite/GameData/ItemPrototypes/QuestItemPrototypes.cfg",
      "path": "",
      "value": {
        "old": "Weight\\s*=\\s*[0-9]*\\.?[0-9]+",
        "new": "Weight = 0"
      },
      "isRegex": true,
      "comment": "Replace all Weight values with 0"
    }
    ```
    This will use a regex pattern to find all the `Weight` values and replace them with `0`.

### Additional Features

#### Copy Assets

If your mod requires additional things like textures, sounds, etc., packed in .ucas, and .utoc files, you have two options to include them in the mod:

1. Create a folder with the same name as the mod file and place all the necessary files there. The application will copy all the files from the folder to the `~mods` folder.
2. Create a zip archive with the same name as the mod file and place all the necessary files there. The application will extract all the files from the archive to the `~mods` folder.

The important thing is that a JSON mod file is necessary even if it doesn't change any config files. Just create an JSON file like this:
```json
{
  "version": "1.0",
  "description": "This mod only copies assets",
  "author": "Me",
  "actions": []
}
```

Keep in mind, that it won't pack the files into the .pak, .ucas, .utoc files. It will just copy them to the `~mods` folder. But that's fine because we can't merge the assets files.