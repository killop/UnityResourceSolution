# Asset Import Pipeline for Unity
[![openupm](https://img.shields.io/npm/v/com.daihenka.assetpipeline?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.daihenka.assetpipeline/)

This tool is a rule-based approach to asset postprocessors within Unity. 
The goal is develop re-usable processors that can be applied to assets when they are imported.

## Asset Import Profiles
Asset Import Profiles are `ScriptableObject`s that store information about how to import assets based on a rule based path system.
The rules consist of a base folder, asset types to process and processors that should execute on these assets when they are imported.

### Asset Types
The asset types are Textures, Models, Audio, Videos, Fonts, Animations, Materials, Prefabs, SpriteAtlases and Other.
_Other_ can be used to match against any asset that matches a specified set of file extensions.

## Path Variables
The asset import profile folder path and asset type file filters can create variables that you can use within the asset processors.

To specify a variable, wrap the variable name in `{}`.  As an example, `Assets/3DGamekit/Art/Textures/Characters/{characterName}/` will create the variable `characterName` with the folder name as a value when processors are run.  This variable can be used when setting the asset bundle name, asset label, etc.

The variables can be forced to match a string convention such as snake case or pascal case.  This can be done by adding a suffix to the variable name `{characterName:\snake}`.

| Convention        | Variable Suffix | Convention Example  |
| :---------------: | :-------------: | :-----------------: |
| No convention     | `:\none`        | The quick brown fox |
| Snake Case        | `:\snake`       | the_quick_brown_fox |
| Upper Snake Case  | `:\usnake`      | THE_QUICK_BROWN_FOX |
| Pascal Case       | `:\pascal`      | TheQuickBrownFox    | 
| Camel Case        | `:\camel`       | theQuickBrownFox    |
| Kebab Case        | `:\kebab`       | the-quick-brown-fox |
| Upper Case        | `:\upper`       | THE QUICK BROWN FOX |
| Lower Case        | `:\lower`       | the quick brown fox |

The variable suffix can also be used as basic regex. As an example if the variable should be enforced to be a number, this can be achieved by using `:\d+`.

When using the variable within a processor that supports it, you can use the string convention suffixes to convert the variable value to a specific string convention.

## Asset Processors
Asset Processors are similar asset postprocessors that are executed when an asset is imported and it matches an profile and asset filter.

There are several asset processors provided and more will be added over time.

| Processor           | Asset Types | Descriptions |
| :-----------------: | :---------: | :----------------------------------------------------------------------------------------------: |
| Apply Preset        | Textures, Models, Audio, SpriteAtlases, Fonts, Videos | Applies a preset for the asset type                    |
| Set Asset Bundle    | All         | Assigns the asset bundle name and variant                                                        |
| Set Asset Labels    | All         | Assigns labels                                                                                   |
| Add To Sprite Atlas | Textures    | Adds a sprite or texture to a SpriteAtlas and will create the SpriteAtlas if it does not exist   |
| Pack Texture 2D     | Textures    | Packs a texture into a channel of another texture                                                |
| Extract Materials   | Models      | Extracts materials from a model                                                                  |
| Setup Materials     | Models      | Sets up materials from a model - Sets shader, properties, and assigns textures if they are found |
| Strip Mesh Data     | Models      | Strips mesh data from model, such as vertex colors, other uv channels                            |
| Reset Transform     | Models      | Resets position, rotation and scale on the model root object                                     |
| Create Prefab       | Models      | Creates a prefab with the option to set layer, tag and mesh renderer(s) settings                 |

### User Data within the .meta file
Most processors will add custom user data to the meta file.  This is done to signify that it has processed the asset and prevent multiple applications of the processor to the asset when it should only run once.

### Custom Processors
Custom processors can be created by them via code.  This is done by inheriting from the `AssetProcessor` class.

The `AssetProcessorDescription` attribute will allow you to setup a custom icon and assign what asset types are viable for your processor. 
The default will make the processor available for all asset types.

Here is an example processor that will set labels on assets
```c#
using System.Linq;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;

[AssetProcessorDescription("FilterByLabel@2x")]
public class SetAssetLabels : AssetProcessor
{
    [SerializeField] string[] Labels;

    public override void OnPostprocess(Object asset, string assetPath)
    {
        var assetLabels = AssetDatabase.GetLabels(asset).ToHashSet();
        assetLabels.AddRange(labels);
        AssetDatabase.SetLabels(asset, assetLabels.ToArray());
        
        // Update the user data in the meta file
        ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
    }
}
```

## How to Use
Open the `Import Profiles` window via the `Tools > Asset Pipeline > Import Profiles` menu item.

![image](https://user-images.githubusercontent.com/6211561/115570406-5fd1c100-a2be-11eb-8046-63deaf70f3f3.png)

![image](https://user-images.githubusercontent.com/6211561/115570335-521c3b80-a2be-11eb-83a6-486bdb908c7a.png)

From here you can create Import Profiles via the `Create New` button.

Once you have created an import profile, double click on the item to open the editor for it.

![image](https://user-images.githubusercontent.com/6211561/115570637-91e32300-a2be-11eb-8b4d-352a371cd4a0.png)

Here you can specify the Base Folder and setup any Asset Filters and Processors.
