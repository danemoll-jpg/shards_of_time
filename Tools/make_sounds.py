"""Shards of Time, Round 4 item 10 (Hostile Horizon's make_sounds.py / make_music.py, ported): the game's sound effects and short
music (stings, the Violinist's tune), made with ElevenLabs. Level songs are Dan's (Suno, Docs/MUSIC.md): never made here (Round 5). The key comes from ELEVENLABS_API_KEY (never printed or saved). Nothing is played: making a
sound never makes any noise on this PC (Dan's testing rule).

  sound effects -> Assets/Resources/Sounds/<name>.mp3   (Audio/Sfx.cs loads them by name; a missing file = the code-made tone)
  short music   -> Assets/Resources/Music/<name>.mp3    (kind 'music': stings and jingles only, never a level song)
Prompts: Docs/SOUNDS.md (written by this tool). Usage: python Tools/make_sounds.py [--only a,b] [--force] [--list]
"""
import argparse, json, os, pathlib, sys, time, urllib.request, urllib.error

ROOT = pathlib.Path(__file__).resolve().parents[1]
SFX = ROOT / 'Assets/Resources/Sounds'
MUSIC = ROOT / 'Assets/Resources/Music'
DOC = ROOT / 'Docs/SOUNDS.md'

# name, seconds, kind ('sfx', 'loop', 'music'), what it is for, prompt
SOUNDS = [
    ('footstep_snow', 0.5, 'sfx', 'a step on snow (played per step while running)',
     'A single soft footstep crunching into fresh powder snow, close, dry, no reverb, no music.'),
    ('crack', 1.5, 'sfx', 'ice cracking (cracked bridge planks, cracked ice breaking)',
     'Thick ice cracking and splitting with a sharp snap and a short creak, outdoors, no music.'),
    ('shard', 1.0, 'sfx', 'a small shard picked up',
     'A bright magical crystal chime pickup sound for a family video game, two quick sparkling glassy notes rising, no music bed.'),
    ('necklace', 2.0, 'sfx', 'the necklace pulses / wakes',
     'A warm magical pulse: a deep soft glowing hum swelling with a shimmering crystal sparkle on top, gentle, fairytale, no music.'),
    ('growl', 1.5, 'sfx', 'a wolf growls before it circles or lunges',
     'A cartoon wolf giving a low playful menacing growl, short, close, for a family animated film, not scary, no music.'),
    ('wind_loop', 12.0, 'loop', 'the Ice Age ambience under the music (loops)',
     'Steady cold wind blowing across a wide snowy mountain valley, soft gusts, faint blowing snow, calm and continuous, no music.'),
    ('pound', 1.0, 'sfx', "Dan's ground pound landing",
     'A heavy cartoon body slam onto packed snow and ice: a deep thud with a crunchy snow burst, punchy, no music.'),
    ('violinist_tune', 6.0, 'sfx', 'the Violinist plays (he teaches a move)',
     'A short mysterious gentle melody on a rustic wooden fiddle, a few slow lilting notes rising, warm and magical, solo instrument, music cue.'),
    # Round 6: Egypt (item 13)
    ('footstep_sand', 0.5, 'sfx', 'a step on sand (Egypt, per step while running)',
     'A single soft footstep in dry desert sand, a gentle sandy crunch and shift, close, no reverb, no music.'),
    ('trap_click', 0.8, 'sfx', 'a trap fires in the tomb (darts, pressure plates)',
     'An ancient stone pressure plate clicking and a hidden mechanism snapping inside a tomb wall, short and crisp, no music.'),
    ('stone_grind', 2.0, 'sfx', 'stone grinding (cat doors sinking, guardians waking, the lift)',
     'A heavy stone block grinding and sliding against stone in an ancient temple, deep rumble, slow, no music.'),
    ('mummy_groan', 1.6, 'sfx', 'a mummy wakes',
     'A cartoon mummy waking with a long dusty sleepy groan, muffled through bandages, silly not scary, for a family animated film, no music.'),
    ('scarab_skitter', 1.2, 'sfx', 'scarabs skitter',
     'A few small beetles skittering quickly over stone, tiny clicking legs, light and comic, no music.'),
    ('camel_snort', 1.2, 'sfx', 'the judgmental camel snorts',
     'A camel giving one big unimpressed snort through its lips, a wet dismissive blubbery huff, comic, close, no music.'),
    ('guard_halt', 1.2, 'sfx', 'a guard spots you',
     'A short sharp whistle blast and a stomp of a spear butt on stone, an ancient palace guard alert, comic, no words, no music.'),
    ('sand_sink', 1.5, 'sfx', 'sinking into quicksand',
     'Sinking into quicksand, a soft sucking sandy slurp and gloop, comic, no music.'),
    ('lyre_tune', 6.0, 'sfx', 'the Violinist plays his lyre (Egypt: he teaches Abi the double jump)',
     'A short mysterious gentle melody plucked on an ancient Egyptian lyre, a few slow bright rising notes, warm and magical, solo instrument, music cue.'),
    # Round 7: Bethlehem (item 12)
    ('footstep_grass', 0.5, 'sfx', 'a step on dry grass (Bethlehem, per step while running)',
     'A single soft footstep on dry grass and packed earth at night, a light rustle, close, no reverb, no music.'),
    ('hay_rustle', 1.5, 'sfx', 'someone lands in hay / hay bursts',
     'Someone plopping into a big soft pile of dry hay, a fluffy rustling whoosh and crackle of straw, comic, close, no music.'),
    ('hay_bounce', 1.0, 'sfx', 'lamb Kira bounces off springy hay or wool',
     'A springy cartoon boing off a soft bouncy pile of hay, a bright playful spring sound with a little straw rustle, family game, no music.'),
    ('sheep_bleat', 1.5, 'sfx', 'sheep bleat (the flocks)',
     'A gentle sheep bleating baa once, soft and friendly, outdoors at night, close, no music.'),
    ('goat_charge', 1.5, 'sfx', 'a grumpy goat lowers its head to charge',
     'A grumpy billy goat giving an annoyed nasal bleat and stamping its hooves on dry ground, then a quick scrabble of hooves, comic, no music.'),
    ('dog_bark', 1.2, 'sfx', 'a guard dog barks (it calls the watchman)',
     'A scruffy medium-sized village dog barking twice, gruff and alert but not scary, outdoors at night, no music.'),
    ('scorpion_click', 0.8, 'sfx', 'a scorpion scuttles and snaps',
     'A small cartoon scorpion clicking its pincers twice and scuttling on dry stone, tiny crisp chitin clicks, comic, no music.'),
    ('lantern_creak', 1.2, 'sfx', "a night watchman's lantern creaks as he walks",
     'An old iron lantern swinging on its handle with a soft metallic creak, quiet night, no music.'),
    ('roof_crash', 2.0, 'sfx', 'the party crashes through the barn roof',
     'Bodies crashing through an old wooden barn roof: splintering boards, a burst of thatch and straw, a cartoon crash with debris, no music.'),
    ('door_knock', 1.0, 'sfx', 'a muffled voice behind the inn door (no room)',
     'A wooden inn door being knocked on twice, then a muffled grumpy hmph from inside, comic, no clear words, no music.'),
    ('harp_tune', 6.0, 'sfx', "the Violinist plays his shepherd's harp (Bethlehem: he teaches Kira the pounce)",
     "A short mysterious gentle melody plucked on a small wooden shepherd's harp, a few slow bright rising notes, warm and magical, solo instrument, music cue."),
    ('choir_hallelujah', 6.0, 'sfx', 'the choir swells as the Wise Men walk in (the finale; a short sting, not a level song)',
     'A short heavenly choir swell singing a big joyful hallelujah chord, bright and grand, over-the-top and a little comic, rising then holding, music sting.'),
    # Round 8: the Medieval Kingdom
    ('fire_puff', 1.0, 'sfx', "baby dragon Kira's small fire puff",
     'A baby dragon breathing one small comic puff of fire, a soft whoosh with a tiny crackle of flame, cute, family game, no music.'),
    ('hay_fire', 2.0, 'sfx', 'dry hay catches fire',
     'A pile of dry hay catching fire, a soft whoomp of flame igniting and then crackling straw burning, close, no music.'),
    ('wagon_blast', 2.5, 'sfx', 'the burning hay wagon hits the castle wall',
     'A comic cartoon explosion of a wooden hay wagon crashing into a stone castle wall: a crash, a big boom, wood splinters flying and a silly springy sproing at the end, no music.'),
    ('armor_clank', 1.0, 'sfx', "a clumsy knight's armor clanks as he walks / turns",
     'A clumsy knight in loose plate armor clanking and rattling as he takes a step and turns, tinny metal clatter, comic, close, no music.'),
    ('shield_block', 0.6, 'sfx', 'a hit glances off a knight\'s shield',
     'A sword glancing off a metal shield, a sharp metallic clang with a short ring, close, no music.'),
    ('shield_break', 1.2, 'sfx', "a knight's shield is knocked away",
     'A metal shield knocked out of a hand and clattering and bouncing on stone cobbles, comic, no music.'),
    ('arrow_twang', 0.8, 'sfx', 'an archer shoots',
     'A wooden bow string twanging and an arrow whooshing past, quick and crisp, no music.'),
    ('rat_squeak', 0.7, 'sfx', 'a castle rat squeaks (scared)',
     'A cartoon rat giving a quick frightened squeak, high and comic, close, no music.'),
    ('mm_grunt', 1.0, 'sfx', 'Muscle Man lifts / throws something heavy',
     'A huge strongman man giving one deep comic effort grunt while lifting something heavy, close, no words, no music.'),
    ('mm_laugh', 2.0, 'sfx', "Muscle Man's mocking laugh",
     'A huge strongman man laughing a big booming mocking belly laugh, ha ha ha, deep voice, comic, close, no music.'),
    ('mm_my_hero', 1.4, 'sfx', 'Muscle Man calls Dan "My hero!" in awe (Round 9: was "Master!")',
     'A huge strongman man with a deep voice gasping the two spoken words "My hero!" in awe and adoration, clear male voice exclamation, comic, close, no music.'),
    ('stone_whirl', 4.0, 'sfx', 'the cobblestones swirl up into a whirlpool (TheStone, Round 9)',
     'Many cobblestones grinding and rattling as they swirl round in a rising magical whirlpool, a deep rumbling swirling rush with a sparkling shimmer, fairytale, no music.'),
    ('boulder_roll', 2.0, 'sfx', 'a boulder rolls on cobbles / heavy stone grinds (Medieval; Egypt keeps stone_grind)',
     'A heavy stone boulder rolling and grinding over cobblestones, a deep rumbling scrape, slow, no music.'),
    ('shard_slide', 2.0, 'sfx', 'the story shard slides out of the stone',
     'A crystal sliding slowly out of a stone with a scraping hiss and a magical rising ringing shimmer, fairytale, no music.'),
    ('punch_charge', 1.0, 'sfx', 'a punch charges up',
     'A rising energy hum building up quickly as a magical punch charges, a swelling whirr, video game power up, no music.'),
    ('punch_big', 0.8, 'sfx', 'the charged punch lands',
     'A huge cartoon punch impact with a fast whoosh and a deep heavy thump, punchy, no music.'),
    ('wall_break', 1.5, 'sfx', 'a stone wall crumbles',
     'An old stone wall breaking and crumbling into rubble, stones tumbling and dust, close, no music.'),
    ('wings_flap', 1.0, 'sfx', 'baby dragon Kira flaps and glides',
     'Small leathery wings flapping a few times quickly and then a short soft gliding whoosh, cute, no music.'),
    ('crowd_cheer', 3.0, 'sfx', 'the tournament crowd cheers',
     'A small medieval tournament crowd cheering and clapping outdoors, happy shouts, no clear words, no music.'),
    ('bell_ring', 1.5, 'sfx', "the strongman high-striker's bell",
     'A strongman high striker at a fair: a puck shooting up and hitting a bright brass bell with a loud ding that rings out, no music.'),
    ('horse_neigh', 1.5, 'sfx', 'a goofy pony whinnies',
     'A goofy little pony giving a silly whinny and a snort, comic, close, no music.'),
    ('footstep_cobble', 0.5, 'sfx', 'a step on cobblestones (Medieval, per step while running)',
     'A single footstep on stone cobblestones, a short hard tap with a tiny scuff, close, no reverb, no music.'),
    ('portcullis', 2.0, 'sfx', 'the portcullis is heaved up',
     'A heavy iron portcullis being heaved up, chains rattling over a winch and the iron gate creaking and clanking, no music.'),
    ('lute_tune', 6.0, 'sfx', 'the Violinist plays his lute as a bard (Medieval: he teaches a move)',
     'A short mysterious gentle playful melody plucked on a medieval lute, a few slow bright rising notes, warm and magical, solo instrument, music cue.'),
    # Round 10: the Pirate Seas
    ('waves_loop', 8.0, 'sfx', 'gentle sea waves lapping at a wooden ship (ambience)',
     'Gentle calm sea waves lapping against the wooden hull of an old sailing ship at sunset, soft splashes, a little creak, no music, no voices.'),
    ('wood_creak', 2.0, 'sfx', "the ship's timbers creak",
     'The wooden timbers and ropes of an old sailing ship creaking slowly as it rocks on the sea, close, no music.'),
    ('barrel_thud', 0.8, 'sfx', 'someone lands in a wooden barrel',
     'A hollow wooden barrel thud as something lands inside it, a short bonk with a little rattle, comic, close, no music.'),
    ('sail_rip', 1.2, 'sfx', 'Muscle Man drops through a sail',
     'A big canvas sail ripping as something heavy crashes through it from above, a fast tearing whoosh, comic, no music.'),
    ('wood_crash', 1.5, 'sfx', 'Muscle Man crashes flat onto the deck among barrels',
     'A huge heavy cartoon crash onto a wooden ship deck, barrels smashing into staves and planks clattering, comic, no music.'),
    ('chest_open', 1.2, 'sfx', 'the treasure chest creaks open',
     'An old wooden treasure chest lid creaking open with a little metal clink and a magical shimmer, no music.'),
    ('shard_bounce', 0.6, 'sfx', 'the crystal shard bounces on the deck',
     'A small glass crystal bouncing on a wooden deck, a bright clink tock, close, no music.'),
    ('shard_roll', 3.0, 'sfx', 'the crystal shard rolls across the deck',
     'A small crystal rolling across wooden deck planks, a light rattling roll that speeds up, close, no music.'),
    ('splash', 1.2, 'sfx', 'someone splashes into the sea',
     'Someone jumping into the sea with a big splash, water splashing up and falling back, outdoors, no music.'),
    ('bubbles', 2.0, 'sfx', 'bubbles under water',
     'Air bubbles rising underwater, soft gurgling bubbly pops, muffled underwater, no music.'),
    ('sea_vortex', 4.0, 'sfx', 'the swirling green underwater vortex (the finale)',
     'A magical swirling vortex opening under the sea, a deep muffled rushing whirl with rising sparkling shimmering chimes and bubbles, fairytale, no music.'),
    ('crab_click', 0.8, 'sfx', 'a grumpy crab snaps its claw',
     'A small crab clicking and snapping its claws together quickly, crisp clicks, comic, close, no music.'),
    ('puffer_puff', 1.0, 'sfx', 'a pufferfish puffs up',
     'A cartoon pufferfish suddenly inflating underwater with a quick muffled whoomp and a squeaky stretch, comic, no music.'),
    ('pirate_arr', 1.2, 'sfx', 'a goofy pirate deckhand spots you: "Arr!"',
     'A goofy cartoon pirate man growling the word "Arrr!" in a silly gruff voice, comic, close, no music.'),
    ('rope_creak', 1.5, 'sfx', 'climbing the rope rigging',
     'Someone climbing a rope net on a sailing ship, ropes creaking and stretching, hand over hand, close, no music.'),
    ('footstep_wood', 0.5, 'sfx', 'a step on a wooden deck (Pirate Seas, per step while running)',
     'A single footstep on a wooden ship deck, a short hollow knock with a tiny creak, close, no reverb, no music.'),
    ('cannon_roll', 2.0, 'sfx', 'Muscle Man rolls a heavy cannon aside',
     'A heavy iron cannon on wooden wheels rolling over a wooden deck, a deep rumbling roll and creak, no music.'),
    ('fiddle_shanty', 6.0, 'sfx', 'the Violinist plays a sea shanty on his fiddle (Pirate Seas: he teaches the deep dive)',
     'A short lively playful sea shanty melody played on a solo fiddle, a few bouncy bright notes then a warm magical rising phrase, solo instrument, music cue.'),
]


