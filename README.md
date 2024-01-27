## cs2-retakes-weapon-allocator

WeaponsAllocator plugin for retakes written in C# (.Net) for CounterStrikeSharp

## Retakes

This plugin is made to run alongside B3none's retakes implementation: https://github.com/b3none/cs2-retakes

## Config

The config file will be generated automaticly by the plugin and will be located where the plugin is.

The DBConnection will be generated empty and the plugin will raise an exception, make sure to update it or update the one provided with the release.

### Example Config

```
{
  "DBConnection": {
    "Host": "<HOST>",
    "Database": "<DB>",
    "User": "<USER>",
    "Password": "<PASSWORD>",
    "Port": 3306
  },
  "PREFIX": {
    "PREFIX": " \u0004[Retakes]\u0001",
    "PREFIX_CON": "[Retakes]",
  },
  "ArmorAmount": 100
}

```

## Setup for development

Use cmd in the current directory and run the command `dotnet restore` to install the CounterStrikeSharp API
