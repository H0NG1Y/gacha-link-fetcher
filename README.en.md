<div align="center">

# Gacha Link Fetcher

**Local link discovery, history sync, backups, exports, and analysis**

[![GitHub](https://img.shields.io/badge/GitHub-181717?logo=github&logoColor=white)](https://github.com/H0NG1Y/gacha-link-fetcher)
[![Stars](https://img.shields.io/github/stars/H0NG1Y/gacha-link-fetcher?color=yellow&label=stars&logo=github)](https://github.com/H0NG1Y/gacha-link-fetcher/stargazers)
[![Downloads](https://img.shields.io/github/downloads/H0NG1Y/gacha-link-fetcher/total?color=orange&label=downloads)](https://github.com/H0NG1Y/gacha-link-fetcher/releases/latest)
[![Windows Download](https://img.shields.io/github/v/release/H0NG1Y/gacha-link-fetcher?color=brightgreen&label=Windows%20Download&logo=windows&logoColor=white)](https://github.com/H0NG1Y/gacha-link-fetcher/releases/latest)
[![C#](https://img.shields.io/badge/C%23-.NET-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Supported Games](https://img.shields.io/badge/Supported%20games-4-5C6BC0)](https://github.com/H0NG1Y/gacha-link-fetcher#supported-games)

</div>

## About

A local Windows application that discovers gacha-history links from game logs or caches. After your explicit confirmation, it queries the corresponding game's official history API. Synced data stays on your PC and can be backed up, restored, exported, and analysed.

## Supported games

| Game | History page |
| --- | --- |
| Wuthering Waves | Convene History |
| Genshin Impact | Wish History |
| Honkai: Star Rail | Warp Records |
| Zenless Zone Zero | Signal Search Records |

## Download

Download the latest version from [GitHub Releases](https://github.com/H0NG1Y/gacha-link-fetcher/releases/latest).

- `GachaLinkFetcher-Setup-vVERSION.exe` is recommended. It installs to `C:\Program Files\GachaLinkFetcher` and creates a Start menu entry, uninstall information, and an optional desktop shortcut.
- For portable use, download `GachaLinkFetcher-vVERSION.exe` instead.
- Each release also includes `.sha256` and `checksums.txt` files for SHA-256 verification.

## Usage

1. Open any banner and its history page in the target game.
2. Run the EXE and select the correct game.
3. Select **Automatically get link**. If needed, select the game directory manually.
4. To save and analyse records, select **Sync records**, read the prompt, and confirm the official API request.
5. Choose **All banners** or a specific banner, then view, analyse, or export the filtered records.

## Features

- Checks common installation directories, including official launcher, WeGame, Steam, and Epic paths for Wuthering Waves
- Reads the latest history link from local logs or `webCaches`
- Downloads every available page for every known banner only after user confirmation, then merges by game, account, shared-pity banner group, and record ID
- Displays correct banner names; banners sharing pity are merged into one selectable group for the table, analytics, and exports
- Automatic local backups (latest 20 retained), plus manual backup and restore
- CSV, Excel-compatible SpreadsheetML XML, and generic JSON export
- UIGF v4 export for Genshin Impact, Honkai: Star Rail, and Zenless Zone Zero
- Per shared-pity banner group totals, rarity counts, current pity, and average top-rarity interval
- Privacy-safe diagnostic summary with no link, account, or file path
- A standard 64-bit installer: application files go to Program Files, while records, settings, backups, and downloaded updates stay under `%LocalAppData%\GachaLinkFetcher`
- Background GitHub Release check on each real cold start; only the first startup check can show a new-version prompt, while later EXE launches activate the existing window and expose a versioned update button
- Built-in update downloader that retrieves release metadata and checksums directly from GitHub and verifies the installer with SHA-256
- Direct GitHub downloads, automatic or manual live nodes from `github.akams.cn`, and a remembered custom acceleration URL

## Data and privacy

- Link discovery only reads local logs or caches. Links remain in memory or the clipboard and are never written to the local data file.
- The app contacts an official history API only after you choose **Sync records** and approve the confirmation prompt. It uses the temporary credential in the current link for that request.
- Local logs, caches, and backups are never uploaded. Synced records are stored only in `%LocalAppData%\GachaLinkFetcher\records.json`.
- Backups are stored in `%LocalAppData%\GachaLinkFetcher\backups` and can be deleted by the user.
- Release metadata and SHA-256 files are always fetched directly from GitHub. Acceleration nodes receive only the public installer URL; the updater neither requires nor forwards a GitHub token.
- The application does not modify game files and runs without administrator rights. The standard installer requests elevation only when installing or updating Program Files, shortcuts, and Windows uninstall information.

> A gacha-history link contains a temporary query credential. Never post it publicly, send it to strangers, or include it in an issue.

## FAQ

**No link was found?**

Open the in-game history page and wait for it to load, then try again. If it still fails, manually choose the actual game directory.

**Sync failed or the link expired?**

History credentials expire. Reopen the in-game history page, retrieve a new link, and retry. A game-side API change can also require an application update.

**Why is the Excel export an XML file?**

It is a native SpreadsheetML workbook that Excel opens directly, without bundling additional dependencies.

**Update checks or downloads fail?**

Try **GitHub direct (default)** first. If GitHub is unreachable on the current network, refresh the live nodes and choose automatic or manual acceleration, or enter your own HTTPS acceleration URL. A previously used custom URL is stored locally.

**How do I verify an installer manually?**

Run `Get-FileHash .\GachaLinkFetcher-Setup-vVERSION.exe -Algorithm SHA256` in PowerShell and compare it with the matching `.sha256` file or `checksums.txt` from the same release.

## Source layout

- `Models/`: game definitions, gacha records, and local data models
- `Services/`: link discovery, official API requests, exports, and analytics
- `Storage/`: local JSON data and backups
- `UI/`: WinForms main interface
- `installer/`: Inno Setup installer definition
- `Build-Release.ps1`: versioned portable executable, installer, and SHA-256 artifact build

## Disclaimer

This is an unofficial player-made tool and is not affiliated with Kuro Games, miHoYo, or HoYoverse. Game updates that change logs, locations, or history APIs may require an update.

[中文说明](README.md)
