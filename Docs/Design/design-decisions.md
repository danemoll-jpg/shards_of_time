# Shards of Time (game) — design decisions

Sources: `concept-summary.md` (game concept), `film-synopsis.md` (film canon),
Dan's Abi & Dan Story Bible (series canon; the relevant parts are below).
In the repo these live in `Docs/Design/`; the planning chat keeps a copy in
the claude.ai project.

**Faithfulness (Dan, 2026-11-09):** the game does not have to be 100% faithful
to the film. The film is the backbone (its worlds, story shards, Kira forms,
key beats and ending), and the game expands freely around it. When the
concept and the film disagree, the film is the default, but either can be
changed for gameplay.

## Decided (Dan, 2026-11-09)
- **All characters visible on screen.** The two inactive heroes and Muscle Man
  are AI followers. Standard trick: a follower that falls behind or can't make
  a move warps in just behind the player (no per-move pathfinding).
- **Every Kira transformation in the film is canon.** These are an exception
  that belongs to this film only; the bible's "never supernatural" applies
  everywhere else.
- **Acts I, X and XI are cutscenes, not playable.** (I: ornament breaks.
  X: Muscle Man goes home and pulls the sword. XI: rewind, couch, violinist,
  "NO, KIRA, NO!")
- **Shards:** each world has many small shards plus one colored story shard.
  **The small shards are universal (Dan, 2026-11-10):** one total across all
  worlds, never spent. Each world has a cumulative threshold on that total
  (DEFAULT Ice Age 35, Egypt 75); reaching it wakes the necklace there, which
  leads to the story shard, and the story shard opens the vortex. Extra shards
  anywhere count toward later worlds, which is what makes them worth hunting.
  (Framing: the shards charge Dan's necklace; at the threshold it wakes up and
  leads to the story shard.)
- **All cutscenes are remade in-engine.** Film clips are not reused (Grok
  output was inconsistent). The cutscenes are also a **proof of concept for
  making future films** this way, so the cutscene tooling must be reusable
  (Timeline + Cinemachine, authored as data) and able to render a cutscene
  out to a video file.
- **Muscle Man rejoins for post-credits free-roam**, so secrets that need him
  stay reachable after the ending. Canon is unchanged.
- **Art direction comes from the film's own art.** Dan has the images used
  for the film: the characters, each of their outfits per era, and the
  backgrounds. They go in `SourceArt/Reference/` in the repo. Film stills are
  references; Meshy still needs clean full-body A-pose sheets per the standard
  pipeline.
- **Repos:** `danemoll-jpg/shards_of_time` (code, `C:\Users\danmo\shards_of_time`)
  and `danemoll-jpg/shards-of-time-releases` (launcher catalog).
- **Publish a release every round** (the default for new games; Dan didn't
  change it).

- **The Violinist teaches new moves** (Dan, 2026-11-09). He appears in every
  world (the game adds him to the Ice Age), plays a short tune, and a character
  learns a move. He never explains himself.
- **Travel = the vortex** (Dan, 2026-11-09). Every world's story shard opens a
  vortex; inside, the player picks a path: any unlocked world, the next world,
  or Home. The path home is blocked until all the shards are rebuilt.
- **The ornament that shatters** is the dark silver glass bauble Dan holds in
  `SourceArt/Reference/Worlds/00-Home/home-decorating-tree-ornament.png`.

## Kira — fixed in every form
Scottish Fold, folded ears that never rise, solid gray with no white, and
golden-amber eyes. Every concept-art prompt states this.

## Per-world canon from the film
| # | World | Kira | Story shard | Violinist | Key film beat |
|---|---|---|---|---|---|
| 1 | Ice Age | tiny saber-toothed cat | blue | — (first seen in Egypt) | Dan's backward fall; Kira plays with necklace |
| 2 | Egypt | mummy cat | red | lyre | Sphinx looks like Kira; judgmental camel |
| 3 | Bethlehem | gray lamb | golden, in the manger | — | crash through stable roof; Wise Men leave disappointed |
| 4 | Medieval | small dragon (weak puffs) | purple, in the stone | lute (bard) | Dan pulls shard, Muscle Man becomes his fan, swept along |
| 5 | Pirate Seas | otter | green, in a chest | fiddle | Muscle Man drops shard overboard; underwater chase |
| 6 | Steampunk | gunmetal mechanical cat | teal gear, high in a street clock | steampunk violin | Muscle Man breaks the press; lifts Dan to the clock |
| 7 | Tomorrowland | shiny robot cat | none in film | — | robot says rocket is ready; launch to Moon |
| 8 | Moon | still the robot cat | white, on the rover | — | Kira floats, Abi catches her; Abi solves light-pattern puzzle |

Notes:
- Concept said "royal Egyptian cat" and "Space Kira"; the film's mummy cat
  and robot cat replace them.
- Dan's necklace shard pulses near shards: an in-fiction shard detector.
- Muscle Man is in the party from Medieval through the Moon, then leaves.
- Steampunk → Tomorrowland happens by reality changing, not a vortex.
- Series canon worth using: Dan has time-echo visions when touching temporal
  artifacts (fits the Memory Fragments collectible); the Violinist never
  explains himself; comedy is situational, never quippy; minimal dialogue.

## Open (recommended defaults in brackets)
- Tomorrowland has no story shard in the film [the rocket plays that role:
  the shard count fuels it/opens the launch gate].
