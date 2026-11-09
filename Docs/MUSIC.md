# Shards of Time — music

## The Ice Age theme (Suno, for Dan) — Round 4

In the game now: a stand-in made with ElevenLabs' music API (`Assets/Resources/Music/iceage.mp3`, 90 s,
`python Tools/make_sounds.py --only iceage --force` makes it again). The real theme is Dan's Suno track: save it as
`Assets/Resources/Music/iceage.mp3` (over the stand-in) and the next build plays it. The game loops it seamlessly (the
last 3 s crossfade into the start, `Assets/Scripts/Audio/Music.cs`), so it doesn't need a perfect loop point, but an
ending that doesn't fade to silence loops best.

Suno prompt (instrumental, 2-3 minutes):

> Whimsical, adventurous orchestral theme for a Pixar-style animated family film set in the Ice Age. Playful pizzicato
> strings and bouncy woodwinds carry a warm, hopeful, slightly mischievous melody; glockenspiel and celesta sparkle like
> snow; soft French horns swell in the bridge for a sense of a wide frozen valley with distant volcanoes; light
> percussion, a steady bouncing pulse around 100 bpm. Curious and cheerful, never scary. No vocals. Keeps a steady
> energy from start to end (no long intro, no fade-out) so it loops.

Style tags: `orchestral, whimsical, adventure, family film score, pizzicato, woodwinds, glockenspiel, instrumental`.

Under the music: the wind ambience (`Assets/Resources/Sounds/wind_loop.mp3`, ElevenLabs). Volume: Settings > Music volume.
