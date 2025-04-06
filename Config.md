# Currency and Rewards Configuration

This document details the configuration settings for the currency and reward system.

## General Settings

### Currency

| Field | Value |
|-------|--------|
| Type | String |
| Description | The name of the currency used in the system |
| Default | "Skyorb" |
| Example | `"Currency": "Skyorb"` |

### Currency Format

| Field | Value |
|-------|--------|
| Type | String |
| Description | Defines how currency amounts are displayed. Uses placeholders: {currency} for the currency name and {amount} for the value |
| Default | "{currency} {amount}" |
| Example | If currency is "Skyorb" and amount is 100, displays as "Skyorb 100" |

### Abbreviasi Numerik

| Field | Value |
|-------|-------|
| Type | Boolean |
| Description | Enables/disables abbreviasi numerik for currency |
| Default | false |

## Reward Settings

### Reward Chance

| Field | Value |
|-------|--------|
| Type | Number |
| Description | Percentage chance of receiving a reward (0-100) |
| Default | 100 |
| Valid Range | 0-100 |

### Boss Reward

| Field | Value |
|-------|--------|
| Type | String |
| Description | Formula for calculating boss rewards. Uses {hp} placeholder for boss HP |
| Default | "{hp}/4*0.5" |
| Example | For a boss with 1000 HP: (1000/4)*0.5 = 125 reward |

### NPC Reward

| Field | Value |
|-------|--------|
| Type | String |
| Description | Formula for calculating NPC rewards. Uses {hp} placeholder for NPC HP |
| Default | "{hp}/4*1.2" |
| Example | For an NPC with 100 HP: (100/4)*1.2 = 30 reward |

## Death and Special Rewards

### Drop on Death

| Field | Value |
|-------|--------|
| Type | Number |
| Description | Fraction of currency lost on death. Set to 0 to disable |
| Default | 0.5 |
| Valid Range | 0-1 |
| Example | 0.5 means 50% of currency is lost on death |

### Reward From Statue

| Field | Value |
|-------|--------|
| Type | Boolean |
| Description | Enables/disables rewards from statue-spawned enemies |
| Default | false |

### Reward From Friendly NPC

| Field | Value |
|-------|--------|
| Type | Boolean |
| Description | Enables/disables rewards from friendly NPCs |
| Default | false |

### Blacklist NPC

| Field | Value |
|-------|--------|
| Type | Array |
| Description | List of NPC IDs to exclude from rewards |
| Default | [] |
| Example | `[1, 2, 3]` |

## Others

### Log Path

| Field | Value |
|-------|--------|
| Type | String |
| Description | Location of log files. Leave blank to disable logging |
| Default | "tshock/Skynomi/logs" |

## Example Configuration

```json
{
  "Currency": "Skyorb",
  "Currency Format": "{currency} {amount}",
  "Abbreviasi Numerik": false,
  "Reward Chance": 100,
  "Boss Reward": "{hp}/4*0.5",
  "NPC Reward": "{hp}/4*1.2",
  "Drop on Death": 0.5,
  "Reward From Statue": false,
  "Reward From Friendly NPC": false,
  "Blacklist Npc": [],
  "Log Path": "tshock/Skynomi/logs"
}
```

---

# Shop Configuration

This document details the configuration settings for the shop system.

## Broadcast Settings

### Auto Broadcast Shop

| Field | Value |
|-------|--------|
| Type | Boolean |
| Description | Enables/disables automatic broadcasting of shop information |
| Default | false |
| Valid Values | true/false |

### Broadcast Interval in Seconds

| Field | Value |
|-------|--------|
| Type | Integer |
| Description | Time interval between shop broadcasts when Auto Broadcast is enabled |
| Default | 60 |
| Unit | Seconds |
| Minimum | 1 |

## Shop Region Settings

### Protected by Region

| Field | Value |
|-------|--------|
| Type | Boolean |
| Description | Enable/disable the use of the /shop command outside the region |
| Default | false |
| Valid Values | true/false |

### Shop Region

| Field | Value |
|-------|--------|
| Type | String |
| Description | Specifies the region where the shop is located. Only players within this region can use the shop commands |
| Default | "ShopRegion" |
| Example | "ShopRegion", "Shop" |

