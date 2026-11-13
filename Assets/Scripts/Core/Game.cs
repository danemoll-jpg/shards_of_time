using System.Collections;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

namespace ShardsOfTime
{
    // The game's one object (Assets/Scenes/Main.unity holds only this; everything else is built in code at start, as in Hostile
    // Horizon). Title screen -> play -> pause; the world and the party are rebuilt from the save on Continue / New game.
    // Round 4: the world is the Ice Age (World/IceAge.cs) and its vortex space (World/Vortex.cs); New game plays the film's opening
    // (ActOne in the living room, IceAgeArrival in the valley), both skippable. The Round 1 graybox playground is a debug test area
    // (Settings > Test area) with its own save file. Progression: the small-shard requirement wakes the necklace (NecklaceWakes), the
    // story shard opens the vortex gate (VortexOpens; the playground: PortalOpens and its portal); the vortex's paths lead to the
    // worlds unlocked so far, the next world (Egypt: to be continued) and Home (blocked until all eight story shards are rebuilt).
    public sealed class Game : MonoBehaviour
    {
        public enum Mode { Title, Playing, Paused }
        public static Game I { get; private set; }
        public static WorldData World { get; private set; }
        public Mode State { get; private set; } = Mode.Title;
        public static bool Playing => I && I.State == Mode.Playing && Cutscenes.Playing == null;
        public static bool InCutscene => Cutscenes.Playing != null;
        public static bool TestArea => World && World.worldId == WorldData.Playground;
        public static bool InEgypt => World && World.worldId == WorldData.Egypt;   // Round 6
        public static bool InBethlehem => World && World.worldId == WorldData.Bethlehem;   // Round 7
        public static bool InMedieval => World && World.worldId == WorldData.Medieval;     // Round 8
        public static bool InPirate => World && World.worldId == WorldData.Pirate;         // Round 10
        public static bool InSteampunk => World && World.worldId == WorldData.Steampunk;   // Round 11
        public static bool InTomorrowland => World && World.worldId == WorldData.Tomorrowland;   // Round 12
        public static bool InMoon => World && World.worldId == WorldData.Moon;   // Round 13
        // the open world's vortex gate and ground (Round 6: two worlds; Round 7: three)
        public static Portal Gate => InMoon ? Moon.Gate : InTomorrowland ? Tomorrowland.Gate : InSteampunk ? Steampunk.Gate : InPirate ? Pirate.Gate : InMedieval ? Medieval.Gate : InBethlehem ? Bethlehem.Gate : InEgypt ? Egypt.Gate : IceAge.Gate;
        public static Vector3 OnGround(float x, float z) => InMoon ? Moon.OnGround(x, z) : InTomorrowland ? Tomorrowland.OnGround(x, z) : InSteampunk ? Steampunk.OnGround(x, z) : InPirate ? Pirate.OnGround(x, z) : InMedieval ? Medieval.OnGround(x, z, x > 300 ? 3f : 30f) : InBethlehem ? Bethlehem.OnGround(x, z) : InEgypt ? Egypt.OnGround(x, z) : IceAge.OnGround(x, z);
        // the save's world (the playground aside): Round 7, three of them
        static string SavedWorld => SaveGame.Current.currentWorld == WorldData.Egypt || SaveGame.Current.currentWorld == WorldData.Bethlehem || SaveGame.Current.currentWorld == WorldData.Medieval || SaveGame.Current.currentWorld == WorldData.Pirate || SaveGame.Current.currentWorld == WorldData.Steampunk || SaveGame.Current.currentWorld == WorldData.Tomorrowland || SaveGame.Current.currentWorld == WorldData.Moon ? SaveGame.Current.currentWorld : WorldData.IceAge;
        public static readonly Vector3 HomeAnchor = new Vector3(0, 400f, -3000f);   // the living room of Act I (inside its cutscene)

        public Pickup StoryShard; public GameObject StoryBeam; public Portal PortalA;
        Transform worldRoot; CinemachineCamera titleCam; float titleSpin;
        public bool Busy { get; private set; }          // a story moment (cutscene + its follow-up) is running
        public void SetBusy(bool b) => Busy = b;
        public int ToBeContinuedShown { get; private set; }

        void Awake()
        {
            I = this;
            Application.runInBackground = CheckRun.Active || Application.isBatchMode;
            GameSettings.Load(); SaveGame.Load(); Controls.Init();
            // followers never block the player; enemies ignore followers; the low-gravity / water boxes have no colliders
            Physics.IgnoreLayerCollision(Hero.PlayerLayer, Hero.FollowerLayer);
            Physics.IgnoreLayerCollision(Hero.FollowerLayer, Hero.FollowerLayer);
            Physics.IgnoreLayerCollision(Hero.FollowerLayer, Hero.EnemyLayer);
            var rig = CameraRig.Create();
            Look.Apply(rig.Cam);
            Sfx.Init(gameObject); Music.Init(gameObject);
            Hud.Create(); Menu.Create();
            World = WorldData.Load(WorldData.IceAge);
            var tc = new GameObject("Title Camera"); titleCam = tc.AddComponent<CinemachineCamera>(); titleCam.Priority = 30;
            titleCam.Lens.FieldOfView = 50f; titleCam.Lens.FarClipPlane = 2500f;
        }

        void Start()
        {
            BuildWorld();
            ToTitle();
            PlayLog.Line($"Shards of Time {Application.version} started; save {(SaveGame.Exists ? "found" : "none")} at {SaveGame.FilePath}");
            if (CheckRun.Active) gameObject.AddComponent<ExeCheck>();
        }

        public void BuildWorld()
        {
            if (worldRoot) { worldRoot.gameObject.SetActive(false); Destroy(worldRoot.gameObject); }
            if (Party.I) { Party.I.gameObject.SetActive(false); Destroy(Party.I.gameObject); }
            Pickup.All.Clear(); Checkpoint.All.Clear();
            Look.ApplyWorld(World.worldId);
            NecklaceModel.Tint = TestArea ? new Color(.25f, .55f, 1f) : World.areas[0].storyShardColor;   // Round 6, item 6: the shard's colour per world
            if (TestArea) worldRoot = Playground.Build(World);
            else { worldRoot = InMoon ? Moon.Build(World) : InTomorrowland ? Tomorrowland.Build(World) : InSteampunk ? Steampunk.Build(World) : InPirate ? Pirate.Build(World) : InMedieval ? Medieval.Build(World) : InBethlehem ? Bethlehem.Build(World) : InEgypt ? Egypt.Build(World) : IceAge.Build(World); Vortex.Build(worldRoot); }
            var a = World.areas[0];
            Party.Create(a.spawn, Quaternion.Euler(0, a.spawnYaw, 0));
            Health.Refill();
            Checkpoint.RespawnParty();
            PlayLog.Line($"world {World.worldId} built ({World.shards.Count} shards); " + string.Join("; ", World.areas.Select(x => World.RequirementReport(x.id).detail)));
        }

        public AreaData Area => Party.I ? World.AreaAt(Party.I.Player.transform.position) : null;
        // Fix 8.1: the necklace's rule: the universal total reaches the threshold AND this world gave at least `fromHere`
        public bool Ready(AreaData a) => SaveGame.Current.Total >= a.requirement && AreaShards(a.id) >= a.fromHere;
        public static string RuleText(AreaData a) => a.fromHere > 0 ? $"{a.requirement} shards in all and {a.fromHere} from here" : $"{a.requirement} shards in all";
        public string Progress(AreaData a) => $"{SaveGame.Current.Total} in all, {AreaShards(a.id)} from here so far";
        public int AreaShards(string area) { var got = SaveGame.Current.World(World.worldId).shards; return World.InArea(area).Count(s => got.Contains(s.id)); }

        // Fix 6.1: the pause menu's completion tracker: each world's own count (the HUD shows the universal total)
        public string ShardTracker()
        {
            var s = SaveGame.Current;
            if (TestArea) return $"Test area: {s.World(World.worldId).shards.Count} / {World.shards.Count} shards";
            var lines = WorldData.StoryOrder.Where(w => w == WorldData.IceAge || s.worldsReached.Contains(w))
                .Select(w => { var d = WorldData.Load(w); return $"{d.displayName}: {s.World(w).shards.Count} / {d.shards.Count}"; });
            return string.Join("\n", lines) + $"\nShards in all: {s.Total}";
        }

        // ---- title, play, pause
        public void ToTitle()
        {
            if (TestArea) LeaveTestArea(false);
            State = Mode.Paused; Time.timeScale = 1f;
            State = Mode.Title;
            titleCam.Priority = 30;
            var p = new Menu.Page { title = "" };
            p.rows.Add(new Menu.Row { label = "Continue", enabled = () => SaveGame.Exists, submit = () => StartGame(false) });
            p.rows.Add(new Menu.Row { label = "New game", submit = () => { if (SaveGame.Exists) Menu.I.Push(ConfirmNewGame()); else StartGame(true); } });
            p.rows.Add(new Menu.Row { label = "Theater", enabled = () => SaveGame.Exists && SaveGame.Current.seen.Count > 0, submit = () => Menu.I.Push(TheaterPage(true)) });   // Fix 10.1
            p.rows.Add(new Menu.Row { label = "Settings", submit = () => Menu.I.Push(Menu.SettingsPage()) });
            p.rows.Add(new Menu.Row { label = "Controls", submit = () => Menu.I.Push(Menu.ControlsPage()) });
            p.rows.Add(new Menu.Row { label = "Credits", submit = () => Menu.I.Push(Menu.CreditsPage()) });
            p.rows.Add(new Menu.Row { label = "Quit", submit = Quit });
            Menu.I.Show(p, true);
            PlayLog.Line("title screen");
        }

        Menu.Page ConfirmNewGame()
        {
            var p = new Menu.Page { title = "Start a new game?" };
            p.body = () => "\nYour saved game will be erased.";
            p.rows.Add(new Menu.Row { label = "No, keep my save", submit = () => Menu.I.Back() });
            p.rows.Add(new Menu.Row { label = "Yes, start over", submit = () => StartGame(true) });
            return p;
        }

