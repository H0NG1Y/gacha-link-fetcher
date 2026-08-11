# Wuthering Waves Convene Link Fetcher

A local Windows utility that retrieves a **Convene History** link from Wuthering Waves client logs, ready for import into a tracker you trust.

## Features

- Checks common Wuthering Waves installation folders automatically
- Lets you select the game folder manually
- Reads the newest record link from `Client.log` and the WebView `debug.log`
- Supports the log obfuscation format used by current clients
- Copies the result to the clipboard with one click
- Does not connect to the internet, upload account information, or modify game files, the registry, or file permissions

## How to use

1. Open Wuthering Waves and enter any Convene banner.
2. Select **Convene History** and wait for the page to finish loading.
3. Run `鸣潮唤取链接获取器.exe`.
4. Select **Automatically get link**, then **Copy link** when it succeeds.
5. Paste the link into a tracker you trust.

If the game cannot be found automatically, select the game directory containing the `Client` folder and try again.

## Security

The link contains a temporary query credential. Do not post it publicly or send it to strangers. Paste it only into a tracker you trust.

## Files

- `鸣潮唤取链接获取器.exe`: standalone Windows app
- `鸣潮唤取链接获取器.cs`: C# source code
- `图标.ico`: app icon, based on a public Wuthering Waves website icon
- `README.md`: Chinese documentation

## Disclaimer

This is an unofficial player-made utility and is not affiliated with Kuro Games. A game update that changes log locations or formats may require an update to this tool.

[中文说明](README.md)
