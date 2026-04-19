# Playnite Multi Account Steam Plugin

**Beta** — Core functionality is working. Expect rough edges.

## Overview

The **Playnite Multi Account Steam Plugin** adds support for managing multiple Steam accounts within a single Playnite installation. It integrates with [TcNo Account Switcher](https://github.com/TCNOco/TcNo-Acc-Switcher) to handle account switching automatically before launching games.

## Features

- **Multiple Steam Libraries** — Import owned games from each configured Steam account using the Steam Web API.
- **Installed Game Detection** — Reads local Steam library folders to mark which games are currently installed.
- **Account-Aware Launching** — Automatically switches to the correct Steam account before launching a game, then waits for Steam to be ready.
- **Install / Uninstall Support** — Triggers Steam installs and uninstalls for the correct account and monitors completion.
- **TcNo Account Switcher Integration** — Supports manual configuration or automatic download and management of the TcNo Account Switcher tool.

## Requirements

- [Playnite](https://playnite.link/) 
- [TcNo Account Switcher](https://github.com/TCNOco/TcNo-Acc-Switcher) — required for account switching (can be installed automatically via plugin settings)
- A Steam Web API key for each account — obtain one at [steamcommunity.com/dev/apikey](https://steamcommunity.com/dev/apikey)

## Setup

1. Install the plugin in Playnite.
2. Open **Settings → Plugins → Multi-Steam Library Importer**.
3. Add each Steam account with its Steam ID and Web API key.
4. Configure the TcNo Account Switcher path (or use **Automatic** mode to have the plugin install it for you).
5. Update your library — games from all accounts will appear tagged with their account name.

## Known Limitations

- Game artwork (cover images, backgrounds) is not fetched automatically — use Playnite's built-in metadata downloader after importing.
- The plugin requires a Steam Web API key per account; accounts without a key cannot have their library imported.
- Install / uninstall monitoring detects completion by polling local Steam manifests — there is no progress indicator.

## License

This project is released under the [MIT License](LICENSE).