def write_doc():
    lines = ['# Shards of Time — sounds made with ElevenLabs (Round 4; level music removed in Round 5)', '',
             'Made by `python Tools/make_sounds.py` (key from `ELEVENLABS_API_KEY`). Sound effects are',
             '`Assets/Resources/Sounds/<name>.mp3` (the game loads them by name, `Audio/Sfx.cs`, and falls back to a tone made in',
             'code when a file is missing). Level songs are Dan’s Suno tracks (`Docs/MUSIC.md`), never made here. To replace',
             'one: generate it again (`--only <name> --force`) or drop your own file with the same name into that folder and rebuild.', '',
             '| File | Length | Kind | For | Prompt |', '|---|---|---|---|---|']
    for n, sec, kind, what, prompt in SOUNDS:
        lines.append(f'| `{n}.mp3` | {sec:g} s | {kind} | {what} | {prompt} |')
    DOC.write_text('\n'.join(lines) + '\n', encoding='utf-8')


def post(url, key, body, name):
    for attempt in range(3):
        req = urllib.request.Request(url, data=json.dumps(body).encode(), headers={'xi-api-key': key, 'Content-Type': 'application/json'}, method='POST')
        try:
            with urllib.request.urlopen(req, timeout=300) as r: return r.read(), None
        except urllib.error.HTTPError as e:
            msg = e.read().decode('utf-8', 'replace')[:300]
            if e.code in (401, 402, 403): return None, f'HTTP {e.code} {msg}'
            if e.code in (400, 422) and 'loop' in body: body.pop('loop'); continue
            print(f'  {name}: HTTP {e.code} {msg}'); time.sleep(4)
        except Exception as e:
            print(f'  {name}: {e}'); time.sleep(4)
    return None, 'failed after 3 tries'