        public void StartGame(bool newGame)
        {
            SaveGame.Slot = "";
            if (newGame) SaveGame.NewGame(); else SaveGame.Load();
            World = WorldData.Load(SavedWorld);   // Round 6: the world the save is in
            BuildWorld();
            Menu.I.Close();
            titleCam.Priority = -10;
            State = Mode.Playing; Time.timeScale = 1f;
            PlayLog.Line(newGame ? "new game" : $"continue: {SaveGame.Current.Total} shards, checkpoint {SaveGame.Current.checkpoint}");
            if (newGame)
            {
                SaveGame.Current.checkpoint = IceAge.Start; SaveGame.Save("new game");
                StartCoroutine(Opening());
                return;
            }
            // a story moment the save stopped in the middle of
            var prog = SaveGame.Current.World(World.worldId);
            if (prog.storyShard && Gate && !SaveGame.Current.portals.Contains(Gate.id)) SaveGame.Current.portals.Add(Gate.id);
            if (InEgypt && !prog.opened.Contains("EG-arrived")) StartCoroutine(ArriveInEgypt());   // stopped before the arrival ended
            if (InBethlehem && !prog.opened.Contains("BE-arrived")) StartCoroutine(ArriveInBethlehem());
            if (InMedieval && !prog.opened.Contains("ME-arrived")) StartCoroutine(ArriveInMedieval());
            if (InPirate && !prog.opened.Contains("PS-arrived")) StartCoroutine(ArriveInPirate());   // Round 10
            if (InSteampunk && !prog.opened.Contains(Steampunk.ArrivedFlag)) StartCoroutine(ArriveInSteampunk());   // Round 11
            else if (InSteampunk && !prog.opened.Contains(Steampunk.PressFlag)) Steampunk.StartBeltRun();   // stopped on the belt: the run again
            if (InTomorrowland && !prog.opened.Contains(Tomorrowland.ArrivedFlag)) StartCoroutine(ArriveInTomorrowland());   // Round 12
            if (InMoon && !prog.opened.Contains(Moon.ArrivedFlag)) StartCoroutine(ArriveOnMoon());   // Round 13
        }

        // Item 3: a new game plays the film's opening, then play starts at the landing spot
        IEnumerator Opening()
        {
            Busy = true;
            yield return Cutscenes.Play("ActOne", HomeAnchor, Quaternion.identity);
            var a = World.areas[0]; var face = Quaternion.Euler(0, a.spawnYaw, 0);
            var at = IceAge.OnGround(a.spawn.x, a.spawn.z);
            Party.I.PlaceAll(at + Vector3.up * .1f, face); CameraRig.I.SnapBehind();
            yield return Cutscenes.Play("IceAgeArrival", at, face);
            Party.I.PlaceAll(at + Vector3.up * .1f, face); CameraRig.I.SnapBehind();
            Hud.Message($"Find the shards! {RuleText(a)} wake Dan's necklace.", 6f);
            Busy = false;
        }

        // Round 4: the Round 1 graybox playground as a debug test area (its own save file, fresh each time)
        public void StartTestArea()
        {
            SaveGame.Slot = "testarea"; SaveGame.NewGame();
            SaveGame.Current.currentWorld = WorldData.Playground;
            World = WorldData.Load(WorldData.Playground);
            BuildWorld();
            Menu.I.Close(); titleCam.Priority = -10;
            State = Mode.Playing; Time.timeScale = 1f;
            SaveGame.Current.checkpoint = "A-start"; SaveGame.Save("test area");
            Hud.Message("Test area (the Round 1 graybox playground). Pause > Leave the test area to go back.", 5f);
            PlayLog.Line("test area started");
        }

        public void LeaveTestArea(bool toTitle = true)
        {
            SaveGame.Slot = ""; SaveGame.Load();
            World = WorldData.Load(SavedWorld);
            BuildWorld();
            PlayLog.Line("left the test area");
            if (toTitle) { Menu.I.Close(); ToTitle(); }
        }

        public void Pause()
        {
            if (State != Mode.Playing || InCutscene) return;
            State = Mode.Paused; Time.timeScale = 0f; Necklace.StopRumble();
            var p = new Menu.Page { title = "Paused", back = Resume, body = () => "\n" + ShardTracker() };
            p.rows.Add(new Menu.Row { label = "Resume", submit = Resume });
            p.rows.Add(new Menu.Row { label = "Settings", submit = () => Menu.I.Push(Menu.SettingsPage()) });
            p.rows.Add(new Menu.Row { label = "Controls", submit = () => Menu.I.Push(Menu.ControlsPage()) });
            // Fix 7.2, item 3: back to the last checkpoint with the party (no heart lost, shards kept), in every world and the vortex
            p.rows.Add(new Menu.Row { label = "Return to checkpoint", enabled = () => !Busy, submit = () => { Resume(); StartCoroutine(ReturnToCheckpoint()); } });
            // Fix 10.1, item 1: once the first vortex is open, back to the vortex from anywhere (the spot is kept for coming back)
            if (!TestArea) p.rows.Add(new Menu.Row { label = "Return to the vortex", enabled = () => CanReturnToVortex, submit = () => { Resume(); StartCoroutine(ReturnToVortex()); } });
            if (!TestArea) p.rows.Add(new Menu.Row { label = "Theater", enabled = () => !Busy && SaveGame.Current.seen.Count > 0, submit = () => Menu.I.Push(TheaterPage(false)) });
            if (TestArea) p.rows.Add(new Menu.Row { label = "Leave the test area", submit = () => { Menu.I.Close(); ToTitle(); } });
            else p.rows.Add(new Menu.Row { label = "Quit to title", submit = () => { SaveGame.Save("quit to title"); Menu.I.Close(); ToTitle(); } });
            p.rows.Add(new Menu.Row { label = "Quit game", submit = Quit });
            Menu.I.Show(p, false);
            PlayLog.Line("paused");
        }

