[![.NET](https://github.com/KeyouXZ/Skynomi/actions/workflows/dotnet.yml/badge.svg)](https://github.com/KeyouXZ/Skynomi/actions/workflows/dotnet.yml) [![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

# Skynomi - TShock Plugin Documentation

## Overview

**Skynomi** is a TShock plugin designed to introduce a virtual economy system into your Terraria server. Players can check their balance and send currency to each other using simple commands. The plugin is perfect for servers that want to add a layer of interaction and competition through an economy-based system.

## Features

- **Economy System**: Virtual currency for transactions between players.
- **Shop System**: Players can purchase items using in-game currency.
- **Rank Progression**: Players can rank up or down, unlocking perks.
- **Admin Controls**: Easily manage player balances and settings.
- **Custom Rewards**: Configure rewards for killing NPCs or bosses.
- **Optional Death Penalties**: Adjustable currency loss on player death.
- **Broadcast System**: Automated announcements for shop items.

## Commands

### Skynomi

#### `/balance`

**Description**: Displays the player's current virtual currency balance.

**Usage**:
`/balance [player]`

- `[player]`: The name of the player you want to check their balance (Optional, defaults to yourself).

**Example:**

```cmd
/balance
/balance Keyou
```

This command will return the amount of virtual currency the player currently has.

#### `/pay`

**Description**: Allows a player to send virtual currency to another player.

**Usage**:
`/pay <player> <amount>`

- `<player>`: The name of the player you want to send money to.
- `<amount>`: The amount of money you want to send.

**Example**:

```cmd
/pay Keyou 500
```

This will send 500 units of virtual currency to the player named "Keyou".

#### `/admin setbal`

**Description**: Sets the balance of a player.

**Usage**:
`/admin setbal <player> <amount>`

- `<player>`: The name of the player whose balance you want to set.
- `<amount>`: The new balance value for the player.

**Example:**

```cmd
/admin setbal Keyou 1000
/admin setbal Keyou -500
```

#### `/listextension`

**Description**: List all extensions

**Usage**:
`/listextension [name]`

- `[name]`: The name of the extension you want to list.

**Example:**

```cmd
/listextension
/listextension Shop
```

#### `/skynomi`

**Description**: Skynomi commands.

**Usage**:
`/skynomi <help>`

- `help`: Display help for Skynomi commands.

#### `/leaderboard`

**Description**: Shows a leaderboard of the richest players.

**Usage**:
`/leaderboard [top]`

- `[top]`: The number of players to show in the leaderboard (optional, defaults to 5 if not specified. Max 10).

**Example:**

```cmd
/leaderboard
/leaderboard 10
```

---

### Shop Extension

#### `/shop list`

**Description**: Displays a list of items available for purchase.

**Usage**:
`/shop list [page]`

- `[page]`: The page number to view the available items.

**Example:**

```cmd
/shop list 2
```

This command will show a list of all items that can be bought, along with their prices.

#### `/shop buy`

**Description**: Allows a player to buy an item from the available list using virtual currency.

**Usage**:
`/shop buy <item> [amount]`

- `<item>`: The name or id of the item you want to purchase.
- `[amount]`: The quantity of the item you want to purchase (optional, defaults to 1 if not specified).

**Example:**

```cmd
/shop buy 4444 1
```

#### `/shop sell`

**Description**: Allows a player to sell an item back to the shop for virtual currency.

**Usage**:
`/shop sell <item> [amount]`

- `<item>`: The name or id of the item you want to sell.
- `[amount]`: The quantity of the item you want to sell (optional, defaults to 1 if not specified).

**Example:**

```cmd
/shop sell 4444 1
```

---

### Rank System Extension

#### `/rank <command>`

**Description**: Displays rank commands.

#### `/rank up`

**Description**: Rank up to the next level

**Usage**:
`/rank up`

#### `/rank down`

**Description**: Rank down to the previous level

**Usage**:
`/rank down`

#### `/rankutils list`

**Description**: List all available ranks

#### `/rankutils <command>`

**Description**: Displays rankutils commands

**Usage**:
`/rankutils list`

### `/rankutils info`

**Description**: Displays rank details

**Usage**:
`/rankutils info <rank>`

- `<rank>`: The name of the rank you want to view

**Example:**

```cmd
/rankutils info Master
```

This command will display the details of the rank named "Master".

### `/resetrank`

**Description**: Resets rank to the lowest level

**Usage**:
`/resetrank <player/all>`

- `<player/all>`: The name of the player you want to reset their rank or "all".

**Example:**

```cmd
/resetrank Keyou
/resetrank all
```

### `/resethighestrank`

**Description**: Resets rank to the lowest level

**Usage**:
`/resethighestrank <player/all>`

- `<player/all>`: The name of the player you want to reset their rank or "all".

**Example:**

```cmd
/resethighestrank Keyou
/resethighestrank all
```

---

## Permissions

Set up the following permissions to control access to the plugin features:

| **Command**        | **Permission**            |
|--------------------|---------------------------|
| `/balance`         | `skynomi.balance`         |
| `/pay`             | `skynomi.pay`             |
| `/admin`           | `skynomi.admin`           |
| `/admin setbal`    | `skynomi.admin.balance`   |
| `/listextension`   | `skynomi.listextension`   |
| `/skynomi`         | `skynomi.skynomi`         |
| `/leaderboard`     | `skynomi.leaderboard`     |
| `/shop`            | `skynomi.shop`            |
| `/shop list`       | `skynomi.shop.list`       |
| `/shop buy`        | `skynomi.shop.buy`        |
| `/shop sell`       | `skynomi.shop.sell`       |
| `/rank`            | `skynomi.rank`            |
| `/rank up`         | `skynomi.rank.up`         |
| `/rank down`       | `skynomi.rank.down`       |
| `/rankutils`       | `skynomi.rankutils`       |
| `/rankutils list`  | `skynomi.rankutils.list`  |
| `/rankutils info`  | `skynomi.rankutils.info`  |
| `/resetrank`       | `skynomi.resetrank`       |
| `/resethighestrank`| `skynomi.resethighestrank`|

---

## Installation

1. Download the latest version of **Skynomi**.
2. Place the `.dll` file in your server's `TShock/ServerPlugins` folder.
3. Restart your server to load the plugin.
4. Configure the plugin by editing the `Skynomi.json`, `Rank.json`, `Shop.json`, and `Database.json` file in the `tshock/Skynomi` folder.

## Configuration

See [Config Document](./Config.md) for this

## Changelog

**Version 1.0.0** - Initial release

- Added `balance` and `pay` commands.
- Basic economy system.
- Shop system.

**Version 1.0.1**

- Added `shop list` and `shop buy` commands.
- Added `admin setbal` command for admin.
- Added `rank`, `rank up`, `rank down` commands.
- Improve the economy system.
- Added rank .
- Config file more readable.
- Added detailed configuration options for theme customization.
- Bug fixes and optimizations.
- Introduced rank system.

**Version 1.0.2**

- Added support for MySQL database.
- Added fallback mechanism for MySQL.
- Added wiki for all configuration files.
- Added `Ranks->{Name}->Permission` and `Use Parent for Rank` as configuration options in the rank system.
- Database query fixes.
- Bug fixes and optimizations.
- Fix rank system `rank up` logic error.
- Keep alive MySQL connection.
- `/balance` command can be accessed directly from the console.
- Added shop pagination

**Version 1.1.0**

- Add support for loading extensions from `ServerPlugins/`
- Added platform detection for better compatibility.
- Improved detailed NPC kill info display for `PC` users.
- Implemented numerical abbreviation for currency (e.g., `1,000 → 1K`).
- Fixed several bugs and stability issues.
- Removed the `theme` option from the configuration for simplification.

**Version 2.0.0**

- Added `listextension` command to list all extensions.
- The `shop system` and `rank system` are now standalone projects.
- Improve the `loader` extension capability to support custom extensions.
- Rank System: `v1.0.0` -> `v1.1.0`
  - Added `Announce Rank Up` configuration
  - Added `Enable Rank Down` configuration
  - Added `rankutils info` & `rankutils list` commands

**Version 2.1.0**

- Shop System: `v1.0.0` -> `v1.1.0`
  - Added `shop sell` command to allow players to sell items back to the shop.
  - Fix bug on `shop buy` and `shop sell` command working for amounts < 1.
- Skynomi: `v2.0.0` -> `v2.1.0`
  - Enhanced `CustomVoid` method to optionally return query results when `output` parameter is set to true.
  - Use async methods and improve error handling in Database class
  - Remove MySQL keep alive connection
- Rank System: `v1.1.0` -> `v1.1.1`
  - Fix prefix & suffix using `UpdateGroup` instead of setting it manually

**Version 3.0.0**

- Update to TShock 5.2.3
- Skynomi: `v2.1.0` -> `v3.0.0`
  - Change data type for balance (decimal -> long)
  - Break: Using CacheManager to improve performance
  - New Database configuration: `"Auto Save Interval (Seconds)"`
  - Added `skynomi` command to manage the plugin
  - Improved database query & skynomi performance
  - Remove `CustomVoidAsync`, `CustomString`, `CustomStringAsync`, `CustomDecimal`, `CustomDecimalAsync`, `AddParamaters` and all related to async method from Database class
  - Update version handling to use Version type instead of string
- Shop System: `v1.1.0` -> `v1.1.1`
  - Adjusted to be compatible with the latest version of Skynomi
- Rank System: `v1.1.1` -> `v1.1.2`
  - Adjusted to be compatible with the latest version of Skynomi
  - Improve rank system performance & fix rank system configuration bug

**Version 3.1.0**

- Skynomi: `v3.0.0` -> `v3.1.0`
  - New feature: EventManager for CacheManager
  - New feature: Logger for Skynomi (General, Info, Warning, Error, Success)
    - New Skynomi configuration: `"Log Path"`
- Rank System: `v1.1.2` -> `v1.1.3`
  - Bug fixed at creating group

**Version 3.1.1**

- Skynomi: `v3.1.0` -> `v3.1.1`
  - Remove `{Utils.Messages.Name}` from log message
- Shop System: `v1.1.2` -> `v1.2.0`
  - Added `Prefix` option for item (requested by [@HikariiiSora](https://github.com/HikariiiSora))
  - Shop items can be bought/sold under the name and id
  - Shop list now has item names

**Version 3.2.0**

- Skynomi: `v3.1.1` -> `v3.2.0`
  - Config: `"Blacklist NPC"` from reward system
  - Fix `Reward From Friendly NPC` not working
  - New command: `leaderboard`
- Rank System: `v1.1.3` -> `v1.2.0`
  - New feature: Item restriction for rank system. Config: `"Restricted Items"`
  - Corrected rankutils permission
  - New command: `resetrank` and `resethighestrank`
  - New Feature: Syncs player group with stored rank, caps rank at max if exceeded, updates cache, and notifies the player.

## License

This plugin is licensed under the [GNU General Public License v3.0](https://www.gnu.org/licenses/gpl-3.0.html).