def make(key, name, seconds, kind, prompt):
    sfx_url = 'https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_128'
    if kind == 'music':
        data, err = post('https://api.elevenlabs.io/v1/music?output_format=mp3_44100_128', key,
                         {'prompt': prompt, 'music_length_ms': int(seconds * 1000), 'model_id': 'music_v1', 'force_instrumental': True}, name)
        if data: return data, None
        print(f'  {name}: the music API said {err}; making a 22 s looping stand-in with the sound-effects model')
        return post(sfx_url, key, {'text': prompt, 'duration_seconds': 22, 'prompt_influence': .6, 'model_id': 'eleven_text_to_sound_v2', 'loop': True}, name)
    body = {'text': prompt, 'duration_seconds': seconds, 'prompt_influence': .55, 'model_id': 'eleven_text_to_sound_v2'}
    if kind == 'loop': body['loop'] = True
    return post(sfx_url, key, body, name)


def main():
    a = argparse.ArgumentParser(); a.add_argument('--only', default=''); a.add_argument('--force', action='store_true'); a.add_argument('--list', action='store_true')
    o = a.parse_args()
    write_doc()
    if o.list:
        for s in SOUNDS: print(s[0], s[2])
        return 0
    key = os.environ.get('ELEVENLABS_API_KEY', '')
    if not key: print('No ELEVENLABS_API_KEY: see Docs/SOUNDS.md for the prompts.'); return 1
    only = set(x for x in o.only.split(',') if x); ok = 0; failed = []
    for n, sec, kind, what, prompt in SOUNDS:
        if only and n not in only: continue
        f = (MUSIC if kind == 'music' else SFX) / f'{n}.mp3'
        if f.exists() and not o.force: print(f'{n}: kept ({f.stat().st_size} bytes)'); ok += 1; continue
        data, err = make(key, n, sec, kind, prompt)
        if data and len(data) > 1000: f.parent.mkdir(parents=True, exist_ok=True); f.write_bytes(data); ok += 1; print(f'{n}: {len(data)} bytes')
        else: print(f'{n}: FAILED ({err})'); failed.append(n)
    print(f'{ok} made / kept; failed: {", ".join(failed) or "none"}')
    return 0 if not failed else 2


if __name__ == '__main__':
    sys.exit(main())
