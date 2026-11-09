# Shards of Time — music

**Rule (Dan, 2026-11-09):** level songs (and other full-length tracks) are
Dan's. Code writes the Suno prompts here; Dan makes the tracks, chooses the take
he likes and saves it at the path given. Code never generates level music,
stand-ins included. Short music (stings, the Violinist's tune) stays with Code
via ElevenLabs.

**Direction (Dan, 2026-11-09): whimsical and cartoony, not orchestral or
serious.** Light, bouncy, playful, a little silly, like a classic cartoon:
plucky strings, woodwinds, xylophone, tuba, music box, slide whistles. Avoid
epic, heroic, sweeping orchestral, dark or dramatic. Every new music prompt
follows this, except the vortex (mysterious, not quirky; Dan 2026-11-09).

## The Ice Age theme (Suno, for Dan) — Round 4

In the game now: a stand-in made with ElevenLabs' music API (`Assets/Resources/Music/iceage.mp3`, 90 s,
`python Tools/make_sounds.py --only iceage --force` makes it again). The real theme is Dan's Suno track: save it as
`Assets/Resources/Music/iceage.mp3` (over the stand-in) and the next build plays it. The game loops it seamlessly (the
last 3 s crossfade into the start, `Assets/Scripts/Audio/Music.cs`), so it doesn't need a perfect loop point, but an
ending that doesn't fade to silence loops best.

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
