# Gacha Link Fetcher

A local Windows utility that retrieves gacha-history links from local logs or caches for Wuthering Waves, Genshin Impact, Honkai: Star Rail, and Zenless Zone Zero.

## Download

Download the latest version from [GitHub Releases](https://github.com/lllusorysky/wuthering-waves-convene-link-fetcher/releases/latest).

## Features

- Supports Wuthering Waves, Genshin Impact, Honkai: Star Rail, and Zenless Zone Zero
- Automatically detects supported games, or lets you choose one in the app
- Checks common installation folders; Wuthering Waves retains official launcher, WeGame, Steam, and Epic detection
- Reads the newest link from Wuthering Waves logs or miHoYo/HoYoverse `webCaches`
- Copies the result to the clipboard with one click
- Does not connect to the internet, upload account information, or modify game files, the registry, or file permissions

## How to use

1. Enter any banner in the target game and open its history page:
   - Wuthering Waves: **Convene History**
   - Genshin Impact: **Wish History**
   - Honkai: Star Rail: **Warp Records**
   - Zenless Zone Zero: **Signal Search Records**
2. Wait for the page to finish loading, then run the EXE from Releases.
3. Choose a game (or keep automatic detection) and select **Automatically get link**.
4. Select **Copy link** and paste it into a tracker you trust.

For Wuthering Waves, WeGame installations are detected under `WeGameApps\\rail_apps` and `WeGameApps\\apps` on each drive.

If automatic detection fails, choose the actual game directory containing the game EXE or a `*_Data` folder. Do not choose only the launcher directory.

## Security

The link contains a temporary query credential. Do not post it publicly or send it to strangers. Paste it only into a tracker you trust.

## Files

- [Releases](https://github.com/lllusorysky/wuthering-waves-convene-link-fetcher/releases): compiled Windows EXE downloads
- `GachaLinkFetcher.cs`: C# source code
- `图标.ico`: app icon, based on a public Wuthering Waves website icon
- `README.md`: Chinese documentation

## Disclaimer

This is an unofficial player-made utility and is not affiliated with Kuro Games, miHoYo, or HoYoverse. A game update that changes log locations or formats may require an update to this tool.

[中文说明](README.md)

[![GitHub Stars](https://img.shields.io/github/stars/lllusorysky/wuthering-waves-convene-link-fetcher?style=social)](https://github.com/lllusorysky/wuthering-waves-convene-link-fetcher/stargazers)
