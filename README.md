# Skynomi - TShock Plugin Documentation

## Overview

**Skynomi** is a TShock plugin designed to introduce a virtual economy system into your Terraria server. Players can check their balance and send currency to each other using simple commands. The plugin is perfect for servers that want to add a layer of interaction and competition through an economy-based system.

## Features

- **Economy System**: Virtual currency for transactions between players.
- **Shop System**: Players can purchase items using in-game currency.
- **Rank Progression**: Players can rank up or down, unlocking perks.
- **Admin Controls**: Easily manage player balances and settings.
- **Custom Rewards**: Configure rewards for killing NPCs or bosses.
- **Customizable Themes**: Toggle between simple and detailed UI for commands.
- **Optional Death Penalties**: Adjustable currency loss on player death.
- **Broadcast System**: Automated announcements for shop items.

## Commands

### `/balance`

**Description**: Displays the player's current virtual currency balance.

**Usage**: 
`/balance [player]`

- `[player]`: The name of the player you want to check their balance (Optional, defaults to yourself).

**Example:**
```
/balance
/balance Keyou
```

This command will return the amount of virtual currency the player currently has.

### `/pay`

**Description**: Allows a player to send virtual currency to another player.

**Usage**:
`/pay <player> <amount>`

- `<player>`: The name of the player you want to send money to.
- `<amount>`: The amount of money you want to send.

**Example**:
```
/pay Keyou 500
```

This will send 500 units of virtual currency to the player named "Keyou".

### `/shop list`

**Description**: Displays a list of items available for purchase.

**Usage**: 
`/shop list`

This command will show a list of all items that can be bought, along with their prices.

### `/shop buy`

**Description**: Allows a player to buy an item from the available list using virtual currency.

**Usage**:
`/shop buy <item> [amount]`

- `<item>`: The name of the item you want to purchase.
- `[amount]`: The quantity of the item you want to purchase (optional, defaults to 1 if not specified).

**Example:**
```
/shop buy 4444 1
```

### `/admin setbal`
**Description**: Sets the balance of a player.

**Usage**:
`/admin setbal <player> <amount>`

- `<player>`: The name of the player whose balance you want to set.
- `<amount>`: The new balance value for the player.

**Example:**
```
/admin setbal Keyou 1000
/admin setbal Keyou -500
```

### `/rank up`
**Description**: Rank up to the next level

**Usage**:
`/rank up`

### `/rank down`
**Description**: Rank down to the previous level

**Usage**:
`/rank up`

## Permissions

Set up the following permissions to control access to the plugin features:

| **Command**        | **Permission**            |
|--------------------|---------------------------|
| `/balance`         | `skynomi.balance`         |
| `/pay`             | `skynomi.pay`             |
| `/shop list`       | `skynomi.shop.list`       |
| `/shop buy`        | `skynomi.shop.buy`        |
| `/admin setbal`    | `skynomi.admin.balance`   |
| `/rank up`         | `skynomi.rank.up`         |
| `/rank down`       | `skynomi.rank.down`       |

---


## Installation

1. Download the latest version of **Skynomi**.
2. Place the `.dll` file in your server's `TShock/ServerPlugins` folder.
3. Restart your server to load the plugin.
4. Configure the plugin by editing the `Skynomi.json`, `Rank.json`, `Shop.json` file in the `tshock/Skynomi` folder.

## Configuration

In the `Skynomi.json` configuration file, you can set various options such as:
- Currency symbol and format
- Reward chance
- Theme: Simple & Detailed (not case sensitive)
- Boss & NPC base reward
- Drop on death (0 to disable)
- Reward from statue & friendly NPC

**Example:**
```json
{
  "Currency": "Skyorb",
  "Currency Format": "{currency} {amount}",
  "Reward Chance": 100,
  "Theme": "Simple",
  "Theme List": "Simple & Detailed",
  "Boss Reward": "{hp}/4*0.5",
  "NPC Reward": "{hp}/4*1.2",
  "Drop on Death": 0.5,
  "Reward From Statue": false,
  "Reward From Friendly NPC": false
}
```

In the `Shop.json` configuration file, you can set various options such as:
- Shop auto broadcast & the interval if enable
- Shop Items

**Example:**
```json
{
  "Auto Broadcast Shop": false,
  "Broadcast Interval in Seconds": 60,
  "Shop Items": {
    "1": 100,
    "2": 200,
    "3": 300
  }
}
```

In the `Rank.json` configuration file, you can set various options such as:
- Rank name
- Rank cost
- Rank reward

**Example:**
```json
{
  "Ranks": {
    "Rank1": {
      "Cost": 100,
      "Reward": {
        "1": 1,
        "2": 2
      }
    },
    "Rank2": {
      "Cost": 200,
      "Reward": {
        "1": 1,
        "2": 2
      }
    }
  }
}
```

## Changelog

**Version 1.0.0** - Initial release
- Added `balance` and `pay` commands
- Basic economy system
- Shop system

**Version 1.0.1**
- Added `shop list` and `shop buy` commands
- Added `admin setbal` command for admin
- Added `rank`, `rank up`, `rank down` commands
- Improve the economy system
- Added rank system
- Config file more readable
- Added detailed configuration options for theme customization.
- Bug fixes and optimizations.
- Introduced rank system.

## License

This plugin is licensed under the [MIT License](https://opensource.org/licenses/MIT).