## Shop Items

### Shop Items Configuration

| Field | Value |
|-------|--------|
| Type | Object |
| Description | Defines items available in the shop and their prices |
| Format | Key-value pairs where key is the item ID and value is the price |

#### Item Format

```json
"Shop Items": {
    "itemID": {
        "Prefix": number,
        "Buy Price": number,
        "Sell Price": number
    }
}
```

- `itemID`: String or number representing the unique identifier for the item
- `Prefix`: Number representing the [prefix ID](https://terraria.wiki.gg/wiki/Prefix_IDs) for the item 
- `Buy Price`: Number representing the cost of the item in the configured currency
- `Sell Price`: Number representing the price at which the item can be sold back to the shop

## Example Configuration

```json
{
  "Auto Broadcast Shop": false,
  "Broadcast Interval in Seconds": 60,
  "Protected by Region": false,
  "Shop Region": "ShopRegion",
  "Shop Items": {
    "4444": {
      "Prefix": 0,
      "Buy Price": 1000,
      "Sell Price": 900
    },
    "1": {
      "Prefix": 15,
      "Buy Price": 2,
      "Sell Price": 1
    },
  }
}
```

## Usage Notes

- Shop broadcasts will only occur if `Auto Broadcast Shop` is set to true
- Broadcast interval should be set considering server performance and player experience
- Item IDs must be unique within the shop
- Prices must be positive numbers

---

# Rank System Configuration

This document details the configuration settings for the rank system.

## Structure Overview

The rank configuration uses a hierarchical structure where each rank defines its cost and associated rewards.

## Rank Configuration

### Base Structure

```json
"Use Parent for Rank": true,
"Announce Rank Up": false,
"Enable Rank Down": true,
"Ranks": {
    "rankName": {
        "Prefix": string,
        "Suffix": string,
        "Chat Color": [number, number, number],
        "Cost": number,
        "Permission": string,
        "Reward": {
            "itemID": quantity
        }
    }
}
```

### Use Parent for Rank

| Field | Value |
|-------|--------|
| Type | Boolean |
| Description | Determines if the rank should inherit properties from a parent rank |
| Default | true |
| Valid Values | true/false |

### Announce Rank Up

| Field | Value |
|-------|--------|
| Type | Boolean |
| Description | Determines if a message should be sent to all players when a player ranks up |
| Default | false |
| Valid Values | true/false |

### Enable Rank Down

| Field | Value |
|-------|--------|
| Type | Boolean |
| Description | Determines if rank down is enabled |
| Default | true |
| Valid Values | true/false |

### Fields Description

#### Rank Name

| Field | Description |
|-------|-------------|
| Type | String |
| Usage | Unique identifier for each rank |
| Example | "Rank1", "Rank2" |

#### Prefix

| Field | Description |
|-------|-------------|
| Type | String |
| Description | Text displayed before the player's name |
| Example | "[VIP]" |

#### Suffix

| Field | Description |
|-------|-------------|
| Type | String |
| Description | Text displayed after the player's name |
| Example | "[Hero]" |

#### Chat Color

| Field | Description |
|-------|-------------|
| Type | Array of Numbers |
| Description | RGB values defining the chat color for the rank |
| Example | [255, 0, 0] for red |

#### Cost

| Field | Description |
|-------|-------------|
| Type | Number |
| Description | Amount of currency required to obtain the rank |
| Must be | Positive number |

#### Permission

| Field | Description |
|-------|-------------|
| Type | String |
| Description | Permission string associated with the rank |
| Example | "rank.vip" |

#### Rewards

| Field | Description |
|-------|-------------|
| Type | Object |
| Format | Key-value pairs where key is item ID and value is quantity |
| Description | Items and quantities given when rank is obtained |

## Example Configuration

```json
{
  "Use Parent for Rank": true,
  "Announce Rank Up": false,
  "Enable Rank Down": true,
  "Ranks": {
    "Rank1": {
      "Prefix": "[i:4444] [c/FFFFFF:Adventurer] ",
      "Suffix": "",
      "Chat Color": [
        255,
        255,
        255
      ],
      "Cost": 1000,
      "Permission": "adventurer a.openchest",
      "Reward": {
        "1": 1,
        "2": 2
      }
    },
    "Rank2": {
      "Prefix": "[i:2751] [c/4B73F6:Traveler] ",
      "Suffix": "",
      "Chat Color": [
        121,
        152,
        253
      ],
      "Cost": 5000,
      "Permission": "traveler",
      "Reward": {
        "1": 1,
        "2": 2
      },
    }
  }
}
```

## Configuration Notes

- Each rank name must be unique
- Costs should increase with higher ranks
- Item IDs in rewards must be valid item identifiers
- Reward quantities must be positive numbers
- Multiple rewards can be specified for each rank
- Ranks are typically processed in order
- Higher ranks typically have better reward ratios

## Important Note

- A server restart is required after modifying the `Use Parent for Rank` setting to ensure changes take effect.
- Always backup your configuration files before making changes.
- Test the configuration in a development environment if possible.

## Best Practices

1. Use descriptive rank names
2. Maintain balanced cost progression
3. Scale rewards appropriately with rank cost
4. Keep reward quantities reasonable
5. Document item IDs used in rewards

---

# Database Configuration

This document details the configuration settings for the database system.

## Database Type Settings

### Database Type

| Field | Value |
|-------|--------|
| Type | String |
| Description | Specifies the type of database to use |
| Default | "sqlite" |
| Valid Values | "sqlite", "mysql" |

### Auto Save Interval (Seconds)

| Field | Value |
|-------|--------|
| Type | Number |
| Description | Interval in seconds between automatic saves of the database |
| Default | 600 |
| Valid Range | 0- |
| Example | `"Auto Save Interval (Seconds)": 600` |

## SQLite Configuration

### SQLite Database Path

| Field | Value |
|-------|--------|
| Type | String |
| Description | File path for the SQLite database |
| Default | "Skynomi.sqlite3" |
| Required | Only when Database Type is "sqlite" |
| Example | `"SQLite Database Path": "Skynomi.sqlite3"` |

## MySQL Configuration

### MySqlHost

| Field | Value |
|-------|--------|
| Type | String |
| Description | Host address and port for MySQL server |
| Default | "localhost:3306" |
| Required | Only when Database Type is "mysql" |
| Format | "hostname:port" |

### MySqlDbName

| Field | Value |
|-------|--------|
| Type | String |
| Description | Name of the MySQL database |
| Default | "" (empty string) |
| Required | Only when Database Type is "mysql" |

### MySqlUsername

| Field | Value |
|-------|--------|
| Type | String |
| Description | Username for MySQL authentication |
| Default | "" (empty string) |
| Required | Only when Database Type is "mysql" |

### MySqlPassword

| Field | Value |
|-------|--------|
| Type | String |
| Description | Password for MySQL authentication |
| Default | "" (empty string) |
| Required | Only when Database Type is "mysql" |

## Example Configurations

### SQLite Configuration Example

```json
{
  "Database Type": "sqlite",
  "Auto Save Interval (Seconds)": 600,
  "SQLite Database Path": "Skynomi.sqlite3",
  "MySqlHost": "localhost:3306",
  "MySqlDbName": "",
  "MySqlUsername": "",
  "MySqlPassword": ""
}
```

### MySQL Configuration Example

```json
{
  "Database Type": "mysql",
  "Auto Save Interval (Seconds)": 600,
  "SQLite Database Path": "",
  "MySqlHost": "localhost:3306",
  "MySqlDbName": "skynomi_db",
  "MySqlUsername": "skynomi_user",
  "MySqlPassword": "your_secure_password"
}
```

## Security Best Practices

1. Use strong passwords for MySQL authentication
2. Keep database credentials secure and never share them
3. Regularly backup your database
4. Use appropriate file permissions for SQLite database files
5. Consider using environment variables for sensitive information

## Configuration Notes

- Only fill in the credentials for the database type you're using
- Ensure proper network connectivity for MySQL connections
- Verify database user permissions
- Keep regular backups of your database
- Monitor database performance and storage usage
