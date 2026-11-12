# Shards of Time — music

**Rule (Dan, 2026-11-09):** level songs (and other full-length tracks) are
Dan's. Code writes the Suno prompts here; Dan makes the tracks, chooses the take
he likes and saves it at the path given. Code never generates level music,
stand-ins included. Short music (stings, the Violinist's tune) stays with Code
via ElevenLabs.

**Direction (Dan, 2026-11-09): whimsical and cartoony, not orchestral or
serious.** Light, bouncy, playful, a little silly, like a classic cartoon:
plucky strings, woodwinds, xylophone, tuba, music box. **No whistling or slide whistle (Dan, 2026-11-09): it made tracks sound alike.** Give each world its own instrument palette so the songs don't blur together. Avoid
epic, heroic, sweeping orchestral, dark or dramatic. Every new music prompt
follows this (and avoids words Suno reads as artist names, e.g. "strongman"; use "Muscle Man") (texture vocals like oohs / aahs / hallelujahs are fine where they fit, Dan 2026-11-10; no real sung lyrics unless he asks), except the vortex (mysterious, not quirky; Dan 2026-11-09).

## The Ice Age theme (Suno, for Dan) — Round 4

**In the game (Round 5): Dan's Suno track** `Assets/Resources/Music/iceage.mp3` (3:00; Code's ElevenLabs stand-in is gone and
`make_sounds.py` no longer makes level music). It starts with the arrival cutscene and loops in the valley (the last 3 s
crossfade into the start, `Assets/Scripts/Audio/Music.cs`), so it doesn't need a perfect loop point.

Suno prompt (instrumental, 2-3 minutes):

> Bouncy, whimsical, cartoony theme for a funny animated family adventure set in
> a snowy Ice Age valley. Light and playful, not serious or epic: plucky
> pizzicato, a goofy tuba or bassoon bass line, cheeky clarinet and xylophone
> melody, glockenspiel sparkles like snowflakes, slide whistle and woodblock
> accents, light toy-like percussion, a bouncing pulse around 110 bpm.
> Mischievous, sunny, a little silly, like a classic cartoon chase that never
> gets scary. No vocals. Steady energy from start to end (no long intro, no
> fade-out) so it loops.

Style tags: `whimsical, cartoon, playful, quirky, pizzicato, xylophone, clarinet, tuba, bouncy, instrumental`.

Under the music: the wind ambience (`Assets/Resources/Sounds/wind_loop.mp3`, ElevenLabs). Volume: Settings > Music volume.


## Where each track plays (Round 5, `Game.MusicFor` in `Assets/Scripts/Core/Game.cs`)

| File | Plays | Notes |
|---|---|---|
| `title.mp3` (3:00) | the title screen | crossfades to Act I on New game |
| `act1.mp3` (59.9 s, Round 9) | `ActOne` (60 s) from its start | fades out over 2.2 s from 58.4 s, over the white-out into the Ice Age arrival (the Ice Age theme starts on the landing) |
| `iceage.mp3` (3:00) | from the arrival, then the valley (loops) | the wind ambience under it |
| `vortex.mp3` (3:00) | inside the vortex space (loops) | crossfades in when the party enters, back to the Ice Age when it leaves |
| `pirate.mp3` (2:59, Round 10) | `PirateArrival`, `ShardOverboard`, then the ship and the island (loops) | the sea's lapping (`waves_loop`) under it |
| `underwater.mp3` (3:00, Round 10) | under the sea: once the player's head has been under the surface 0.6 s (back to `pirate.mp3` 1.2 s after surfacing) | crossfades both ways; silence under `SeaVortex` |

A missing file is silence (nothing is generated in its place).

## Act I cutscene (Suno, for Dan) — planning chat, 2026-11-09

`ActOne` is **19 s**. Beats (seconds into the scene): 0–4.8 cozy Christmas
evening; 4.8 Kira spots the ornament; 7.1 she swipes it; **9.1 it shatters**;
12.5 the vortex opens; 16.2 they're pulled in; 19 white-out. Save Dan's pick as
`Assets/Resources/Music/act1.mp3`. The game plays it from its start and fades it
out at the white-out, so the first ~19 s are what count (Dan may trim it to the
part he likes). `IceAgeArrival` then starts the Ice Age theme.

Suno prompt (instrumental):

