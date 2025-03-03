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

## Installing the Extension

1. **Build your extension** into a `.dll` file.
2. **Move the `.dll` file** into `ServerPlugins/` with name `Skynomi.xxx.dll`.
3. **Restart the server**

## Managing Extensions

### Listing Extensions

Use the command:

```
/listextension
```

This displays all loaded extensions along with their details.

### Viewing Extension Details

To get more information about a specific extension:

```
/listextension <name>
```

## Conclusion

With this guide, you can now develop powerful extensions for Skynomi, expanding its functionality dynamically. 🚀 Happy coding! 🎉
