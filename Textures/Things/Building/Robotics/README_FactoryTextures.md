# Robotics factory textures

Approved directional texture candidates for the workshop and unmanned factories.
Source previews and the generator live under `docs/robot-textures/buildings/`.

## Workshop factory (2×2)

Path: `Textures/Things/Building/Robotics/WorkshopFactory/`

| File | Size | Direction |
|---|---:|---|
| `WorkshopFactory_south.png` | 256×256 | south / front |
| `WorkshopFactory_north.png` | 256×256 | north / back |
| `WorkshopFactory_east.png` | 256×256 | east / right |
| `WorkshopFactory_west.png` | 256×256 | west / left |
| `WorkshopFactory_top.png` | 256×256 | top-down reference |

## Unmanned factory (2×6)

Path: `Textures/Things/Building/Robotics/UnmannedFactory/`

| File | Size | Direction |
|---|---:|---|
| `UnmannedFactory_south.png` | 256×256 | south / front, 2 cells wide |
| `UnmannedFactory_north.png` | 256×256 | north / back, 2 cells wide |
| `UnmannedFactory_east.png` | 768×256 | east / right, 6 cells long |
| `UnmannedFactory_west.png` | 768×256 | west / left, 6 cells long |
| `UnmannedFactory_top.png` | 256×768 | top-down reference, 2×6 |

## Integration notes

- Scale is 128 px per cell.
- No `ThingDef` currently references these textures; there is no factory building implementation in the mod yet.
- The unmanned factory's south/north textures are 2 cells wide while east/west are 6 cells long. Do not feed all four directly into a single `Graphic_Multi` with one square draw size without testing; use per-direction graphics or normalize the canvases first.
- `_top.png` files are reference/blueprint art, not part of the standard four-direction `Graphic_Multi` set.
- When the factory Defs are added, confirm the final `texPath` and drawSize in-game before shipping.