> Slow, gentle, cozy Christmas music for a heartwarming animated holiday
> film. A tender, carol-like melody at a relaxed tempo (about 70 bpm) on music
> box and celesta, with soft sleigh bells, warm pizzicato and a light felt
> piano, like a quiet Christmas Eve by a glowing tree with snow falling
> outside. Sweet and a little whimsical, never fast or busy. Near the end the
> same melody turns softly magical: twinkling chimes and a slow shimmering
> swell of bells and harp, still gentle and Christmassy. No vocals, no drums.

Style tags: `christmas, slow, gentle, cozy, music box, celesta, sleigh bells, lullaby, whimsical, instrumental`.
Exclude: `fast, zany, orchestral, epic, cinematic, drums, rock`.

## Title screen — main theme (Suno, for Dan) — planning chat, 2026-11-09

The game's main theme: plays on the title screen and loops. Different from the
Act I cue and the Ice Age theme. Instrumental, 2–3 minutes. Save Dan's pick as
`Assets/Resources/Music/title.mp3`. The game crossfades the end into the start,
so it doesn't need a perfect loop point.

Suno prompt (instrumental):

> Catchy, whimsical, cartoony main theme for a funny animated time-travel
> adventure about a couple and their mischievous cat chasing the shards of a
> magic Christmas ornament through history. Light and bouncy, not serious or
> epic: a hummable, cheeky melody on clarinet, xylophone and pizzicato
> strings, a goofy oom-pah tuba bass, a ticking-clock woodblock pulse, a
> music-box twinkle for the magic ornament, a hint of sleigh bells, and playful
> slide whistles and cartoon accents. Fun, sunny, a little silly, full of
> wonder. No vocals. Starts strong without a long intro.

Style tags: `whimsical, cartoon, playful, quirky, bouncy, xylophone, clarinet, tuba, music box, family adventure, instrumental`.

## The vortex — travel hub (Suno, for Dan) — planning chat, 2026-11-09

Plays inside the vortex space while the player chooses a path; loops. **Not
quirky (Dan): mysterious.** Still gentle and family-friendly, never scary.
Instrumental, 2–3 minutes. Save Dan's pick as `Assets/Resources/Music/vortex.mp3`.

Suno prompt (instrumental):

> Mysterious, dreamy, floating ambient music for a magical time vortex in an
> animated family adventure. Slow and spacious, about 70 bpm: soft
> shimmering pads, gently swirling harp and glass-like chimes, a quiet
> ticking-clock pulse far away, a soft music-box motif drifting in and out like
> a half-remembered memory, airy wordless choir in the distance. Curious and
> wondrous, suspended in time, a little melancholy, never scary or dark. No
> vocals, no drums. Steady from start to end (no long intro, no fade-out) so it
> loops.

Style tags: `mysterious, ambient, dreamy, ethereal, magical, harp, chimes, music box, slow, instrumental`.
Exclude: `quirky, cartoon, upbeat, drums, epic, horror, dark`.

## Ancient Egypt theme (Suno, for Dan) — planning chat, 2026-11-09

Egypt's level song: plays in the world and loops. Whimsical and cartoony like
the Ice Age theme, with an Egyptian flavor. Instrumental, 2–3 minutes. Save Dan's
pick as `Assets/Resources/Music/egypt.mp3`.

**In (Dan, 2026-11-09): `egypt.mp3`, 3:00.** Plays from the Egypt arrival on and everywhere in Egypt (the tomb too);
the vortex crossfades to `vortex.mp3` as in the Ice Age (`Game.MusicFor`). Checked playing in the exe (muted run).

Suno prompt (instrumental):

> Bouncy, whimsical, cartoony theme for a funny animated family adventure in
> ancient Egypt, among pyramids, a busy market and a grumpy camel. Light and
> playful, not serious or epic. Lead melody on plucked oud and qanun, answered
> by a cheeky bassoon and pizzicato strings, a sneaky tiptoeing feel, darbuka
> and frame-drum groove with finger cymbals, a soft marimba counter-line,
> around 100 bpm. Sunny, mischievous and a little silly, its own sound, like a
> cartoon caper in a sandy marketplace. Strictly instrumental: no whistling, no
> slide whistle, no flute, no vocals. Steady energy from start to end (no long
> intro, no fade-out) so it loops.

Style tags: `whimsical, cartoon, egyptian, oud, qanun, darbuka, bassoon, pizzicato, playful, instrumental`.
Exclude: `whistling, slide whistle, flute, orchestral, epic, cinematic, dramatic, vocals`.

