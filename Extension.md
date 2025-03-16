# Skynomi Extension Development Guide

## Introduction

Skynomi provides a flexible system for loading external extensions, allowing developers to add new functionality dynamically. This guide explains how to create and integrate a Skynomi extension.

## Getting Started

### Requirements

- **TShock Server** (with Skynomi installed)
- **.NET 6+**
- **Basic C# knowledge**

## Creating an Extension

### Step 1: Create a New Class

Your extension must implement the `ISkynomiExtension` interface.

```csharp
using Skynomi.Utils;
using TShockAPI;

namespace Skynomi.Extensions
{
    public class MyExtension : Loader.ISkynomiExtension
    {
        public string Name => "My Extension";
        public string Description => "A simple Skynomi extension.";
        public string Version => "1.0.0";
        public string Author => "YourName";

        public void Initialize()
        {
            TShockAPI.Commands.ChatCommands.Add(new Command("myextension.use", MyCommand, "mycommand"));
            Console.WriteLine("[Skynomi] My Extension Loaded!");
        }

        private void MyCommand(CommandArgs args)
        {
            args.Player.SendSuccessMessage("Hello from My Extension!");
        }
    }
}
```

## Implementing Additional Features

Extensions can optionally support reloading, disposing, and post-initialization.

### Reloadable Extensions

To allow reloading, implement `ISkynomiExtensionReloadable`.

```csharp
public class MyExtension : Loader.ISkynomiExtension, Loader.ISkynomiExtensionReloadable
{
    public void Reload(ReloadEventArgs args)
    {
        Console.WriteLine("[Skynomi] My Extension Reloaded!");
    }
}
```

### Disposable Extensions

To perform cleanup when unloading, implement `ISkynomiExtensionDisposable`.

```csharp
public class MyExtension : Loader.ISkynomiExtension, Loader.ISkynomiExtensionDisposable
{
    public void Dispose()
    {
        Console.WriteLine("[Skynomi] My Extension Disposed!");
    }
}
```

### Post-Initialization

If your extension requires additional steps after loading, implement `ISkynomiExtensionPostInit`.

```csharp
public class MyExtension : Loader.ISkynomiExtension, Loader.ISkynomiExtensionPostInit
{
    public void PostInitialize(EventArgs args)
    {
        Console.WriteLine("[Skynomi] My Extension Post-Initialized!");
    }
}
```

### Note

- ❌ **Don't do this**

  ```csharp
  ServerApi.Hooks.NetGreetPlayer.Register(this, OnPlayerJoin);
  ```

- ✅ **Do this**

  ```csharp
  ServerApi.Hooks.NetGreetPlayer.Register(Loader.GetPlugin(), OnPlayerJoin);
  ```

## Installing the Extension

1. **Build your extension** into a `.dll` file.
2. **Move the `.dll` file** into `ServerPlugins/` with name `Skynomi.xxx.dll`.
3. **Restart the server**

## Managing Extensions

### Listing Extensions

Use the command:

```n
/listextension
```

This displays all loaded extensions along with their details.

### Viewing Extension Details

To get more information about a specific extension:

```n
/listextension <name>
```

## Using Skynomi Cache Manager

Skynomi provides a built-in caching system that allows extensions to store and retrieve data efficiently.

### Initializing Cache

To load data from the database into the cache:

```csharp
var balanceCache = CacheManager.Cache.GetCache<long>("balances");
balanceCache.SqliteQuery = "SELECT Username AS 'Key', Balance AS 'Value' FROM Accounts;";
balanceCache.MysqlQuery = "SELECT Username AS 'Key', Balance AS 'Value' FROM Accounts;";
balanceCache.Init();
```

### Initializing Complex Cache

To load complex data from the database into the cache:

```csharp
public class TRank
{
    public int Rank { get; set; }
    public int HighestRank { get; set; }
}

var Rankcache = Skynomi.Database.CacheManager.Cache.GetCache<TRank>("Ranks");
Rankcache.MysqlQuery = "SELECT Username AS 'Key', JSON_OBJECT('Rank', Rank, 'HighestRank', HighestRank) AS 'Value' FROM Ranks";
Rankcache.SqliteQuery = Rankcache.MysqlQuery;
Rankcache.SaveMysqlQuery = "INSERT INTO Ranks (Username, Rank, HighestRank) VALUES (@key, @value_Rank, @value_HighestRank) DUPLICATE KEY UPDATE Rank = @value_Rank, HighestRank = @value_HighestRank";
Rankcache.SaveSqliteQuery = "INSERT INTO Ranks (Username, Rank, HighestRank) VALUES (@key, @value_Rank, @value_HighestRank) CONFLICT(Username) DO UPDATE SET Rank = @value_Rank, HighestRank = @value_HighestRank";
Rankcache.Init();
```

### Modifying Cache

To modify cache entries:

```csharp
var rankCache = CacheManager.Cache.GetCache<TRank>("Ranks");

// Define an updater function to increase the rank by 1
Func<TRank, TRank> increaseRank = existingRank =>
{
    existingRank.Rank += 1;
    return existingRank;
};

rankCache.Modify("player1", increaseRank);
```

### Saving Data to Cache

To store and persist data:

```csharp
balanceCache.SaveMysqlQuery = "UPDATE Accounts SET Balance = @value WHERE Username = @key";
balanceCache.Update("player1", 5000);
balanceCache.Save();
```

### Using Complex Objects in Cache

For structured data like auction items:

```csharp
var shopCache = CacheManager.Cache.GetCache<ShopItem>("auctions");
shopCache.SaveMysqlQuery = "INSERT INTO Auctions (Username, ItemId, Price, Amount) VALUES (@key, @value_ItemId, @value_Price, @value_Amount)";
shopCache.Update("player1", new ShopItem { ItemId = 1, Price = 100, Amount = 5 });
shopCache.Save();
```

### Automatic Saving

Enable automatic cache saving:

```csharp
CacheManager.AutoSave(60); // Save cache every 60 seconds
```

## Conclusion

With this guide, you can now develop powerful extensions for Skynomi, expanding its functionality dynamically. 🚀 Happy coding! 🎉
