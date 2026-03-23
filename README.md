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

---

## Описание (RU)

Мод для RimWorld — сигнальный костёр для вызова племенных торговых караванов.

### Возможности

- **Сигнальный костёр** (вкладка «Разное», 70 дерева)
- Зажгите костёр, чтобы вызвать торговый караван случайной дружественной неолитовой фракции
- Требуется **200 серебра** при активации (диалог подтверждения)
- Костёр **горит с эффектами огня и дыма** до прихода каравана (~2 дня), затем уничтожается
- Нельзя разобрать пока горит
- **Общий кулдаун** (15 дней по умолчанию) на все сигнальные костры на карте
- Без очков прочности, не горит от пожаров
- Локализация: английский и русский

---

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