## Bethlehem theme (Suno, for Dan) — planning chat, 2026-11-10 (revised: choir allowed)

Bethlehem's level song: a starry night on the hills around the barn, with sneaking past watchmen. Whimsical and
cartoony, its own instruments, no whistling. **Vocals allowed as texture only (Dan, 2026-11-10):** a small choir
singing "oohs", "aahs" and "hallelujah", like a church choir in the background, never a lead singer with real
lyrics. 2–3 minutes. Save Dan's pick as `Assets/Resources/Music/bethlehem.mp3`.

Suno: **Instrumental OFF** (so the choir can sing), the lyrics below in the Lyrics box.

Style:
> Whimsical, cartoony Christmas-night theme for a funny animated family adventure in the starry hills around a little
> wooden barn. A tiptoeing pizzicato and upright-bass walk, a cheeky clarinet and muted trumpet melody, soft handbells
> and glockenspiel twinkling like stars, harp ripples, brushed snare, around 95 bpm, with a warm angelic church choir
> in the background singing only wordless oohs and aahs and big comic "hallelujah" swells, like a nativity play that
> keeps getting interrupted. Playful, warm and a little silly, not serious or epic. No lead singer, no whistling, no
> flute. Steady energy from start to end so it loops.

Lyrics:
```
[Intro]
[instrumental]

[Choir, soft]
Oooh... aaah...

[instrumental]

[Choir, swelling]
Hal-le-lu-jah! Hal-le-lu-jah!

[instrumental]

[Choir, soft]
Aaah... oooh...

[instrumental]

[Choir, big and comic]
Hal-le-lu-jaaah!

[instrumental]
```

Style tags: `whimsical, cartoon, christmas, church choir, wordless choir, pizzicato, upright bass, clarinet, handbells, playful`.
Exclude: `whistling, slide whistle, flute, solo vocalist, pop vocals, epic, cinematic, dark`.

## Medieval Kingdom theme (Suno, for Dan) — planning chat, 2026-11-11 (rewritten: the first was too serious)

The Medieval Kingdom's level song. Instrumental, 2–3 minutes. Save Dan's pick as `Assets/Resources/Music/medieval.mp3`.
**Suno notes (Dan, 2026-11-11):** the first version came out serious; and Suno rejects "strongman" (an artist's
name), so never use it: say "Muscle Man". Avoid words that pull toward epic: fanfare, brass section, renaissance fair,
knights' march.

Suno prompt (instrumental):

> Silly, bouncy, zany cartoon music for a goofy animated comedy set in a little toy-like castle where clumsy knights
> trip over their own armor and a big show-off called Muscle Man flexes at everyone. Super light and playful, never
> serious: a plucky toy lute and pizzicato strings bouncing along, a honking oom-pah tuba, a goofy bassoon stumbling
> after the melody, a cheeky kazoo answering it, xylophone runs, woodblock clip-clops, tambourine shakes and comic
> boings, a happy hop-skip rhythm around 120 bpm, like a Saturday-morning cartoon chase through a castle. Strictly
> instrumental: no whistling, no slide whistle, no flute, no vocals. Steady energy from start to end so it loops.

Style tags: `cartoon, zany, silly, comedic, bouncy, kazoo, tuba, bassoon, xylophone, pizzicato, lute, playful, instrumental`.
Exclude: `epic, orchestral, cinematic, dramatic, serious, heroic, fanfare, choir, whistling, slide whistle, flute, vocals`.

## Pirate Seas — ship theme (Suno, for Dan) — planning chat, 2026-11-11 (revised: crew shouts allowed)

The ship and island song. Whimsical and cartoony, its own instruments, no whistling. **Vocals as instruments are
welcome (Dan, 2026-11-11):** a rowdy crew shouting "Ahoy!", "Yo-ho!", "Heave-ho!", "Arrr!" between the tunes, like
Bethlehem's hallelujahs: no lead singer, no real lyrics. 2–3 minutes. Save Dan's pick as
`Assets/Resources/Music/pirate.mp3`.

Suno: **Instrumental OFF**, the lyrics below in the Lyrics box.

Style:
> Rollicking, silly cartoon sea-shanty music for a goofy animated pirate comedy with a rowdy ship and rolling barrels.
> Light and playful, never serious: a bouncy squeezebox accordion melody, a cheeky fiddle jig, a stomping oom-pah tuba
> bass, plucky banjo, steel drum sparkles, handclaps and wooden thumps on the offbeat, a swaying 6/8 hornpipe rhythm,
> with a rowdy pirate crew only shouting short calls like "Ahoy!", "Yo-ho!", "Heave-ho!" and a big comic "Arrr!" between
> the phrases, like a Saturday-morning cartoon on the high seas. No lead singer, no whistling, no flute. Steady energy
> from start to end so it loops.

