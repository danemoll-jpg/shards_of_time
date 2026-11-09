# Shards of Time — sounds made with ElevenLabs (Round 4)

Made by `python Tools/make_sounds.py` (key from `ELEVENLABS_API_KEY`). Sound effects are
`Assets/Resources/Sounds/<name>.mp3` (the game loads them by name, `Audio/Sfx.cs`, and falls back to a tone made in
code when a file is missing); the music stand-in is `Assets/Resources/Music/iceage.mp3` (`Audio/Music.cs`). To replace
one: generate it again (`--only <name> --force`) or drop your own file with the same name into that folder and rebuild.

| File | Length | Kind | For | Prompt |
|---|---|---|---|---|
| `footstep_snow.mp3` | 0.5 s | sfx | a step on snow (played per step while running) | A single soft footstep crunching into fresh powder snow, close, dry, no reverb, no music. |
| `crack.mp3` | 1.5 s | sfx | ice cracking (cracked bridge planks, cracked ice breaking) | Thick ice cracking and splitting with a sharp snap and a short creak, outdoors, no music. |
| `shard.mp3` | 1 s | sfx | a small shard picked up | A bright magical crystal chime pickup sound for a family video game, two quick sparkling glassy notes rising, no music bed. |
| `necklace.mp3` | 2 s | sfx | the necklace pulses / wakes | A warm magical pulse: a deep soft glowing hum swelling with a shimmering crystal sparkle on top, gentle, fairytale, no music. |
| `growl.mp3` | 1.5 s | sfx | a wolf growls before it circles or lunges | A cartoon wolf giving a low playful menacing growl, short, close, for a family animated film, not scary, no music. |
| `wind_loop.mp3` | 12 s | loop | the Ice Age ambience under the music (loops) | Steady cold wind blowing across a wide snowy mountain valley, soft gusts, faint blowing snow, calm and continuous, no music. |
| `pound.mp3` | 1 s | sfx | Dan's ground pound landing | A heavy cartoon body slam onto packed snow and ice: a deep thud with a crunchy snow burst, punchy, no music. |
| `violinist_tune.mp3` | 6 s | sfx | the Violinist plays (he teaches a move) | A short mysterious gentle melody on a rustic wooden fiddle, a few slow lilting notes rising, warm and magical, solo instrument, music cue. |
| `iceage.mp3` | 90 s | music | the Ice Age theme (stand-in for the Suno track) | Whimsical adventurous orchestral theme for a family animated film set in the Ice Age: playful pizzicato strings, bouncy woodwinds, glockenspiel and soft brass, light percussion, a warm hopeful melody, steady 100 bpm, instrumental, seamless loop. |
