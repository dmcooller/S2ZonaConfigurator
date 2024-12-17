# S.T.A.L.K.E.R. 2 Zona Configurator

**S.T.A.L.K.E.R. 2 Zona Configurator** is a tool to apply JSON based mods to **S.T.A.L.K.E.R. 2: Heart of Chernobyl** game.

The application's primary goal is to simplify the process of applying mods to the game. The main disadvantages of the current modding system are:
- One mod can overwrite the changes of another mod.
- After every game update, moders need to update their mods to work with the new version of the game.

This tool solves this problem by extracting fresh config files from the game and packing all the mods into a single PAK file.

## Features
- Support mods in JSON format
- Automatically unpack necessary config files from the game
- Eliminate the mods conflicts by packing them into a single PAK file
- Generate a single PAK file with a changelog file (changelog is optional)

## For Users

### Installation
1. Download the latest release from the [Releases](https://github.com/dmcooller/S2ZonaConfigurator/releases) page.
2. Extract the archive to a folder of your choice.
3. Create the file `appsettings.Mine.json` (if it's not created) in the root folder of the application with the following content:
 ```json
{
  "AppConfig": {
    "Game": {
      "GamePath": "C:\\Users\\user\\repos\\personal\\GameFolder",
      "AesKey": "0x33A604DF49A07FFD4A4C919962161F5C35A134D37EFA98DB37A34F6450D7D386"
    }
  }
}
  ```
    `GamePath` - path to the game folder

    `AesKey` - encryption key for the PAK file. You don't need to touch it, but if the devs change it, you can update it here.

Notes:
- You can use the `appsettings.json` file as a template and update any value you want in `appsettings.Mine.json`. The application will use the values from `appsettings.Mine.json` if they are present.

### Usage

1. Place your mods in the `mods` folder. It should be in JSON format, not PAK files!
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
- You can turn off a mod by adding `$` at the beginning of the mod file name. For example, `$super_mod.json` will be ignored
- Mods can be placed in subfolders of the `mods` folder. The application will process them all
- The application produces a changelog file in the `~mods` folder. You can turn it off by setting `OutputChangelogFile` to `false` in the `appsettings.Mine.json` file. (See the `appsettings.json` file for more details)

## For Mod Makers

If anyone wants to create a mod supported by this application, please follow the instructions below.

### Mod Format

Always verify that the changes made by the application are correct. Use any diff tool to compare the original and modified files. I recommend turning off CleanWorkDirectory in the `appsettings.Mine.json` file to keep the modified files for further analysis.

An example of a simple mod is in the `mods/$Example.json`.

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
    - `file` - path to the file to modify (required)
    - `path` - path to the value/structure to modify (required)
    - `defaultValue` - default value (optional)
    - `comment` - comment for the action (optional)

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