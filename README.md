# TradingSignalsHSK

RimWorld mod — signal campfire to call tribal trade caravans.

## Features

- **Signal campfire** building (Misc tab, 70 wood to build)
- Light the fire to call a trade caravan from a random non-hostile Neolithic faction
- Requires **200 silver** payment on activation (confirmation dialog)
- Campfire **burns with fire/smoke effects** until the caravan arrives (~2 days), then auto-destroys
- Cannot be deconstructed while burning
- **Shared cooldown** (default 15 days) across all signal fires on the map
- No hit points, no flammability
- English and Russian localization

## Compatibility

- RimWorld 1.4 / 1.5 / 1.6
- No dependencies

## Build

```
dotnet build -c Release
```

Output: `Assemblies/TradingSignalsHSK.dll`

## Author

[linyaDev](https://github.com/linyaDev)

## License

MIT
