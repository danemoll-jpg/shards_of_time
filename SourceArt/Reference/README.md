# Film art references

Dan's images from the film *The Shards of Time*: characters, their outfits per
era, and backgrounds. These are **references only** (art direction for
concept-art prompts, Meshy models and level look), not game assets.

Drop images in the matching folder. Any file name is fine; a short descriptive
name helps (e.g. `dan-caveman-front.png`, `ice-valley-wide.png`).

| Folder | What goes in it |
|---|---|
| `Characters/Dan/` | Dan in every outfit (home, caveman, Egyptian, medieval, pirate, steampunk, retro-future, spacesuit) |
| `Characters/Abi/` | Abi in every outfit |
| `Characters/Kira/` | Kira as a normal cat and every transformation |
| `Characters/MuscleMan/` | Muscle Man in every outfit |
| `Characters/Violinist/` | The violinist in every era |
| `Characters/Others/` | Wise Men, pirates, robots, the camel, anyone else |
| `Worlds/00-Home/` | Living room, Christmas tree, the ornament, the necklace |
| `Worlds/01-IceAge/` … `Worlds/08-Moon/` | Backgrounds and props for each era |
| `Shards/` | Each colored story shard |

If a picture shows several characters, put it in `Worlds/` for that era.

## Film clips (added 2026-11-11)

`Clips/`: short video clips from the film (MP4), one per scene, named after the cutscene they belong to, e.g.
`Clips/03-Bethlehem_WiseMenArrive.mp4`. **They are the staging reference for the game's cutscenes:** shot order,
camera angles, timing, who does what. Code pulls frames from them (ffmpeg) and matches the staging, in the game's own
style and with the game's characters. Clips placed in a world folder (e.g. `Worlds/05-PirateSeas/shard-rolls-overboard.mp4`)
count the same.
