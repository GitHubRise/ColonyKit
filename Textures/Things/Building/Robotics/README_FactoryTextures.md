# Robotics factory textures

Approved directional texture candidates for the five robotics factories.
Source previews and the generators live under `docs/robot-textures/buildings/`.

All sets use 128 px per cell and include south / north / east / west plus a top-down reference.

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

## Frame factory (2×2)

Path: `Textures/Things/Building/Robotics/FrameFactory/`

| File | Size | Direction |
|---|---:|---|
| `FrameFactory_south.png` | 256×256 | south / front |
| `FrameFactory_north.png` | 256×256 | north / back |
| `FrameFactory_east.png` | 256×256 | east / right |
| `FrameFactory_west.png` | 256×256 | west / left |
| `FrameFactory_top.png` | 256×256 | top-down reference |

Open truss hall with a central hoist, exposed frame jig, two welding arms and side material racks.

## Two-station factory (4×3)

Path: `Textures/Things/Building/Robotics/TwoStationFactory/`

| File | Size | Direction |
|---|---:|---|
| `TwoStationFactory_south.png` | 512×256 | south / front, 4 cells wide |
| `TwoStationFactory_north.png` | 512×256 | north / back, 4 cells wide |
| `TwoStationFactory_east.png` | 384×256 | east / right, 3 cells long |
| `TwoStationFactory_west.png` | 384×256 | west / left, 3 cells long |
| `TwoStationFactory_top.png` | 512×384 | top-down reference, 4×3 |

Twin sawtooth roofs, independent amber/cyan station doors, a shared tool tower and a conveyor spine.

## Prefab factory (3×3)

Path: `Textures/Things/Building/Robotics/PrefabFactory/`

| File | Size | Direction |
|---|---:|---|
| `PrefabFactory_south.png` | 384×256 | south / front |
| `PrefabFactory_north.png` | 384×256 | north / back |
| `PrefabFactory_east.png` | 384×256 | east / right |
| `PrefabFactory_west.png` | 384×256 | west / left |
| `PrefabFactory_top.png` | 384×384 | top-down reference, 3×3 |

Yellow gantry crane, roof container stacks, a hanging prefab module, input containers and output pallets.

## Integration notes

- No `ThingDef` currently references these textures; there is no factory building implementation in the mod yet.
- Directional canvases intentionally differ by footprint: do not feed a 2-wide front, a 6-long side and a 2×6 top into one `Graphic_Multi` with a single square draw size without testing. Use per-direction graphics or normalize the canvases first.
- `_top.png` files are reference / blueprint art, not part of the standard four-direction `Graphic_Multi` set.
- When factory Defs are added, confirm the final `texPath` and drawSize in-game before shipping.
