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
            if (TestArea) worldRoot = Playground.Build(World);
            else { worldRoot = IceAge.Build(World); Vortex.Build(worldRoot); }
            var a = World.areas[0];
            Party.Create(a.spawn, Quaternion.Euler(0, a.spawnYaw, 0));
            Health.Refill();
            Checkpoint.RespawnParty();
            Music.Play(TestArea ? null : "iceage");
            PlayLog.Line($"world {World.worldId} built ({World.shards.Count} shards); " + string.Join("; ", World.areas.Select(x => World.RequirementReport(x.id).detail)));
        }

        public AreaData Area => Party.I ? World.AreaAt(Party.I.Player.transform.position) : null;
        public int AreaShards(string area) { var got = SaveGame.Current.World(World.worldId).shards; return World.InArea(area).Count(s => got.Contains(s.id)); }

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
            p.rows.Add(new Menu.Row { label = "Settings", submit = () => Menu.I.Push(Menu.SettingsPage()) });
            p.rows.Add(new Menu.Row { label = "Controls", submit = () => Menu.I.Push(Menu.ControlsPage()) });
            p.rows.Add(new Menu.Row { label = "Credits", submit = () => Menu.I.Push(Menu.CreditsPage()) });
            p.rows.Add(new Menu.Row { label = "Quit", submit = Quit });
            Menu.I.Show(p, true);
            Music.Play("iceage");
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
            World = WorldData.Load(WorldData.IceAge);
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
            if (prog.storyShard && IceAge.Gate && !SaveGame.Current.portals.Contains(IceAge.Gate.id)) SaveGame.Current.portals.Add(IceAge.Gate.id);
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
            Hud.Message($"Find the shards! {a.requirement} of them wake Dan's necklace.", 6f);
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
            World = WorldData.Load(WorldData.IceAge);
            BuildWorld();
            PlayLog.Line("left the test area");
            if (toTitle) { Menu.I.Close(); ToTitle(); }
        }

        public void Pause()
        {
            if (State != Mode.Playing || InCutscene) return;
            State = Mode.Paused; Time.timeScale = 0f; Necklace.StopRumble();
            var p = new Menu.Page { title = "Paused", back = Resume };
            p.rows.Add(new Menu.Row { label = "Resume", submit = Resume });
            p.rows.Add(new Menu.Row { label = "Settings", submit = () => Menu.I.Push(Menu.SettingsPage()) });
            p.rows.Add(new Menu.Row { label = "Controls", submit = () => Menu.I.Push(Menu.ControlsPage()) });
            if (TestArea) p.rows.Add(new Menu.Row { label = "Leave the test area", submit = () => { Menu.I.Close(); ToTitle(); } });
            else p.rows.Add(new Menu.Row { label = "Quit to title", submit = () => { SaveGame.Save("quit to title"); Menu.I.Close(); ToTitle(); } });
            p.rows.Add(new Menu.Row { label = "Quit game", submit = Quit });
            Menu.I.Show(p, false);
            PlayLog.Line("paused");
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
                else
                {
                    // the valley from above the landing spot, toward the volcanoes, swaying slowly
                    float s = Mathf.Sin(titleSpin * .04f);
                    titleCam.transform.position = new Vector3(18f + s * 10f, 16f, -14f);
                    titleCam.transform.rotation = Quaternion.LookRotation(new Vector3(-10f + s * 25f, 40f, 400f) - titleCam.transform.position);
                }
            }
            if (Party.I && (State == Mode.Playing))
            {
                var area = Area; var prog = SaveGame.Current.World(World.worldId);
                Necklace.Tick(area != null ? area.id : "", area != null && area.requirement > 0 && prog.necklaceAwake && !prog.storyShard);
                if (area != null) { ApplyKiraForm(area); ApplyOutfits(area); }
                bool dark = DarkVolume.At(Party.I.Player.Motor.Center) && !InCutscene;
                Look.InVortex(Vortex.Inside(Party.I.Player.transform.position) && !InCutscene);
                Look.SetDark(dark);
                foreach (var h in Party.I.Heroes) NecklaceModel.Light(h.NecklaceGem, dark);
            }
        }

        // ---- progression
        public void OnCollected(Pickup p)
        {
            var prog = SaveGame.Current.World(World.worldId);
            if (p.kind == Pickup.Kind.StoryShard) foreach (var h in Party.I.Heroes) h.Cheer();   // Round 3: everyone cheers the story shard
            else if (p.kind == Pickup.Kind.Shard) Party.I.Player.Cheer();
            if (p.kind == Pickup.Kind.Shard)
            {
                var area = World.Area(p.area);
                if (area != null && area.requirement > 0 && !prog.necklaceAwake && AreaShards(area.id) >= area.requirement) StartCoroutine(WakeNecklace(area));
            }
            else if (p.kind == Pickup.Kind.StoryShard) StartCoroutine(TestArea ? OpenPortal() : OpenVortex());
        }

        IEnumerator WakeNecklace(AreaData area)
        {
            Busy = true;
            var prog = SaveGame.Current.World(World.worldId); prog.necklaceAwake = true;
            PlayLog.Line($"area {area.id}: {AreaShards(area.id)} of {area.requirement} shards, the necklace wakes");
            yield return new WaitForSeconds(.4f);
            // not mid-air (a vent, a jump) and not in a cave (its cameras need the open sky): it waits for the party to come out
            while ((!Party.I.Player.Motor.Grounded && Party.I.Player.Motor.Mode != MoveMode.Swim) || DarkVolume.At(Party.I.Player.Motor.Center) || !Game.Playing) yield return null;
            var player = Party.I.Player.transform;
            var to = StoryShard.transform.position - player.position; to.y = 0;
            var at = player.position;
            if (Physics.Raycast(at + Vector3.up * .5f, Vector3.down, out var ground, 30f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << 4) | (1 << 2)), QueryTriggerInteraction.Ignore)) at = ground.point;   // on the ground, even mid-jump
            Sfx.Play("necklace", .7f);
            yield return Cutscenes.Play("NecklaceWakes", at, Quaternion.LookRotation(to.sqrMagnitude > .01f ? to : player.forward));
            StoryShard.gameObject.SetActive(true); StoryBeam.SetActive(true);
            Hud.Message("The necklace is awake! Follow it to the story shard.", 5f);
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
            var g = IceAge.Gate;
            yield return Cutscenes.Play("VortexOpens", g.transform.position, g.transform.rotation);
            if (!SaveGame.Current.portals.Contains(g.id)) SaveGame.Current.portals.Add(g.id);
            Hud.Message($"The blue story shard opened a vortex! Step in to travel. ({SaveGame.Current.StoryShards} of {Vortex.StoryShardsTotal} story shards)", 6f);
            SaveGame.Save("story shard");
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
            if (door.Blocked)
            {
                int missing = Vortex.StoryShardsTotal - SaveGame.Current.StoryShards;
                Hud.Message($"The way home is closed. {missing} story shard{(missing == 1 ? " is" : "s are")} still missing: rebuild them all to open it.", 5f);
                Sfx.Play("hurt", .3f, .6f);
                var back = (Party.I.Player.transform.position - door.transform.position); back.y = 0;
                Party.I.Player.Motor.Knockback(back.normalized * 6f + Vector3.up * 4f);
                return;
            }
            if (path.id == "IceAge")
            {
                SaveGame.Current.checkpoint = "IA-vortex"; Checkpoint.RespawnParty();
                Sfx.Play("portal", .6f); SaveGame.Save("vortex to the Ice Age"); OnAreaChanged();
                return;
            }
            StartCoroutine(ToBeContinued(path));
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
        public IEnumerator Film(string name)
        {
            SaveGame.Disabled = true; Cutscenes.AllowSkip = false;
            Menu.I.Close(); titleCam.Priority = -10; State = Mode.Playing;
            var a = World.areas[0];
            Vector3 at; Quaternion facing;
            if (name == "ActOne") { at = HomeAnchor; facing = Quaternion.identity; }
            else if (name == "VortexOpens") { at = IceAge.Gate.transform.position; facing = IceAge.Gate.transform.rotation; }
            else if (name == "ViolinistTeaches") { var v = Violinist.I; at = v.transform.position; facing = Quaternion.Euler(0, 135f, 0); }
            else if (name == "NecklaceWakes") { at = IceAge.OnGround(10f, 300f); var to = StoryShard.transform.position - at; to.y = 0; facing = Quaternion.LookRotation(to); }
            else { at = IceAge.OnGround(a.spawn.x, a.spawn.z); facing = Quaternion.Euler(0, a.spawnYaw, 0); }
            Party.I.PlaceAll(at, facing); CameraRig.I.SnapBehind();
            yield return new WaitForSeconds(.5f);
            yield return Cutscenes.Play(name, at, facing);
        }
    }
}
