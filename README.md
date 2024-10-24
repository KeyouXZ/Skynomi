# Skynomi - TShock Plugin Documentation

## Overview

**Skynomi** is a TShock plugin designed to introduce a virtual economy system into your Terraria server. Players can check their balance and send currency to each other using simple commands. The plugin is perfect for servers that want to add a layer of interaction and competition through an economy-based system.

## Features

- Players can check their balance using a simple command.
- Allows players to send money to other players via the `pay` command.
- Virtual currency system that integrates smoothly with TShock.
- Simple shop system with broadcasting system

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

## Permissions

To use the Skynomi commands, players must have the appropriate permissions set up in the server.

- **`balance` permission**: `skynomi.balance`
- **`pay` permission**: `skynomi.pay`
- **`shop` permission**: `skynomi.shop`
- **`shop buy` permission**: `skynomi.shop.buy`
- **`shop list` permission**: `skynomi.shop.list`
- **`admin` permission**: `skynomi.admin`
- **`admin setball` permission**: `skynomi.admin.balance`


## Installation

1. Download the latest version of **Skynomi**.
2. Place the `.dll` file in your server's `TShock/ServerPlugins` folder.
3. Restart your server to load the plugin.
4. Configure the plugin by editing the `Skynomi.json` file in the `tshock/` folder.

## Configuration

In the `Skynomi.json` configuration file, you can set various options such as:
- Currency symbol and format
- Shop auto broadcast & the interval if enable
- Shop Items

**Example:**
```json
{
  "Currency": "Skyorb",
  "Auto Broadcast Shop": false,
  "Broadcast Interval in Seconds": 60,
  "Shop Items": {
    "1": 100,
    "2": 200,
    "3": 300
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
- Config file more readable

## License

This plugin is licensed under the [MIT License](https://opensource.org/licenses/MIT).