        public int CheckpointReturns { get; private set; }
        IEnumerator ReturnToCheckpoint()
        {
            if (Busy) yield break;
            Busy = true; Party.I.Frozen = true;
            for (float t = 0; t < 1f; t += Time.deltaTime / .35f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(1f);
            Checkpoint.RespawnParty(); CheckpointReturns++; stuckFor = 0; stuckAnchor = Party.I.Player.transform.position;
            PlayLog.Line($"returned to checkpoint {SaveGame.Current.checkpoint}");
            yield return null;
            for (float t = 1; t > 0f; t -= Time.deltaTime / .35f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(0f); Party.I.Frozen = false; Busy = false;
            OnAreaChanged();
        }

        // ---- Fix 10.1, item 1: Pause > Return to the vortex
        public bool CanReturnToVortex => !Busy && !TestArea && SaveGame.Current.World(WorldData.IceAge).storyShard && Party.I && !Vortex.Inside(Party.I.Player.transform.position);
        public int VortexReturns { get; private set; }
        public IEnumerator ReturnToVortex()
        {
            if (!CanReturnToVortex) yield break;
            Busy = true; Party.I.Frozen = true;
            var pl = Party.I.Player; var m = pl.Motor;
            // the spot to come back to: where they stand (on the ground or swimming), else the checkpoint's
            var s = SaveGame.Current;
            s.returnWorld = World.worldId; s.returnCp = s.checkpoint;
            bool here = m.Grounded || m.Mode == MoveMode.Swim;
            s.returnSpot = here ? pl.transform.position : Vector3.zero; s.returnYaw = pl.transform.eulerAngles.y;
            Sfx.Play("portal", .6f);
            for (float t = 0; t < 1f; t += Time.deltaTime / .5f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(1f);
            s.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind();
            VortexReturns++;
            SaveGame.Save("return to the vortex");
            PlayLog.Line($"returned to the vortex from {World.worldId} (spot {(here ? s.returnSpot.ToString("F1") : "the checkpoint " + s.returnCp)})");
            yield return null;
            for (float t = 1; t > 0f; t -= Time.deltaTime / .5f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(0f); Party.I.Frozen = false; Busy = false;
            Hud.Message("The vortex: walk a path to choose where it takes you. Your spot is kept.", 5f);
        }

        // ---- Fix 10.1, item 5: the Theater. Every cutscene seen so far (SaveGame.seen), by world in story order; plays with its music.
        public static readonly (string world, string[] names)[] TheaterList =
        {
            (WorldData.IceAge, new[] { "ActOne", "IceAgeArrival", "ViolinistTeaches", "NecklaceWakes", "VortexOpens" }),
            (WorldData.Egypt, new[] { "EgyptArrival", "SphinxGag", "ViolinistTeachesEgypt", "NecklaceWakesEgypt", "VortexOpensEgypt" }),
            (WorldData.Bethlehem, new[] { "BethlehemArrival", "ViolinistTeachesBethlehem", "NecklaceWakesBethlehem", "WiseMenArrive" }),
            (WorldData.Medieval, new[] { "MedievalArrival", "ViolinistTeachesMedieval", "NecklaceWakesMedieval", "TheStone" }),
            (WorldData.Pirate, new[] { "PirateArrival", "ViolinistTeachesPirate", "NecklaceWakesPirate", "ShardOverboard", "SeaVortex" }),
            (WorldData.Steampunk, new[] { "SteampunkArrival", "MuscleManHero", "ViolinistTeachesSteampunk", "NecklaceWakesSteampunk", "ClockShard" }),   // Round 11
            (WorldData.Tomorrowland, new[] { "TomorrowlandArrival", "ViolinistTeachesTomorrowland", "NecklaceWakesTomorrowland", "RocketLaunch" }),   // Round 12
            (WorldData.Moon, new[] { "MoonLanding", "KiraFloats", "ViolinistPlaysMoon", "NecklaceWakesMoon", "WhiteShard" }),   // Round 13
        };
        public static string TheaterLabel(string n) =>
            n == "ActOne" ? "Act I: the living room" : n.EndsWith("Arrival") ? "The arrival" : n.StartsWith("ViolinistTeaches") ? "The Violinist" :
            n.StartsWith("NecklaceWakes") ? "The necklace wakes" : n.StartsWith("VortexOpens") ? "The vortex opens" : n == "SphinxGag" ? "The Sphinx" :
            n == "WiseMenArrive" ? "The Wise Men" : n == "TheStone" ? "The stone" : n == "ShardOverboard" ? "Overboard!" : n == "SeaVortex" ? "The sea vortex" : n == "MuscleManHero" ? "Muscle Man the hero" : n == "ClockShard" ? "The great clock" : n == "RocketLaunch" ? "The launch" : n == "MoonLanding" ? "The landing" : n == "KiraFloats" ? "Kira floats" : n == "ViolinistPlaysMoon" ? "The Violinist" : n == "WhiteShard" ? "The white shard" : n;
        public Menu.Page TheaterPage(bool fromTitle)
        {
            var p = new Menu.Page { title = "Theater" };
            p.body = () => "\nThe cutscenes you have seen, world by world.";
            foreach (var (w, names) in TheaterList)
            {
                int n = names.Count(SaveGame.Current.seen.Contains); if (n == 0) continue;
                var d = WorldData.Load(w); var nn = names; var dn = d.displayName;
                p.rows.Add(new Menu.Row { label = $"{dn} ({n})", submit = () => Menu.I.Push(TheaterWorldPage(dn, nn, fromTitle)) });
            }
            p.rows.Add(new Menu.Row { label = "Back", submit = () => Menu.I.Back() });
            return p;
        }
        public Menu.Page TheaterWorldPage(string title, string[] names, bool fromTitle)
        {
            var p = new Menu.Page { title = title };
            p.body = () => $"\nHold {Controls.Btn(Controls.Act.Skip)} to skip.";
            foreach (var n in names) { if (!SaveGame.Current.seen.Contains(n)) continue; var nm = n; p.rows.Add(new Menu.Row { label = TheaterLabel(n), submit = () => StartCoroutine(Theater(nm, fromTitle)) }); }
            p.rows.Add(new Menu.Row { label = "Back", submit = () => Menu.I.Back() });
            return p;
        }
        public int TheaterPlays { get; private set; }
        public string TheaterLast { get; private set; }
        public IEnumerator Theater(string name, bool fromTitle)
        {
            if (Busy) yield break;
            TheaterPlays++; TheaterLast = name;
            PlayLog.Line($"theater: {name}");
            Vector3 spot = Vector3.zero; float yaw = 0;
            if (!fromTitle) { SaveGame.Save("theater"); spot = Party.I.Player.transform.position; yaw = Party.I.Player.transform.eulerAngles.y; }
            Time.timeScale = 1f;
            Hud.SetFade(1f);
            yield return Film(name, true);
            // back as it was: the save reloaded, its world rebuilt (the film may have opened things), the party where it stood
            Hud.SetFade(1f);
            SaveGame.Disabled = false; Cutscenes.AllowSkip = true; Busy = false;
            SaveGame.Load();
            World = WorldData.Load(SavedWorld);
            // Round 11, item 0: back on the title BEFORE the world is rebuilt. The film left State on Playing, so for one frame the rebuilt
            // world ran its own story (a necklace ready to wake started WakeNecklace, set Busy and waited forever on the title: every
            // later Theater pick did nothing until a restart, and Continue then played the wake twice).
            if (fromTitle) { State = Mode.Title; BuildWorld(); Hud.SetFade(0f); ToTitle(); Menu.I.Push(TheaterPage(true)); yield break; }
            BuildWorld();
            Party.I.PlaceAll(spot, Quaternion.Euler(0, yaw, 0)); CameraRig.I.SnapBehind();
            State = Mode.Playing;
            yield return null;
            for (float t = 1; t > 0f; t -= Time.deltaTime / .4f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(0f);
            OnAreaChanged();
        }

        // Fix 7.2, item 3: the safety net. Pushing the stick for ~15 s without getting more than 4 m anywhere = stuck: a small hint.
        public const float StuckSeconds = 15f, StuckRange = 4f;
        public int StuckHints { get; private set; }
        Vector3 stuckAnchor; float stuckFor; bool stuckShown;
        void WatchStuck()
        {
            var p = Party.I.Player.transform.position;
            if ((p - stuckAnchor).sqrMagnitude > StuckRange * StuckRange) { stuckAnchor = p; stuckFor = 0; stuckShown = false; return; }
            if (Busy || InCutscene || Party.I.Frozen) return;
            if (Controls.Move.ReadValue<Vector2>().sqrMagnitude > .25f) stuckFor += Time.deltaTime;
            if (stuckFor > StuckSeconds && !stuckShown)
            {
                stuckShown = true; StuckHints++;
                Hud.Message($"Stuck? Pause ({Controls.Btn(Controls.Act.Pause)}) > Return to checkpoint", 6f);
            }
        }

        public void Resume()
        {
            if (State != Mode.Paused) return;
            Menu.I.Close(); State = Mode.Playing; Time.timeScale = 1f;
            PlayLog.Line("resumed");
        }

        public void Quit()
        {
            if (State != Mode.Title) SaveGame.Save("quit");
            PlayLog.Line("quit");
            if (CheckRun.Active) return;   // a check decides when to close
            Application.Quit();
        }

        void OnApplicationQuit()
        {
            Necklace.StopRumble();
            if (State != Mode.Title && !CheckRun.Active) SaveGame.Save("closed");
        }

        void Update()
        {
            if (State == Mode.Playing && !InCutscene && Controls.Pause.WasPressedThisFrame()) Pause();
            else if (State == Mode.Paused && Controls.Pause.WasPressedThisFrame() && Menu.I.Current != null && Menu.I.Current.title == "Paused") Resume();
            if (State == Mode.Title)
            {
                titleSpin += Time.unscaledDeltaTime * 4f;
                if (TestArea)
                {
                    var c = new Vector3(0, 2f, -28f);
                    titleCam.transform.position = c + Quaternion.Euler(0, 200f + Mathf.Sin(titleSpin * .05f) * 25f, 0) * new Vector3(0, 9f, 26f);
                    titleCam.transform.rotation = Quaternion.LookRotation(c - titleCam.transform.position);
                }
                else if (InMoon)
                {
                    // Round 13: the landing site, the rocket on the gray dust, Earth huge in the deep blue sky
                    float s = Mathf.Sin(titleSpin * .04f);
                    titleCam.transform.position = new Vector3(-16f + s * 6f, 4f, -22f);
                    titleCam.transform.rotation = Quaternion.LookRotation(new Vector3(4f + s * 3f, 14f, 30f) - titleCam.transform.position);
                }
                else if (InTomorrowland)
                {
                    // Round 12: the neon avenue at dusk, the chrome towers, the Rocket Terminal far off
                    float s = Mathf.Sin(titleSpin * .04f);
                    titleCam.transform.position = new Vector3(-4f + s * 4f, 5f, 4f);
                    titleCam.transform.rotation = Quaternion.LookRotation(new Vector3(0f + s * 3f, 9f, 120f) - titleCam.transform.position);
                }
                else if (InSteampunk)
                {
                    // Round 11: the square in the morning, the great clock, the brick and cream terraces
                    float s = Mathf.Sin(titleSpin * .04f);
                    titleCam.transform.position = new Vector3(-18f + s * 8f, 7f, 168f);
                    titleCam.transform.rotation = Quaternion.LookRotation(new Vector3(0f + s * 4f, 6f, 205f) - titleCam.transform.position);
                }
                else if (InPirate)
                {
                    // Round 10: the ship at anchor in the sunset, the island behind
                    float s = Mathf.Sin(titleSpin * .04f);
                    titleCam.transform.position = new Vector3(-22f + s * 6f, 9f, -30f);
                    titleCam.transform.rotation = Quaternion.LookRotation(new Vector3(4f + s * 3f, 8f, 4f) - titleCam.transform.position);
                }
                else if (InMedieval)
                {
                    // Round 8: from the drawbridge into the torch-lit courtyard, the keep and its spire behind
                    float s = Mathf.Sin(titleSpin * .04f);
                    titleCam.transform.position = new Vector3(-6f + s * 6f, 6f, -26f);
                    titleCam.transform.rotation = Quaternion.LookRotation(new Vector3(0f + s * 4f, 14f, 60f) - titleCam.transform.position);
                }
                else if (InBethlehem)
                {
                    // Round 7: over the farmyard toward the barn under the star, the hills behind
                    float s = Mathf.Sin(titleSpin * .04f);
                    titleCam.transform.position = new Vector3(-14f + s * 8f, 9f, -16f);
                    titleCam.transform.rotation = Quaternion.LookRotation(new Vector3(0f + s * 6f, 12f, 40f) - titleCam.transform.position);
                }
                else if (InEgypt)
                {
                    // Round 6: over the town toward the pyramids and the Sphinx
                    float s = Mathf.Sin(titleSpin * .04f);
                    titleCam.transform.position = new Vector3(10f + s * 12f, 24f, -20f);
                    titleCam.transform.rotation = Quaternion.LookRotation(new Vector3(-10f + s * 30f, 30f, 300f) - titleCam.transform.position);
                }
                else
                {
                    // the valley from above the landing spot, toward the volcanoes, swaying slowly
                    float s = Mathf.Sin(titleSpin * .04f);
                    titleCam.transform.position = new Vector3(18f + s * 10f, 16f, -14f);
                    titleCam.transform.rotation = Quaternion.LookRotation(new Vector3(-10f + s * 25f, 40f, 400f) - titleCam.transform.position);
                }
            }
            MusicFor();
            if (Party.I && (State == Mode.Playing))
            {
                var area = Area;
                if (Party.I.Companion && !Party.I.Companion.gameObject.activeSelf) Party.I.Companion.gameObject.SetActive(true); var prog = SaveGame.Current.World(World.worldId);
                Necklace.Tick(area != null ? area.id : "", area != null && area.requirement > 0 && prog.necklaceAwake && !prog.storyShard);
                // Fix 6.1: the necklace wakes when the universal total reaches this world's threshold (also on arriving with enough already);
                // Fix 8.1: and at least `fromHere` shards come from this world (a world that is already awake never relocks)
                if (area != null && area.requirement > 0 && !prog.necklaceAwake && !prog.storyShard && !Busy && !InCutscene && Ready(area))
                    StartCoroutine(WakeNecklace(area));
                if (area != null) { ApplyKiraForm(area); ApplyOutfits(area); }
                if (Party.I.Companion && area != null) Party.I.Companion.SetOutfit(string.IsNullOrEmpty(area.outfit) ? Outfits.Home : area.outfit);
                WatchStuck();
                bool dark = DarkVolume.At(Party.I.Player.Motor.Center) && !InCutscene;
                Look.InVortex(Vortex.Inside(Party.I.Player.transform.position) && !InCutscene);
                Look.SetDark(dark);
                foreach (var h in Party.I.Heroes) NecklaceModel.Light(h.NecklaceGem, dark);
            }
        }

        // Round 5, item 4: Dan's tracks by place. The title screen: "title"; Act I: "act1", faded out over the white-out; the
        // arrival and the valley: "iceage"; the vortex space: "vortex" (crossfades both ways); the test area: silence.
        public const float ActOneMusicFadeAt = 58.4f;   // Round 9: Act I is 60 s, timed to Dan's 60 s act1.mp3
        bool actOneFaded;
        void MusicFor()
        {
            if (State == Mode.Title) { Music.Play(TestArea ? null : "title"); return; }
            if (TestArea) { Music.Play(null); return; }
            if (Cutscenes.Playing == "ActOne")
            {
                if (Cutscenes.Director && Cutscenes.Director.time >= ActOneMusicFadeAt) { Music.Play(null, 2.2f); actOneFaded = true; }   // out by the full white-out
                else if (!actOneFaded) Music.Play("act1", .3f);   // (never back on: as Act I ends its director's time goes back to 0)
                return;
            }
            actOneFaded = false;
            if (Cutscenes.Playing == "IceAgeArrival") { Music.Play("iceage", .8f); return; }
            if (Cutscenes.Playing == "EgyptArrival") { Music.Play("egypt", .8f); return; }   // Round 6: Dan's Egypt song (silence if missing)
            if (Cutscenes.Playing == "BethlehemArrival") { Music.Play("bethlehem", .8f); return; }   // Round 7: Dan's Bethlehem song
            if (Cutscenes.Playing == "WiseMenArrive") { Music.Play(null, 1.2f); return; }   // the finale: the choir sting is the cutscene's own sound
            if (Cutscenes.Playing == "MedievalArrival") { Music.Play("medieval", .8f); return; }   // Round 8: Dan's Medieval song
            if (Cutscenes.Playing == "TheStone") { Music.Play(null, 1.2f); return; }
            if (Cutscenes.Playing == "PirateArrival" || Cutscenes.Playing == "ShardOverboard") { Music.Play("pirate", .8f); return; }   // Round 10: Dan's ship song
            if (Cutscenes.Playing == "SeaVortex") { Music.Play(null, 1.2f); return; }
            if (Cutscenes.Playing == "SteampunkArrival" || Cutscenes.Playing == "MuscleManHero") { Music.Play("steampunk", .8f); return; }   // Round 11: Dan's steampunk song
            if (Cutscenes.Playing == "ClockShard") { Music.Play(null, 1.2f); return; }
            if (Cutscenes.Playing == "TomorrowlandArrival") { Music.Play("tomorrowland", .8f); return; }   // Round 12: Dan's tomorrowland.mp3
            if (Cutscenes.Playing == "RocketLaunch" || Cutscenes.Playing == "WhiteShard") { Music.Play(null, 1.2f); return; }   // their own sound
            if (Cutscenes.Playing == "MoonLanding" || Cutscenes.Playing == "KiraFloats") { Music.Play("moon", .8f); return; }   // Round 13: Dan's moon.mp3
            if (State != Mode.Playing || !Party.I) return;   // paused: keep what plays
            Music.Play(Vortex.Inside(Party.I.Player.transform.position) ? "vortex" : InMoon ? "moon" : InTomorrowland ? "tomorrowland" : InSteampunk ? "steampunk" : InPirate ? (Pirate.Underwater ? "underwater" : "pirate") : InMedieval ? "medieval" : InBethlehem ? "bethlehem" : InEgypt ? "egypt" : "iceage");
        }

        // ---- progression
        public void OnCollected(Pickup p)
        {
            var prog = SaveGame.Current.World(World.worldId);
            if (p.kind == Pickup.Kind.StoryShard) foreach (var h in Party.I.Heroes) h.Cheer();   // Round 3: everyone cheers the story shard
            else if (p.kind == Pickup.Kind.Shard) Party.I.Player.Cheer();
            if (Party.I.Companion && p.kind != Pickup.Kind.Heal) Party.I.Companion.Cheer();   // Round 8: Muscle Man cheers every pickup
            if (p.kind == Pickup.Kind.StoryShard && InPirate) { StartCoroutine(p.id == Pirate.ChestShardId ? Overboard() : SeaFinale()); return; }   // Round 10
            if (p.kind == Pickup.Kind.StoryShard && InTomorrowland) { PinkShardTaken(); return; }   // Round 12: carried to the rocket's fuel socket
            if (p.kind == Pickup.Kind.StoryShard && InMoon) return;   // Round 13: the white shard is the rover's puzzle (Moon.RoverPanel)
            if (p.kind == Pickup.Kind.StoryShard) StartCoroutine(TestArea ? OpenPortal() : InMedieval ? StoneFinale() : InBethlehem ? WiseMenFinale() : OpenVortex());
        }

        IEnumerator WakeNecklace(AreaData area)
        {
            Busy = true;
            var prog = SaveGame.Current.World(World.worldId); prog.necklaceAwake = true;
            PlayLog.Line($"area {area.id}: {SaveGame.Current.Total} shards in all (threshold {area.requirement} and {area.fromHere} from here; {AreaShards(area.id)} from here), the necklace wakes");
            yield return new WaitForSeconds(.4f);
            // not mid-air (a vent, a jump) and not in a cave (its cameras need the open sky): it waits for the party to come out
            while ((!Party.I.Player.Motor.Grounded && Party.I.Player.Motor.Mode != MoveMode.Swim) || DarkVolume.At(Party.I.Player.Motor.Center) || !Game.Playing)
            {
                // Round 11, item 0: never left waiting on the title screen (or a rebuilt world) with Busy set: it wakes again in play
                if (State == Mode.Title || !Party.I) { prog.necklaceAwake = false; Busy = false; yield break; }
                yield return null;
            }
            var player = Party.I.Player.transform;
            var to = StoryShard.transform.position - player.position; to.y = 0;
            var at = player.position;
            if (Physics.Raycast(at + Vector3.up * .5f, Vector3.down, out var ground, 30f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << 4) | (1 << 2)), QueryTriggerInteraction.Ignore)) at = ground.point;   // on the ground, even mid-jump
            Sfx.Play("necklace", .7f);
            yield return Cutscenes.Play(InMoon ? "NecklaceWakesMoon" : InTomorrowland ? "NecklaceWakesTomorrowland" : InSteampunk ? "NecklaceWakesSteampunk" : InPirate ? "NecklaceWakesPirate" : InMedieval ? "NecklaceWakesMedieval" : InBethlehem ? "NecklaceWakesBethlehem" : InEgypt ? "NecklaceWakesEgypt" : "NecklaceWakes", at, Quaternion.LookRotation(to.sqrMagnitude > .01f ? to : player.forward));
            if (InPirate) Pirate.ShowStoryShard(); else { StoryShard.gameObject.SetActive(true); StoryBeam.SetActive(true); }   // Round 10: the chest first, the sea floor after it goes overboard
            if (InMedieval && Medieval.StuckShard) Medieval.StuckShard.SetActive(false);   // the pickup takes its place in the stone
            Hud.Message(InMoon ? "The necklace is awake! It pulls toward the ridge... the lunar rover." : InTomorrowland ? "The necklace is awake! It glows pink and pulls toward the Skyline Tower: its glass lift is lit." : InSteampunk ? "The necklace is awake! It pulls toward the square... the great clock." : InPirate ? "The necklace is awake! It pulls toward the treasure chest by the rail..." : InMedieval ? "The necklace is awake! It pulls toward the courtyard... toward the stone." : InBethlehem ? "The necklace is awake! Follow it... back to the barn? Something is glowing in the manger." : "The necklace is awake! Follow it to the story shard.", 5f);
            SaveGame.Save("necklace awake");
            Busy = false;
        }

        IEnumerator OpenPortal()
        {
            Busy = true;
            var prog = SaveGame.Current.World(World.worldId); prog.storyShard = true;
            StoryBeam.SetActive(false);
            yield return new WaitForSeconds(.3f);
            yield return Cutscenes.Play("PortalOpens", PortalA.transform.position, PortalA.transform.rotation);
            if (!SaveGame.Current.portals.Contains(PortalA.id)) SaveGame.Current.portals.Add(PortalA.id);
            Hud.Message("The story shard opened the portal to Area B!", 5f);
            SaveGame.Save("story shard");
            Busy = false;
        }

        // Item 5: the story shard opens the world's vortex
        IEnumerator OpenVortex()
        {
            Busy = true;
            var prog = SaveGame.Current.World(World.worldId); prog.storyShard = true;
            StoryBeam.SetActive(false);
            yield return new WaitForSeconds(.3f);
            var g = Gate;
            yield return Cutscenes.Play(InEgypt ? "VortexOpensEgypt" : "VortexOpens", g.transform.position, g.transform.rotation);
            if (!SaveGame.Current.portals.Contains(g.id)) SaveGame.Current.portals.Add(g.id);
            Hud.Message($"The {(InBethlehem ? "golden" : InEgypt ? "red" : "blue")} story shard opened a vortex! Step in to travel. ({SaveGame.Current.StoryShards} of {Vortex.StoryShardsTotal} story shards)", 6f);
            SaveGame.Save("story shard");
            Busy = false;
        }

        // Round 7, item 10: the golden shard in the manger. WiseMenArrive (skippable): the Wise Men walk in with their presents, the choir
        // swells, they find a glowing shard and three confused travellers (one a lamb with a cat's face), look at each other and leave
        // disappointed; the hay bursts up into a spinning golden vortex that pulls the party in -> the vortex space. The vortex stays open
        // in the barn afterwards (the gate back).
        public int Finales { get; private set; }
        IEnumerator WiseMenFinale()
        {
            Busy = true; Finales++;
            var prog = SaveGame.Current.World(World.worldId); prog.storyShard = true;
            StoryBeam.SetActive(false);
            if (!SaveGame.Current.portals.Contains(Bethlehem.Gate.id)) SaveGame.Current.portals.Add(Bethlehem.Gate.id);
            SaveGame.Save("story shard");
            yield return new WaitForSeconds(.3f);
            var at = new Vector3(Bethlehem.MangerAt.x, Bethlehem.Ground(Bethlehem.MangerAt.x, 33f, 2f), 33f);
            Party.I.PlaceAll(at, Quaternion.Euler(0, 0, 0)); CameraRig.I.SnapBehind();
            yield return Cutscenes.Play("WiseMenArrive", at, Quaternion.identity);
            Bethlehem.Gate.gameObject.SetActive(true);
            PlayLog.Line("the Wise Men's finale: into the vortex");
            for (float t = 0; t < 1f; t += Time.deltaTime / .5f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(1f);
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind();
            SaveGame.Save("vortex");
            yield return null;
            for (float t = 1; t > 0; t -= Time.deltaTime / .6f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(0f);
            Hud.Message($"The golden shard! {SaveGame.Current.StoryShards} of {Vortex.StoryShardsTotal} story shards. The way to the Medieval Kingdom is open.", 6f);
            Busy = false;
        }

        // Round 8, item 10: the purple shard in the stone. TheStone (skippable, the film's scene): Muscle Man is there first, brags, heaves with
        // everything he has, fails and falls flat on his back; Dan steps up, the shard answers his necklace and slides out with no effort;
        // Muscle Man laughs, then gapes: "My hero!" (Round 9); the cobblestones swirl into a whirlpool that takes him along with the party -> the vortex space. From then
        // on he follows the party (Abilities.MuscleMan: the boulders, portcullises and boosts in every world).
        IEnumerator StoneFinale()
        {
            Busy = true; Finales++;
            var prog = SaveGame.Current.World(World.worldId); prog.storyShard = true;
            StoryBeam.SetActive(false);
            if (!SaveGame.Current.portals.Contains(Medieval.Gate.id)) SaveGame.Current.portals.Add(Medieval.Gate.id);
            if (!SaveGame.Current.abilities.Contains(Abilities.MuscleMan)) SaveGame.Current.abilities.Add(Abilities.MuscleMan);
            SaveGame.Save("story shard");
            yield return new WaitForSeconds(.3f);
            var at = Medieval.OnGround(Medieval.StoneAt.x, Medieval.StoneAt.z - 3f);
            Party.I.PlaceAll(at, Quaternion.identity); CameraRig.I.SnapBehind();
            foreach (var r in RivalMuscleMan.All.ToArray()) r.gameObject.SetActive(false);
            yield return Cutscenes.Play("TheStone", new Vector3(Medieval.StoneAt.x, at.y, Medieval.StoneAt.z), Quaternion.identity);
            Medieval.Gate.gameObject.SetActive(true);
            Party.I.AddCompanion(at + Vector3.back * 2f, Quaternion.identity);
            PlayLog.Line("the stone's finale: Muscle Man joins; into the vortex");
            for (float t = 0; t < 1f; t += Time.deltaTime / .5f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(1f);
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind();
            SaveGame.Save("vortex");
            yield return null;
            for (float t = 1; t > 0; t -= Time.deltaTime / .6f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(0f);
            Hud.Message($"The purple shard! {SaveGame.Current.StoryShards} of {Vortex.StoryShardsTotal} story shards... and Muscle Man came too. He moves boulders, lifts portcullises and boosts you up: press {Controls.Btn(Controls.Act.Attack)} near one.", 8f);
            Busy = false;
        }

        // Round 10, item 5: the chest by the rail. ShardOverboard (skippable, Dan's clip): Muscle Man grabs the green shard, holds it up laughing,
        // it slips, bounces and rolls across the deck, under the rail and over the side; he leaps in after it, Dan and Abi follow, otter Kira
        // dives. Then the deep sea opens (Abilities.Overboard is a world flag: "PS-overboard") and play goes on in the water by the ship.
        public int Overboards { get; private set; }
        IEnumerator Overboard()
        {
            Busy = true; Overboards++;
            var prog = SaveGame.Current.World(World.worldId);
            yield return new WaitForSeconds(.3f);
            var at = Pirate.ChestAt + new Vector3(-1.4f, 0, 0);
            Party.I.PlaceAll(at, Quaternion.Euler(0, 90, 0)); CameraRig.I.SnapBehind();
            Pirate.SetChestOpen(true);
            yield return Cutscenes.Play("ShardOverboard", Pirate.ChestAt, Quaternion.identity);
            Pirate.OpenDeepSea(true);
            if (!prog.opened.Contains(Pirate.OverboardFlag)) prog.opened.Add(Pirate.OverboardFlag);
            Party.I.PlaceAll(Pirate.SplashAt, Quaternion.Euler(0, 270, 0)); CameraRig.I.SnapBehind();
            SaveGame.Current.checkpoint = Pirate.SeaCp; SaveGame.Save("overboard");
            Pirate.ShowStoryShard();
            PlayLog.Line("the green shard went overboard: the deep sea is open");
            Hud.Message($"Overboard! The green shard sank... follow the necklace down. Hold {Controls.Btn(Controls.Act.Dive)} to dive; air bubbles from the sea floor give Dan and Abi their breath back.", 8f);
            Busy = false;
        }

        // Round 10, item 11: the green shard on the sea floor: a swirling underwater vortex pulls everyone in, Muscle Man too -> the vortex space
        IEnumerator SeaFinale()
        {
            Busy = true; Finales++;
            var prog = SaveGame.Current.World(World.worldId); prog.storyShard = true;
            StoryBeam.SetActive(false);
            if (!SaveGame.Current.portals.Contains(Pirate.Gate.id)) SaveGame.Current.portals.Add(Pirate.Gate.id);
            SaveGame.Save("story shard");
            yield return new WaitForSeconds(.3f);
            var at = Pirate.StoryAt;
            yield return Cutscenes.Play("SeaVortex", at, Quaternion.Euler(0, 90, 0));
            Pirate.Gate.gameObject.SetActive(true);
            PlayLog.Line("the green shard's finale: into the vortex");
            for (float t = 0; t < 1f; t += Time.deltaTime / .5f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(1f);
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind();
            SaveGame.Save("vortex");
            yield return null;
            for (float t = 1; t > 0; t -= Time.deltaTime / .6f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(0f);
            Hud.Message($"The green shard! {SaveGame.Current.StoryShards} of {Vortex.StoryShardsTotal} story shards. The way to Victorian Steampunk is open.", 6f);
            Busy = false;
        }

        // the vortex gate -> the vortex space
        public void EnterVortex(Portal from)
        {
            if (Busy) return;
            SaveGame.Current.checkpoint = Vortex.Checkpoint;
            Checkpoint.RespawnParty();
            Sfx.Play("portal", .6f);
            PlayLog.Line($"into the vortex from {from.id}");
            SaveGame.Save("vortex");
            Hud.Message("The vortex: walk a path to choose where it takes you.", 5f);
        }

        // a path in the vortex space
        public void TakeVortexPath(Vortex.PathInfo path, VortexDoor door)
        {
            PlayLog.Line($"vortex path {path.id}{(door.Blocked ? " (blocked)" : "")}");
            if (door.Blocked && (path.id == "Bethlehem" || path.id == "Medieval" || path.id == "PirateSeas" || path.id == "Steampunk" || path.id == "Tomorrowland" || path.id == "Moon"))
            {
                Hud.Message(path.id == "Bethlehem" ? "The way to Bethlehem is still closed. Egypt's red story shard opens it." : path.id == "Medieval" ? "The way to the Medieval Kingdom is still closed. Bethlehem's golden story shard opens it." : path.id == "Steampunk" ? "The way to Victorian Steampunk is still closed. The Pirate Seas' green story shard opens it." : path.id == "Tomorrowland" ? "The way to Tomorrowland is still closed. Victorian Steampunk's teal story shard opens it." : path.id == "Moon" ? "The way to the Moon is still closed. Only the rocket from Tomorrowland has been there yet." : "The way to the Pirate Seas is still closed. The Medieval Kingdom's purple story shard opens it.", 5f);
                Sfx.Play("hurt", .3f, .6f);
                var back2 = (Party.I.Player.transform.position - door.transform.position); back2.y = 0;
                Party.I.Player.Motor.Knockback(back2.normalized * 6f + Vector3.up * 4f);
                return;
            }
            if (door.Blocked)
            {
                int missing = Vortex.StoryShardsTotal - SaveGame.Current.StoryShards;
                Hud.Message($"The way home is closed. {missing} story shard{(missing == 1 ? " is" : "s are")} still missing: rebuild them all to open it.", 5f);
                Sfx.Play("hurt", .3f, .6f);
                var back = (Party.I.Player.transform.position - door.transform.position); back.y = 0;
                Party.I.Player.Motor.Knockback(back.normalized * 6f + Vector3.up * 4f);
                return;
            }
            if (path.id == "IceAge" || path.id == "Egypt" || path.id == "Bethlehem" || path.id == "Medieval" || path.id == "PirateSeas" || path.id == "Steampunk" || path.id == "Tomorrowland" || (path.id == "Moon" && Moon.Real))
            {
                string world = path.world;
                string arrivedFlag = world == WorldData.Egypt ? "EG-arrived" : world == WorldData.Bethlehem ? "BE-arrived" : world == WorldData.Medieval ? "ME-arrived" : world == WorldData.Pirate ? "PS-arrived" : world == WorldData.Steampunk ? Steampunk.ArrivedFlag : world == WorldData.Tomorrowland ? Tomorrowland.ArrivedFlag : world == WorldData.Moon ? Moon.ArrivedFlag : null;
                bool first = arrivedFlag != null && !SaveGame.Current.World(world).opened.Contains(arrivedFlag);
                string cp = world == WorldData.Egypt ? (first ? Egypt.Start : Egypt.VortexCp) : world == WorldData.Bethlehem ? (first ? Bethlehem.Start : Bethlehem.VortexCp)
                    : world == WorldData.Medieval ? (first ? Medieval.Start : Medieval.VortexCp) : world == WorldData.Pirate ? (first ? Pirate.Start : Pirate.VortexCp) : world == WorldData.Steampunk ? (first ? Steampunk.Start : Steampunk.VortexCp) : world == WorldData.Tomorrowland ? (first ? Tomorrowland.Start : Tomorrowland.VortexCp) : world == WorldData.Moon ? (first ? Moon.Start : Moon.VortexCp) : "IA-vortex";
                if (!SaveGame.Current.worldsReached.Contains(world)) SaveGame.Current.worldsReached.Add(world);
                StartCoroutine(GoToWorld(world, cp, first));
                return;
            }
            StartCoroutine(ToBeContinued(path));
        }

        // Round 6: the vortex takes the party to another world: fade, build it, set them down (Egypt's first time: its arrival)
        public int WorldLoads { get; private set; }
        public int VortexComebacks { get; private set; }
        IEnumerator GoToWorld(string world, string checkpoint, bool arrival)
        {
            Busy = true; Party.I.Frozen = true;
            Sfx.Play("portal", .6f);
            for (float t = 0; t < 1f; t += Time.deltaTime / .6f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(1f);
            SaveGame.Current.currentWorld = world; SaveGame.Current.checkpoint = checkpoint;
            var sv = SaveGame.Current;   // Fix 10.1: back where Pause > Return to the vortex left this world
            bool back = !arrival && sv.returnWorld == world;
            if (back && !string.IsNullOrEmpty(sv.returnCp)) SaveGame.Current.checkpoint = sv.returnCp;
            if (World.worldId != world) { World = WorldData.Load(world); BuildWorld(); WorldLoads++; }
            else Checkpoint.RespawnParty();
            if (back)
            {
                if (sv.returnSpot != Vector3.zero) Party.I.PlaceAll(sv.returnSpot + Vector3.up * .1f, Quaternion.Euler(0, sv.returnYaw, 0));
                PlayLog.Line($"back where the party left for the vortex: {(sv.returnSpot != Vector3.zero ? sv.returnSpot.ToString("F1") : "")} (checkpoint {sv.checkpoint})");
                VortexComebacks++;
            }
            if (!arrival) sv.returnWorld = "";
            CameraRig.I.SnapBehind();
            SaveGame.Save("vortex to " + world);
            PlayLog.Line($"the vortex: to {world} (checkpoint {checkpoint})");
            yield return null;
            Hud.SetFade(0f); Party.I.Frozen = false; Busy = false;
            if (!arrival && InSteampunk && !SaveGame.Current.World(world).opened.Contains(Steampunk.PressFlag)) Steampunk.StartBeltRun();   // Round 11: left mid-belt: the run again
            if (arrival) yield return world == WorldData.Moon ? ArriveOnMoon() : world == WorldData.Tomorrowland ? ArriveInTomorrowland() : world == WorldData.Steampunk ? ArriveInSteampunk() : world == WorldData.Pirate ? ArriveInPirate() : world == WorldData.Medieval ? ArriveInMedieval() : world == WorldData.Bethlehem ? ArriveInBethlehem() : ArriveInEgypt();
            else OnAreaChanged();
        }

        // Round 7, item 3: Bethlehem's arrival (BethlehemArrival, skippable): through the barn's roof; then play starts in the barn
        IEnumerator ArriveInBethlehem()
        {
            Busy = true;
            var a = World.areas[0]; var face = Quaternion.Euler(0, a.spawnYaw, 0);
            var at = Bethlehem.ArriveAt;
            Party.I.PlaceAll(at + Vector3.up * .1f, face); CameraRig.I.SnapBehind();
            yield return Cutscenes.Play("BethlehemArrival", new Vector3(0, at.y, 34f), Quaternion.identity);
            Party.I.PlaceAll(at + Vector3.up * .1f, face); CameraRig.I.SnapBehind();
            var prog = SaveGame.Current.World(WorldData.Bethlehem); if (!prog.opened.Contains("BE-arrived")) prog.opened.Add("BE-arrived");
            SaveGame.Current.checkpoint = Bethlehem.Start; SaveGame.Save("arrived in Bethlehem");
            Hud.Message($"Bethlehem! Kira is a little lamb. At {RuleText(a)}, the golden shard on Dan's necklace wakes ({Progress(a)}).", 6f);
            Busy = false;
        }

        // Round 8, item 3: the Medieval Kingdom's arrival (MedievalArrival, skippable): into the hay wagon, Kira's sneeze, the wagon's blast;
        // then play starts in the courtyard
        IEnumerator ArriveInMedieval()
        {
            Busy = true;
            var a = World.areas[0]; var face = Quaternion.Euler(0, a.spawnYaw, 0);
            var at = Medieval.ArriveAt;
            Party.I.PlaceAll(at + Vector3.up * .1f, face); CameraRig.I.SnapBehind();
            bool rival = Medieval.CourtRival && Medieval.CourtRival.gameObject.activeSelf;
            if (Medieval.WreckSite) Medieval.WreckSite.SetActive(false); if (rival) Medieval.CourtRival.gameObject.SetActive(false);
            yield return Cutscenes.Play("MedievalArrival", new Vector3(0, at.y, 14f), Quaternion.identity);
            if (Medieval.WreckSite) Medieval.WreckSite.SetActive(true); if (rival) Medieval.CourtRival.gameObject.SetActive(true);
            Party.I.PlaceAll(at + Vector3.up * .1f, face); CameraRig.I.SnapBehind();
            var prog = SaveGame.Current.World(WorldData.Medieval); if (!prog.opened.Contains("ME-arrived")) prog.opened.Add("ME-arrived");
            SaveGame.Current.checkpoint = Medieval.Start; SaveGame.Save("arrived in the Medieval Kingdom");
            Hud.Message($"The Medieval Kingdom! Kira is a baby dragon: hold {Controls.Btn(Controls.Act.Jump)} in the air to glide, {Controls.Btn(Controls.Act.Attack)} puffs fire. At {RuleText(a)}, the purple shard on Dan's necklace wakes ({Progress(a)}).", 8f);
            Busy = false;
        }

        // Round 10, item 4: the Pirate Seas' arrival (PirateArrival, skippable, Dan's clip): the ship at night with its lanterns, a green vortex
        // swirls round it, Dan and otter Kira (and Abi) pop out of barrels, Muscle Man drops through the sails and crashes flat on the deck,
        // lies there, sits up grinning; Abi laughs; the necklace glows green. Then play starts on the main deck at sunset.
        IEnumerator ArriveInPirate()
        {
            Busy = true;
            var a = World.areas[0]; var face = Quaternion.Euler(0, a.spawnYaw, 0);
            var at = Pirate.ArriveAt;
            Party.I.PlaceAll(at + Vector3.up * .1f, face); CameraRig.I.SnapBehind();
            Pirate.SetNight(true);
            yield return Cutscenes.Play("PirateArrival", Pirate.ArriveAt, Quaternion.identity);
            Pirate.SetNight(false);
            Party.I.PlaceAll(at + Vector3.up * .1f, face); CameraRig.I.SnapBehind();
            var prog = SaveGame.Current.World(WorldData.Pirate); if (!prog.opened.Contains("PS-arrived")) prog.opened.Add("PS-arrived");
            SaveGame.Current.checkpoint = Pirate.Start; SaveGame.Save("arrived in the Pirate Seas");
            Hud.Message($"The Pirate Seas! Kira is a little otter: she swims fast and deep, and belly-slides ({Controls.Btn(Controls.Act.Dive)} while running). At {RuleText(a)}, the green shard on Dan's necklace wakes ({Progress(a)}).", 8f);
            Busy = false;
        }

        // Round 11, item 4: Victorian Steampunk's arrival (SteampunkArrival, skippable, Dan's clip): night over the factory city, the teal
        // vortex, down onto the chain line into the copper bolt bucket, tipped out onto the belt. Then the belt run (item 5) starts at once.
        IEnumerator ArriveInSteampunk()
        {
            Busy = true;
            var at = Steampunk.ArriveAt;
            Party.I.PlaceAll(at + Vector3.up * .1f, Quaternion.identity); CameraRig.I.SnapBehind();
            yield return Cutscenes.Play("SteampunkArrival", Steampunk.ArriveAt, Quaternion.identity);
            var prog = SaveGame.Current.World(WorldData.Steampunk); if (!prog.opened.Contains(Steampunk.ArrivedFlag)) prog.opened.Add(Steampunk.ArrivedFlag);
            SaveGame.Current.checkpoint = Steampunk.Start; SaveGame.Save("arrived in Victorian Steampunk");
            Busy = false;
            Steampunk.StartBeltRun();
        }

        // Round 11, item 5: the end of the belt run: MuscleManHero (Dan's clip): the dome press comes down on Dan, Abi and Kira; Muscle Man
        // holds it up (red alarms, snapping cables, sparks) and heaves it off; it smashes out through the wall. Then the factory is free.
        public int PressScenes { get; private set; }
        public IEnumerator PressScene()
        {
            if (Busy) yield break;
            Busy = true; PressScenes++;
            Party.I.PlaceAll(Steampunk.PressAt + new Vector3(0, .05f, -1.5f), Quaternion.identity); CameraRig.I.SnapBehind();
            Steampunk.PrepareScene();
            yield return Cutscenes.Play("MuscleManHero", Steampunk.PressAt, Quaternion.identity);
            var prog = SaveGame.Current.World(WorldData.Steampunk); if (!prog.opened.Contains(Steampunk.PressFlag)) prog.opened.Add(Steampunk.PressFlag);
            Steampunk.SetPressGone(true);
            Party.I.PlaceAll(Steampunk.AfterPress + Vector3.up * .1f, Quaternion.identity); CameraRig.I.SnapBehind();
            SaveGame.Current.checkpoint = Steampunk.FactoryCp; SaveGame.Save("the press is gone");
            PlayLog.Line("the press smashed out through the wall: the factory is free");
            var a = World.areas[0];
            Hud.Message($"Muscle Man saved the day! Kira is clockwork now: she winds up machines from their brass sockets, and her magnet paws climb riveted pillars ({Controls.Btn(Controls.Act.Jump)} at the foot). At {RuleText(a)}, the teal shard on Dan's necklace wakes ({Progress(a)}).", 9f);
            Busy = false;
            OnAreaChanged();
        }

        // Round 11, item 11: the great clock: the teal gear shard sits in its face, too high; ClockShard (Kira spots it, Muscle Man lifts Dan,
        // Dan takes it); then the teal vortex opens (the game's change) and takes all four -> the vortex space
        public IEnumerator ClockFinale()
        {
            if (Busy) yield break;
            Busy = true; Finales++;
            var prog = SaveGame.Current.World(World.worldId); prog.storyShard = true;
            StoryBeam.SetActive(false);
            if (!SaveGame.Current.portals.Contains(Steampunk.Gate.id)) SaveGame.Current.portals.Add(Steampunk.Gate.id);
            SaveGame.Save("story shard");
            yield return new WaitForSeconds(.2f);
            Party.I.PlaceAll(Steampunk.ClockAt + new Vector3(0, .05f, -4f), Quaternion.identity); CameraRig.I.SnapBehind();
            if (Steampunk.ShardVisual) Steampunk.ShardVisual.SetActive(false);
            yield return Cutscenes.Play("ClockShard", Steampunk.ClockAt, Quaternion.identity);
            Steampunk.Gate.gameObject.SetActive(true);
            foreach (var h in Party.I.Heroes) h.Cheer();
            PlayLog.Line("the teal shard's finale: into the vortex");
            for (float t = 0; t < 1f; t += Time.deltaTime / .5f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(1f);
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind();
            SaveGame.Save("vortex");
            yield return null;
            for (float t = 1; t > 0; t -= Time.deltaTime / .6f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(0f);
            Hud.Message($"The teal shard! {SaveGame.Current.StoryShards} of {Vortex.StoryShardsTotal} story shards. The way to Tomorrowland is open.", 6f);
            Busy = false;
        }

        // Round 12, item 4: Tomorrowland's arrival (TomorrowlandArrival, skippable; the film has none): the teal vortex drops all four onto the
        // neon avenue at dusk; a shimmer, their retro-future outfits, Kira's robot body; Muscle Man gapes at a flying car; the greeter robot
        // rolls up: "Your rocket is ready for departure!" and points at the Rocket Terminal sign; Dan's necklace glows pink.
        IEnumerator ArriveInTomorrowland()
        {
            Busy = true;
            var a = World.areas[0]; var face = Quaternion.Euler(0, a.spawnYaw, 0);
            var at = Tomorrowland.ArriveAt;
            Party.I.PlaceAll(at + Vector3.up * .1f, face); CameraRig.I.SnapBehind();
            foreach (var g in Greeter.All.ToArray()) if (g) g.gameObject.SetActive(false);
            yield return Cutscenes.Play("TomorrowlandArrival", Tomorrowland.ArriveAt, Quaternion.identity);
            foreach (var g in Greeter.All.ToArray()) if (g) g.gameObject.SetActive(true);
            foreach (var g in Object.FindObjectsByType<Greeter>(FindObjectsInactive.Include, FindObjectsSortMode.None)) g.gameObject.SetActive(true);
            Party.I.PlaceAll(at + Vector3.up * .1f, face); CameraRig.I.SnapBehind();
            var prog = SaveGame.Current.World(WorldData.Tomorrowland); if (!prog.opened.Contains(Tomorrowland.ArrivedFlag)) prog.opened.Add(Tomorrowland.ArrivedFlag);
            SaveGame.Current.checkpoint = Tomorrowland.Start; SaveGame.Save("arrived in Tomorrowland");
            Hud.Message($"Tomorrowland! Kira is a little robot: on a glowing terminal pad she plugs in and takes over machines; {Controls.Btn(Controls.Act.Dive)} on the ground rolls her fast on her wheels. At {RuleText(a)}, the pink glow on Dan's necklace wakes ({Progress(a)}).", 9f);
            Busy = false;
        }

        // Round 13, item 2: the Moon's arrival (MoonLanding, skippable; continues RocketLaunch): the rocket comes down on its flame and touches
        // down in a puff of dust; inside, helmets click on; the hatch opens; the four bounce down the ladder in slow motion; Earth rises huge
        // behind them; Muscle Man does a big slow jump and laughs. The Moon's path in the vortex is open from now on.
        IEnumerator ArriveOnMoon()
        {
            Busy = true;
            var a = World.areas[0]; var face = Quaternion.Euler(0, a.spawnYaw, 0);
            var at = Moon.OnGround(Moon.ArriveAt.x, Moon.ArriveAt.z);
            Party.I.PlaceAll(at + Vector3.up * .1f, face); CameraRig.I.SnapBehind();
            if (Moon.LandedRocket) Moon.LandedRocket.SetActive(false);
            yield return Cutscenes.Play("MoonLanding", Moon.RocketGround, Quaternion.identity);
            if (Moon.LandedRocket) Moon.LandedRocket.SetActive(true);
            Party.I.PlaceAll(at + Vector3.up * .1f, face); CameraRig.I.SnapBehind();
            var prog = SaveGame.Current.World(WorldData.Moon); if (!prog.opened.Contains(Moon.ArrivedFlag)) prog.opened.Add(Moon.ArrivedFlag);
            if (!SaveGame.Current.worldsReached.Contains(WorldData.Moon)) SaveGame.Current.worldsReached.Add(WorldData.Moon);
            SaveGame.Current.checkpoint = Moon.Start; SaveGame.Save("landed on the Moon");
            Hud.Message($"The Moon! Everything floats: jumps go high and slow. Kira is in her little spacesuit: hold {Controls.Btn(Controls.Act.Jump)} in the air and she floats up. At {RuleText(a)}, the white glow on Dan's necklace wakes ({Progress(a)}).", 9f);
            Busy = false;
        }

        // Round 13, item 4: KiraFloats (the film's gag, short, skippable): on the way to the rover Kira takes a little leap and keeps floating up,
        // paddling her paws; Abi catches her by the ankle and pulls her back down. Once.
        public int FloatScenes { get; private set; }
        public IEnumerator KiraFloatsScene(Vector3 at)
        {
            if (Busy) yield break;
            Busy = true; FloatScenes++;
            var prog = SaveGame.Current.World(World.worldId); if (!prog.opened.Contains(Moon.FloatsFlag)) prog.opened.Add(Moon.FloatsFlag);
            var face = Quaternion.Euler(0, 225, 0);
            Party.I.PlaceAll(at + Vector3.up * .1f, face); CameraRig.I.SnapBehind();
            yield return Cutscenes.Play("KiraFloats", at, face);
            Party.I.PlaceAll(at + Vector3.up * .1f, face); CameraRig.I.SnapBehind();
            SaveGame.Save("Kira floats");
            Hud.Message($"Space Kira floats! Hold {Controls.Btn(Controls.Act.Jump)} in the air: she drifts up a long way (the tall rock columns).", 6f);
            Busy = false;
        }

        // Round 13, item 10: the light puzzle solved: WhiteShard (the shard answers, every shard in Dan's necklace lights up together, brilliant
        // light fills the Moon: the ornament is ready to be restored) -> the vortex with the Home path open (Acts X / XI: to be continued)
        public IEnumerator WhiteShardFinale()
        {
            if (Busy) yield break;
            Busy = true; Finales++;
            var prog = SaveGame.Current.World(World.worldId); prog.storyShard = true;
            if (!prog.opened.Contains(Moon.PuzzleFlag)) prog.opened.Add(Moon.PuzzleFlag);
            StoryBeam.SetActive(false); StoryShard.gameObject.SetActive(false);
            if (!SaveGame.Current.portals.Contains(Moon.Gate.id)) SaveGame.Current.portals.Add(Moon.Gate.id);
            SaveGame.Save("story shard");
            yield return new WaitForSeconds(.3f);
            var at = new Vector3(Moon.PanelAt.x, Moon.PlateauY, Moon.PanelAt.z);
            Party.I.PlaceAll(at + new Vector3(0, .1f, 2.4f), Quaternion.Euler(0, 180, 0)); CameraRig.I.SnapBehind();
            yield return Cutscenes.Play("WhiteShard", at, Quaternion.Euler(0, 180, 0));
            Moon.Gate.gameObject.SetActive(true);
            foreach (var h in Party.I.Heroes) h.Cheer();
            PlayLog.Line("the white shard: every shard lights up; into the vortex (Home open)");
            for (float t = 0; t < 1f; t += Time.deltaTime / .5f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(1f);
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind();
            SaveGame.Save("vortex");
            yield return null;
            for (float t = 1; t > 0; t -= Time.deltaTime / .6f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(0f);
            Hud.Message($"The white shard! All {SaveGame.Current.StoryShards} story shards: the ornament can be made whole. The way Home is open.", 7f);
            Busy = false;
        }

        // Round 12, item 5: the pink shard taken from the Skyline Tower's roof: it is the rocket's missing fuel cell (carried to the socket)
        public int PinkShards { get; private set; }
        void PinkShardTaken()
        {
            PinkShards++;
            var prog = SaveGame.Current.World(World.worldId); prog.storyShard = true;
            StoryBeam.transform.position = Tomorrowland.SocketAt + Vector3.up * 30f; StoryBeam.SetActive(true);   // the beam moves over the rocket's socket
            SaveGame.Save("the pink shard");
            PlayLog.Line("the pink shard: carried to the rocket's fuel socket");
            Hud.Message("The neon pink shard! It hums like a power cell... \"Fuel cell missing!\" Take it to the rocket in the Terminal's launch hall.", 7f);
        }

        // Round 12, item 11: the fuel socket. RocketLaunch (skippable, Dan's clip): Dan places the pink shard; the four small before the huge rocket;
        // aboard; the doors glow, the engines ignite; from below it rises into the starry dusk. It flies straight to the Moon (Round 13: the
        // landing); Tomorrowland's way back to the vortex is open from then on.
        public IEnumerator RocketFinale()
        {
            if (Busy) yield break;
            Busy = true; Finales++;
            var prog = SaveGame.Current.World(World.worldId); prog.storyShard = true;
            if (!prog.opened.Contains(Tomorrowland.LaunchFlag)) prog.opened.Add(Tomorrowland.LaunchFlag);
            StoryBeam.SetActive(false);
            if (!SaveGame.Current.portals.Contains(Tomorrowland.Gate.id)) SaveGame.Current.portals.Add(Tomorrowland.Gate.id);
            if (!SaveGame.Current.abilities.Contains(Abilities.Rocket)) SaveGame.Current.abilities.Add(Abilities.Rocket);
            SaveGame.Save("story shard");
            yield return new WaitForSeconds(.2f);
            Party.I.PlaceAll(Tomorrowland.SocketAt + new Vector3(0, .05f, -3f), Quaternion.identity); CameraRig.I.SnapBehind();
            foreach (var g in Greeter.All.ToArray()) if (g) g.gameObject.SetActive(false);
            if (Tomorrowland.RocketModel) Tomorrowland.RocketModel.SetActive(false); if (Tomorrowland.HallDome) Tomorrowland.HallDome.SetActive(false);
            yield return Cutscenes.Play("RocketLaunch", Tomorrowland.RocketAt, Quaternion.identity);
            if (Tomorrowland.RocketModel) Tomorrowland.RocketModel.SetActive(true); if (Tomorrowland.HallDome) Tomorrowland.HallDome.SetActive(true);
            foreach (var g in Object.FindObjectsByType<Greeter>(FindObjectsInactive.Include, FindObjectsSortMode.None)) g.gameObject.SetActive(true);
            Tomorrowland.Gate.gameObject.SetActive(true);
            if (!SaveGame.Current.worldsReached.Contains(WorldData.Moon)) SaveGame.Current.worldsReached.Add(WorldData.Moon);
            PlayLog.Line("the rocket launches: straight to the Moon");
            if (Moon.Real) { Busy = false; Party.I.Frozen = false; yield return GoToWorld(WorldData.Moon, Moon.Start, true); yield break; }
            // (Round 12 alone: the Moon is to be continued; the launch ends in the vortex)
            for (float t = 0; t < 1f; t += Time.deltaTime / .5f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(1f);
            Hud.Title("The Moon", "To be continued..."); ToBeContinuedShown++;
            yield return new WaitForSeconds(3f); Hud.Title(null, null);
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind();
            SaveGame.Save("vortex");
            yield return null;
            for (float t = 1; t > 0; t -= Time.deltaTime / .6f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(0f);
            Hud.Message($"The pink shard powered the rocket! {SaveGame.Current.StoryShards} of {Vortex.StoryShardsTotal} story shards. The Moon is next...", 6f);
            Busy = false;
        }

        // Round 6, item 6: Egypt's arrival (EgyptArrival, skippable), then play starts on the plaza
        IEnumerator ArriveInEgypt()
        {
            Busy = true;
            var a = World.areas[0]; var face = Quaternion.Euler(0, a.spawnYaw, 0);
            var at = Egypt.OnGround(a.spawn.x, a.spawn.z);
            Party.I.PlaceAll(at + Vector3.up * .1f, face); CameraRig.I.SnapBehind();
            yield return Cutscenes.Play("EgyptArrival", at, face);
            Party.I.PlaceAll(at + Vector3.up * .1f, face); CameraRig.I.SnapBehind();
            var prog = SaveGame.Current.World(WorldData.Egypt); if (!prog.opened.Contains("EG-arrived")) prog.opened.Add("EG-arrived");
            SaveGame.Current.checkpoint = Egypt.Start; SaveGame.Save("arrived in Egypt");
            Hud.Message($"Ancient Egypt! Every shard counts: at {RuleText(a)}, the red shard on Dan's necklace wakes ({Progress(a)}).", 6f);
            Busy = false;
        }

        // Round 6, item 7: the Sphinx gag (once): the cutscene where the party stands, facing the Sphinx
        public IEnumerator SphinxGag(Vector3 spot)
        {
            Busy = true;
            var at = Egypt.OnGround(spot.x, spot.z); var face = Quaternion.LookRotation(Egypt.SphinxAt - at, Vector3.up); face = Quaternion.Euler(0, face.eulerAngles.y, 0);
            yield return Cutscenes.Play("SphinxGag", at, face);
            Party.I.PlaceAll(at + face * new Vector3(0, .1f, 1f), face); CameraRig.I.SnapBehind();
            SaveGame.Save("the Sphinx");
            Busy = false;
        }

        IEnumerator ToBeContinued(Vortex.PathInfo path)
        {
            Busy = true; ToBeContinuedShown++;
            if (!SaveGame.Current.worldsReached.Contains(path.world)) SaveGame.Current.worldsReached.Add(path.world);
            SaveGame.Save("reached " + path.world);
            Sfx.Play("portal", .6f, .8f);
            Party.I.Frozen = true;
            for (float t = 0; t < 1f; t += Time.deltaTime / .8f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(1f);
            Hud.Title(path.label, "To be continued...");
            PlayLog.Line($"{path.label}: to be continued (saved)");
            yield return new WaitForSeconds(4f);
            Hud.Title(null, null);
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty();
            for (float t = 1; t > 0; t -= Time.deltaTime / .8f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(0f);
            Party.I.Frozen = false;
            Busy = false;
        }

        // Round 2, item 7: arriving in an area sets Kira's form (AreaData.kiraForm)
        public static void ApplyKiraForm(AreaData area)
        {
            if (!Party.I || area == null) return;
            var kira = Party.I.Heroes.Find(h => h.Stats.displayName == "Kira");
            string id = string.IsNullOrEmpty(area.kiraForm) ? KiraForm.Home : area.kiraForm;
            if (kira && (!kira.Form || kira.Form.id != id)) kira.SetForm(KiraForm.Load(id));
        }

        // Round 3: arriving in an area dresses Dan and Abi for it (AreaData.outfit)
        public static void ApplyOutfits(AreaData area)
        {
            if (!Party.I || area == null) return;
            string outfit = string.IsNullOrEmpty(area.outfit) ? Outfits.Home : area.outfit;
            foreach (var h in Party.I.Heroes) if (h.IsPerson && h.Outfit != outfit) h.SetOutfit(outfit);
        }

        public void OnAreaChanged() { var a = Area; Hud.Message(a != null ? a.name : "The vortex", 2.5f); PlayLog.Line("area " + (a != null ? a.id : "vortex")); }

        // ---- the editor's film render (Editor/CutsceneRender.cs): play one cutscene without touching the save
        public IEnumerator Film(string name, bool theater = false)
        {
            SaveGame.Disabled = true; Cutscenes.AllowSkip = theater; Busy = true;   // Busy: the world's own triggers (the Sphinx gag) stay quiet while filming
            Menu.I.Close(); titleCam.Priority = -10; State = Mode.Playing;
            bool egypt = name == "EgyptArrival" || name == "SphinxGag" || name.EndsWith("Egypt");   // Round 6: Egypt's cutscenes in Egypt
            bool beth = name == "BethlehemArrival" || name == "WiseMenArrive" || name.EndsWith("Bethlehem");   // Round 7
            bool med = name == "MedievalArrival" || name == "TheStone" || name.EndsWith("Medieval");   // Round 8
            bool sea = name == "PirateArrival" || name == "ShardOverboard" || name == "SeaVortex" || name.EndsWith("Pirate");   // Round 10
            bool steam = name == "SteampunkArrival" || name == "MuscleManHero" || name == "ClockShard" || name.EndsWith("Steampunk");   // Round 11
            bool tl = name == "TomorrowlandArrival" || name == "RocketLaunch" || name.EndsWith("Tomorrowland");   // Round 12
            bool moon = name == "MoonLanding" || name == "KiraFloats" || name == "WhiteShard" || name.EndsWith("Moon");   // Round 13
            string want = moon ? WorldData.Moon : tl ? WorldData.Tomorrowland : steam ? WorldData.Steampunk : sea ? WorldData.Pirate : med ? WorldData.Medieval : beth ? WorldData.Bethlehem : egypt ? WorldData.Egypt : WorldData.IceAge;
            if (World.worldId != want) { World = WorldData.Load(want); BuildWorld(); yield return null; }
            var a = World.areas[0];
            Vector3 at; Quaternion facing;
            if (name == "ActOne") { at = HomeAnchor; facing = Quaternion.identity; }
            else if (name == "VortexOpens" || name == "VortexOpensEgypt") { at = Gate.transform.position; facing = Gate.transform.rotation; }
            else if (name == "ViolinistTeaches" || name == "ViolinistTeachesEgypt") { var v = Violinist.I; at = v.transform.position; facing = v.transform.rotation; }
            else if (name == "NecklaceWakes") { at = IceAge.OnGround(10f, 300f); var to = StoryShard.transform.position - at; to.y = 0; facing = Quaternion.LookRotation(to); }
            else if (name == "NecklaceWakesEgypt") { at = Egypt.OnGround(0, 60); facing = Quaternion.Euler(0, 40, 0); }
            else if (name == "SphinxGag") { at = Egypt.GagSpot; facing = Quaternion.Euler(0, 270, 0); }
            else if (name == "BethlehemArrival") { at = new Vector3(0, Bethlehem.ArriveAt.y, 34f); facing = Quaternion.identity; }
            else if (name == "WiseMenArrive") { at = new Vector3(0, Bethlehem.Ground(0, 33f, 2f), 33f); facing = Quaternion.identity; }
            else if (name == "NecklaceWakesBethlehem") { at = Bethlehem.OnGround(60, 120); facing = Quaternion.Euler(0, 240, 0); }
            else if (name == "ViolinistTeachesBethlehem" || name == "ViolinistTeachesMedieval") { var v = Violinist.I; at = v.transform.position; facing = v.transform.rotation; }
            else if (name == "MedievalArrival") { at = new Vector3(0, Medieval.ArriveAt.y, 14f); facing = Quaternion.identity; }
            else if (name == "TheStone") { at = new Vector3(Medieval.StoneAt.x, Medieval.Ground(0, 31f, 3f), Medieval.StoneAt.z); facing = Quaternion.identity; }
            else if (name == "NecklaceWakesMedieval") { at = Medieval.OnGround(-6, 18); facing = Quaternion.Euler(0, 20, 0); }
            else if (name == "PirateArrival") { at = Pirate.ArriveAt; facing = Quaternion.identity; Pirate.SetNight(true); }
            else if (name == "ShardOverboard") { at = Pirate.ChestAt; facing = Quaternion.identity; Pirate.SetChestOpen(true); }
            else if (name == "SeaVortex") { Pirate.OpenDeepSea(true); at = Pirate.StoryAt; facing = Quaternion.Euler(0, 90, 0); }
            else if (name == "NecklaceWakesPirate") { at = Pirate.OnGround(0, -9); facing = Quaternion.Euler(0, 20, 0); }
            else if (name == "ViolinistTeachesPirate" || name == "ViolinistTeachesSteampunk") { var v = Violinist.I; at = v.transform.position; facing = v.transform.rotation; }
            else if (name == "SteampunkArrival") { Steampunk.SetPressGone(false); at = Steampunk.ArriveAt; facing = Quaternion.identity; }   // Round 11
            else if (name == "MuscleManHero") { Steampunk.SetPressGone(false); Steampunk.PrepareScene(); at = Steampunk.PressAt; facing = Quaternion.identity; }
            else if (name == "ClockShard") { at = Steampunk.ClockAt; facing = Quaternion.identity; if (Steampunk.ShardVisual) Steampunk.ShardVisual.SetActive(false); }
            else if (name == "NecklaceWakesSteampunk") { at = Steampunk.OnGround(0, 160); facing = Quaternion.identity; }
            else if (name == "TomorrowlandArrival") { at = Tomorrowland.ArriveAt; facing = Quaternion.identity; foreach (var g in Greeter.All.ToArray()) if (g) g.gameObject.SetActive(false); }   // Round 12
            else if (name == "RocketLaunch") { at = Tomorrowland.RocketAt; facing = Quaternion.identity; if (Tomorrowland.RocketModel) Tomorrowland.RocketModel.SetActive(false); if (Tomorrowland.HallDome) Tomorrowland.HallDome.SetActive(false); foreach (var g in Greeter.All.ToArray()) if (g) g.gameObject.SetActive(false); }
            else if (name == "NecklaceWakesTomorrowland") { at = Tomorrowland.OnGround(0, 172); facing = Quaternion.Euler(0, -60, 0); }
            else if (name == "ViolinistTeachesTomorrowland" || name == "ViolinistPlaysMoon") { var v = Violinist.I; at = v.transform.position; facing = v.transform.rotation; }
            else if (name == "MoonLanding" || name == "KiraFloats" || name == "WhiteShard" || name == "NecklaceWakesMoon") { Moon.FilmSpot(name, out at, out facing); }   // Round 13

            else { at = OnGround(a.spawn.x, a.spawn.z); facing = Quaternion.Euler(0, a.spawnYaw, 0); }
            Party.I.PlaceAll(at, facing); CameraRig.I.SnapBehind();
            if (name == "TheStone" && Medieval.StuckShard) Medieval.StuckShard.SetActive(false);   // Round 9: as in the game, the necklace has taken it by then
            if (!theater) yield return new WaitForSeconds(.5f);
            else { yield return null; Hud.SetFade(0f); }
            yield return Cutscenes.Play(name, at, facing);
        }
    }
}
