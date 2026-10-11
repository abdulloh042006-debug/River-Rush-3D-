# River Rush 3D — Natural Ambient Audio and Water Colors

## Implemented source modules

- `Assets/Scripts/Audio/RiverNatureAudio.cs`: three separately mixed ambient layers and tributary splash playback.
- `Assets/Scripts/Graphics/RiverColorSystem.cs`: seeded blue/cyan palettes and multi-human crowd mode.
- `Assets/Shaders/WaterDropletURP.shader`: semitransparent URP water droplets with glassy highlights.
- `Assets/Editor/RiverRushSceneBuilder.cs` creates the controller objects and assigns the droplet shader.

## Ambient sound logic

Three seamlessly repeating, procedural mono loops are created once when the race scene starts.
They contain no third-party/licensed samples:

1. **Water:** softly filtered bubbling/rushing noise, volume gently increases as the river accelerates.
2. **Wind:** low-frequency gusts that rise and fall.
3. **Forest/nature:** gentle rustling and short synthesised bird chirps; fades toward the sea.
4. **Splash:** a short one-shot when a tributary adds water volume.

The loops use 16 kHz by default, no audio mixing computations on Unity's real-time audio callback, and only three continuously playing AudioSources plus one-shots. All audio is currently *synthetic*. It is meant to create pleasant ambience, **not yet production-quality field-recorded nature realism**.

To upgrade with authentic recordings on the new laptop, assign your own **licensed** clips to
`RiverNatureAudio`'s optional `Recorded Water Loop`, `Recorded Wind Loop`,
`Recorded Forest Loop` and `Recorded Splash` fields. When an external clip is assigned,
it takes priority over procedural synthesis; only generated clips are destroyed by the component.

**Controls:** Sound ON/OFF and +/− volume buttons in lower-right corner of demo HUD. Volume and mute state persist in PlayerPrefs across restarts. No music is included.

## Color behavior (requested priority)

- **Default:** natural clean blues, turquoise, cyan and pale transparent blue glass droplets. A different seed generates subtly different shades each race. The river/ocean themselves remain believable natural blue.
- **Many people:** at **8 or more actual human participants**, all player/bot water streams receive broad randomized vibrant hues (purple, pink, green, yellow, orange, red, etc.).
- **Bots don't trigger crowd mode.** The current offline test scene uses 1 human + 5 AI bots, so the default is still natural blue.
- **Multiplayer integration later:** an authoritative session manager calls `RiverColorSystem.SetConnectedHumanCount(realConnectedPlayers)`. Networking is not implemented yet, so automatic live remote player counts are **not** available in this offline build. For visual preview of a busy race, turn on `Inspector Crowd Preview` and set `Preview Human Count` to 8 or higher, then refresh the palette.
- **Transparency:** shader opacity defaults to 0.77 for the bright human droplet and ~0.72 for other water streams. These are partial translucency, not ray-traced refraction.

### Performance

Natural mode pools and reuses only **12** URP droplet materials. Crowd mode pools **48** materials with hues spread around the spectrum. They are shared by all racer renderers to avoid a new material and draw-state cost per each of hundreds of racers. The transparent droplet shader is one pass with cheap Fresnel-like edges and no texture fetching. The environmental river/ocean use a single MaterialPropertyBlock each to vary seeded blues.

Even with the material pooling, transparency can be expensive at high overdraw. Actual Android and Windows frame-rate/thermal measurements are necessary to determine capacity. Graphics **Auto / BatterySaver / Balanced / High / Ultra** and **30/60/90/120/Native** FPS remain separately configurable.

## New laptop acceptance tests (not yet performed)

- [ ] Unity 6 URP builds and uses `WaterDropletURP` instead of pink missing-shader models.
- [ ] First run and five-bot offline demo show varied transparent blues; no rainbow.
- [ ] New race seed changes subtle river, ocean and droplet hues.
- [ ] With preview human count 8, full color spectrum appears; back at 1, natural blues return.
- [ ] Check that crowd mode does not recolor the river bed, mountain terrain or whole ocean.
- [ ] Sound layers start, gently mix and fade; no pop at loop seam.
- [ ] Pickup triggers an audible splash; all sound turns fully off when muted.
- [ ] Volume and sound state persist after exiting and reopening the game.
- [ ] Optional externally provided recorded audio clips take precedence and aren't destroyed at runtime.
- [ ] Record FPS, draw calls and overdraw on low/mid/high Android and Windows hardware.
