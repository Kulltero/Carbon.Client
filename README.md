<p align="center">
  <img src="https://codefling.com/uploads/monthly_2023_03/image.thumb.png.276343ad1b15a658368a7ae6e252172f.png" />
</p>
<hr />

We're introducing <b>Carbon for client</b>, which is a tool designed to allow developers to go wild and explore Rust client-side features and communicate with the server.

## Features

This repository is designed to allow doing as much as possible that's not player client distructive as things can go crazy very quickly. Some of the following features are planned and not yet implemented.

- Load custom content (AssetBundles); audio, 3d models, shaders, materials, textures.
- Create custom entities & item definitions which are then synchronized with the server.
- Properly clean up when disconnecting from a server.

## Downsides

Currently, the main downside is that it is basically impossible to properly protect the client & server with Anti-Cheat, since these are memory modifications which Rust's anti-cheat disallows running Rust when Carbon for client is loaded.
One other downside is the way it's shipped and the way you can enable/disable Carbon for client.

## Installation

Unzip the release patch in the root of your client. 

![image](https://github.com/CarbonCommunity/Carbon.Client/assets/22857337/abe3293f-a5cc-418c-a9ed-2b3427d65d4c)


<b>Very important to note:</b> run <b>RustClient.exe</b> and not through Steam or Rust.exe. Nothing bad will happen, it'll just not start, by throwing this error:

![image](https://github.com/CarbonCommunity/Carbon.Client/assets/22857337/dd9ea3d5-e6cd-4bf7-b700-5aa6f7e5dec0)


## Uninstallation

To be able to connect to any regular Anti-Cheat-enabled servers, all you need to do is remove the `winhttp.dll` that can be found in the Rust client folder when Carbon for client is present. I advise moving that file in any other directory than root of the client.

For a full uninstallation, all you need to do is delete the following folders/files: `BepInEx`, `.doorstop_version`, `dotnet`, `doorstop_config.ini`.
