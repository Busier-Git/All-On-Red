# Cómo agregar sprites al juego (Banco de Sprites)

Todo el arte del juego ahora se maneja desde **un solo asset**: el Banco de Sprites.
No hay que tocar ninguna escena ni ningún script — arrastras los sprites una vez
y funcionan en el nivel 1 y en el nivel 2.

## Paso 1: crear el asset (solo la primera vez)

1. En la ventana **Project**, crea la carpeta `Assets/Resources` (click derecho sobre Assets > Create > Folder, nómbrala exactamente `Resources`).
2. Click derecho sobre esa carpeta > **Create > Roguelike > Banco de Sprites**.
3. Deja el nombre por defecto: **BancoSprites** (importante: la carpeta `Resources` y el nombre `BancoSprites` son lo que permite que se cargue solo).

## Paso 2: arrastrar tus sprites

Selecciona el asset y en el Inspector verás todos los campos. Cualquier campo que
dejes **vacío** seguirá usando el cuadro de color de siempre, así que puedes ir
agregando arte de a poco.

| Campo | Qué es |
|---|---|
| Enemigo Perseguidor | El rojo que te persigue |
| Enemigo Disparador | El naranjo que te sigue y dispara |
| Enemigo Diagonal | El morado que deambula y dispara en X |
| Enemigo Torreta | El celeste fijo que dispara con línea de visión |
| Jefe Nivel 1 | El jefe de los sprays |
| Jefe Nivel 2 | El Adversario (rayo + teletransporte) |
| Roca / Caja | Obstáculos (roca indestructible, caja destructible) |
| Suelo / Pared | Se repiten en mosaico (tiled) por toda la sala |
| Decoraciones | Lista de detalles (manchas, grietas, huesos...) que se reparten al azar por el piso de cada sala |
| Puerta Abierta / Cerrada / Peaje / Muro | Estados de las puertas (la de peaje es la dorada del nivel 2) |
| Corazón | El pickup de vida |
| Granada / Explosión | El proyectil del Lanzagranadas y su boom |
| Portal | El portal al siguiente nivel |
| Pedestal Base | La base donde flota el objeto |
| Objetos | Sprite por id de objeto (ver tabla de ids abajo) |

### Ids de los objetos

Para la lista **Objetos**, agrega un elemento, escribe el id y arrastra el sprite:

`dano_up`, `vel_up`, `cadencia_up`, `doble`, `quad`, `granadas`, `brimstone`, `sagrado`

## Cosas que se ajustan solas

- **Tamaño**: no importa la resolución ni el Pixels Per Unit del sprite; el juego
  lo escala automáticamente al tamaño que corresponde (un enemigo mide lo mismo
  con tu sprite que con el cuadro de color).
- **Colliders**: se reajustan para coincidir con el sprite.
- **Puertas**: el sprite se estira para llenar exactamente el hueco de la puerta.
- **Suelo y pared**: se repiten en mosaico; usa sprites que "tileen" bien.

## Lo que se edita en otro lado (ya era fácil)

- **Bala del jugador**: prefab `Assets/Scripts/Prefabs/Proyectil.prefab` (cambia el sprite ahí).
- **Bala enemiga**: prefab `EnemyBullet.prefab`.
- **Moneda**: prefab `Coin.prefab`.
- **Jugador**: su sprite/animaciones viven en el objeto Player de la escena (Animator).
- **Enemigos con prefab propio**: si asignas prefabs en `Prefabs Enemigos` del
  Generador, se usan tal cual con su propio arte (el Banco solo alimenta a los
  enemigos generados por código).

## Consejos de importación

Para pixel art: al importar el sprite, en el Inspector del archivo pon
**Filter Mode = Point (no filter)** y **Compression = None** para que se vea nítido.
