## CS2_Retakes_WeaponAllocator

WeaponsAllocator plugin for retakes written in C# (.Net) for CounterStrikeSharp

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
  }
}

```

## Setup for development

Use cmd in the current directory and run the command `dotnet restore` to install the CounterStrikeSharp API