Lyrics:
```
[Intro]
[instrumental]

[Crew shout]
Ahoy!

[instrumental]

[Crew shout]
Yo-ho! Yo-ho!

[instrumental]

[Crew shout]
Heave... ho! Heave... ho!

[instrumental]

[Crew shout, big and comic]
Arrrrr!

[instrumental]
```

Style tags: `cartoon, sea shanty, hornpipe, accordion, fiddle, tuba, banjo, crew shouts, gang vocals, silly, playful, 6/8`.
Exclude: `epic, orchestral, cinematic, dramatic, serious, dark, solo vocalist, pop vocals, whistling, slide whistle, flute`.

## Pirate Seas — underwater theme (Suno, for Dan) — planning chat, 2026-11-11 (revised: as dreamy as possible, soft oohs)

The Pirate Seas are two areas (Dan, 2026-11-11): **the ship** (`pirate.mp3`) and **under the sea** (this one). Plays
whenever the party is underwater; crossfades with the ship theme at the surface. **As dreamy as possible (Dan)**, with
soft wordless "oohs" and "aahs" as texture, no lyrics, no whistling. 2–3 minutes. Save Dan's pick as
`Assets/Resources/Music/underwater.mp3`.

Suno: **Instrumental OFF**, the lyrics below in the Lyrics box.

Style:
> Dreamy, floating, magical underwater music for a gentle animated adventure through a glowing coral reef, sunbeams
> rippling down through blue water and friendly fish drifting by. Slow and weightless, about 75 bpm: soft shimmering
> vibraphone and celesta, slow plucked harp ripples, warm hazy synth pads, a gentle wobbly upright bass, kalimba and
> glockenspiel sparkles like rising bubbles, with a soft airy wordless choir floating in and out singing only "oohs" and
> "aahs", distant and echoing like voices under water. Peaceful, wondrous, a little playful, never dark or scary. No lead
> singer, no drums, no whistling, no flute. Steady from start to end so it loops.

Lyrics:
```
[Intro]
[instrumental]

[Soft choir, distant]
Oooh... oooh...

[instrumental]

[Soft choir, floating]
Aaah... aaah...

[instrumental]

[Soft choir, distant]
Oooh... aaah...

[instrumental]
```

Style tags: `dreamy, underwater, ambient, ethereal, floating, vibraphone, celesta, harp, kalimba, wordless choir, soft oohs, slow`.
Exclude: `epic, orchestral, cinematic, dramatic, dark, scary, drums, solo vocalist, pop vocals, lyrics, whistling, flute`.

## Act I cutscene — longer version (Suno, for Dan) — planning chat, 2026-11-11

The redo of `ActOne` follows Dan's film clip, about **60 s** (the first song is 22 s). Same gentle Christmas mood,
then magic. Beats: 0–20 decorating the tree, Kira swats, the ornament falls; 20–30 it glows and cracks; 30–48 the
rainbow vortex pulls them in; 48–60 they fall out over the Ice Age (the Ice Age theme takes over on landing).
Instrumental, about 60–75 s (trim to taste). Save as `Assets/Resources/Music/act1.mp3` (over the old one). **In (Round 9): Dan's 59.9 s take.**

Suno prompt (instrumental):

> Gentle, cozy Christmas music for a heartwarming animated holiday film, slow and warm at first: a tender carol-like
> melody on music box and celesta with soft sleigh bells, warm pizzicato and felt piano, like Christmas Eve by a
> glowing tree. About a third of the way in, a soft magical shimmer as something sparkles and cracks open: twinkling
> chimes and harp glissandos rising. Then the music swells into a whirling, wondrous, playful rush of bells, harp,
> celesta and soft strings spinning faster and faster like being pulled into a rainbow time vortex, still light and
> whimsical, never epic or scary, ending on a bright airy wash as if floating through the sky. No vocals, no drums.

Style tags: `christmas, gentle, cozy, music box, celesta, sleigh bells, magical, whimsical, harp, swell, instrumental`.
Exclude: `epic, orchestral, cinematic, dramatic, dark, drums, rock, vocals, whistling`.
