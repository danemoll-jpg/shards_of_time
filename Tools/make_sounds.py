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
