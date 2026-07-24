# Cómo agregar música y efectos de sonido

Igual que los sprites, todo el audio se maneja desde **un solo asset**: el Banco de Audio.
El volumen se controla desde el menú de pausa (ESC) con dos barras: Música y Efectos.

## Paso 1: crear el asset (solo la primera vez)

1. Necesitas la carpeta `Assets/Resources` (si ya la creaste para los sprites, sirve la misma).
2. Click derecho sobre esa carpeta > **Create > Roguelike > Banco de Audio**.
3. Deja el nombre por defecto: **BancoAudio** (la carpeta `Resources` y el nombre exacto son lo que permite que se cargue solo).

## Paso 2: arrastrar tus clips

Selecciona el asset y arrastra tus archivos de audio (`.wav`, `.mp3`, `.ogg`) a los campos.
Lo que dejes vacío simplemente no suena, así que puedes ir agregando de a poco.

**Música (se repite en bucle, cambia sola según la escena):**

| Campo | Suena en |
|---|---|
| Musica Menu | escena `Menu` |
| Musica Nivel 1 | escena `test` |
| Musica Nivel 2 | escena `2` |

**Efectos:**

| Campo | Cuándo suena |
|---|---|
| Disparo | disparo normal |
| Disparo Rayo | rayo Brimstone |
| Disparo Granada | lanzar una granada |
| Explosion | boom de la granada |
| Jugador Dano | el jugador recibe daño |
| Enemigo Muere | muere un enemigo o jefe |
| Jefe Rayo | el jefe del nivel 2 dispara su rayo |
| Jefe Teleport | el jefe del nivel 2 se teletransporta |
| Recoger Moneda | recoger una moneda |
| Recoger Objeto | recoger un objeto del pedestal |
| Recoger Corazon | recoger un corazón |
| Abrir Puerta | (disponible por id "puerta", sin enganchar por defecto para no saturar) |

## Volumen (menú ESC)

Al presionar ESC ahora aparecen dos barras deslizables arriba de los botones:
**Música** y **Efectos**. Se guardan solas (con PlayerPrefs), así que el juego
recuerda el volumen la próxima vez que juegues.

## Reproducir un efecto desde tu propio código

Si más adelante quieres disparar un sonido desde cualquier script, es una línea:

```csharp
GestorAudio.Efecto("disparo");   // usa el id del efecto (ver tabla del banco)
```

Y para poner un efecto que no está en la lista fija, en el asset hay una sección
**Efectos extra por id**: agregas un elemento con un `id` de texto y su clip, y lo
llamas con `GestorAudio.Efecto("mi_id")`.

## Consejo de importación

Para efectos cortos (disparo, moneda): en el Inspector del archivo de audio pon
**Load Type = Decompress On Load**. Para la música, **Load Type = Streaming** y
marca **Loop** si tu clip no vuelve solo.
