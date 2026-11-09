using System.Collections;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

namespace ShardsOfTime
{
    // The game's one object (Assets/Scenes/Main.unity holds only this; everything else is built in code at start, as in Hostile
    // Horizon). Title screen -> play -> pause; the world (graybox playground) and the party are rebuilt from the save on Continue /
    // New game. Area A's progression: the small-shard requirement wakes the necklace (cutscene NecklaceWakes), the story shard opens the
    // portal (cutscene PortalOpens).
    public sealed class Game : MonoBehaviour
    {
        public enum Mode { Title, Playing, Paused }
        public static Game I { get; private set; }
        public static WorldData World { get; private set; }
        public Mode State { get; private set; } = Mode.Title;
        public static bool Playing => I && I.State == Mode.Playing && Cutscenes.Playing == null;
        public static bool InCutscene => Cutscenes.Playing != null;

        public Pickup StoryShard; public GameObject StoryBeam; public Portal PortalA;
        Transform worldRoot; CinemachineCamera titleCam; float titleSpin;
        public bool Busy { get; private set; }          // a story moment (cutscene + its follow-up) is running

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
            Sfx.Init(gameObject);
            Hud.Create(); Menu.Create();
            World = WorldData.Load("Playground");
            var tc = new GameObject("Title Camera"); titleCam = tc.AddComponent<CinemachineCamera>(); titleCam.Priority = 30;
            titleCam.Lens.FieldOfView = 50f; titleCam.Lens.FarClipPlane = 600f;
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
            worldRoot = Playground.Build(World);
            var a = World.Area("A");
            Party.Create(a.spawn, Quaternion.Euler(0, a.spawnYaw, 0));
            Health.Refill();
            Checkpoint.RespawnParty();
        }

        public AreaData Area => World.Area(Party.I && Party.I.Player.transform.position.x > 200f ? "B" : "A");
        public int AreaShards(string area) { var got = SaveGame.Current.World(World.worldId).shards; return World.InArea(area).Count(s => got.Contains(s.id)); }

        // ---- title, play, pause
        public void ToTitle()
        {
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
            if (newGame) SaveGame.NewGame(); else SaveGame.Load();
            BuildWorld();
            Menu.I.Close();
            titleCam.Priority = -10;
            State = Mode.Playing; Time.timeScale = 1f;
            if (newGame)
            {
                SaveGame.Current.checkpoint = "A-start"; SaveGame.Save("new game");
                Hud.Message($"Find the shards! {World.Area("A").requirement} of them wake Dan's necklace.", 5f);
            }
            PlayLog.Line(newGame ? "new game" : $"continue: {SaveGame.Current.Total} shards, checkpoint {SaveGame.Current.checkpoint}");
            // a story moment the save stopped in the middle of
            var prog = SaveGame.Current.World(World.worldId);
            if (prog.storyShard && !SaveGame.Current.portals.Contains(PortalA.id)) SaveGame.Current.portals.Add(PortalA.id);
        }

        public void Pause()
        {
            if (State != Mode.Playing || InCutscene) return;
            State = Mode.Paused; Time.timeScale = 0f; Necklace.StopRumble();
            var p = new Menu.Page { title = "Paused", back = Resume };
            p.rows.Add(new Menu.Row { label = "Resume", submit = Resume });
            p.rows.Add(new Menu.Row { label = "Settings", submit = () => Menu.I.Push(Menu.SettingsPage()) });
            p.rows.Add(new Menu.Row { label = "Controls", submit = () => Menu.I.Push(Menu.ControlsPage()) });
            p.rows.Add(new Menu.Row { label = "Quit to title", submit = () => { SaveGame.Save("quit to title"); Menu.I.Close(); ToTitle(); } });
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
                var c = new Vector3(0, 2f, -28f);
                titleCam.transform.position = c + Quaternion.Euler(0, 200f + Mathf.Sin(titleSpin * .05f) * 25f, 0) * new Vector3(0, 9f, 26f);
                titleCam.transform.rotation = Quaternion.LookRotation(c - titleCam.transform.position);
            }
            if (Party.I && (State == Mode.Playing))
            {
                var area = Area; var prog = SaveGame.Current.World(World.worldId);
                Necklace.Tick(area.id, area.requirement > 0 && prog.necklaceAwake && !prog.storyShard);
            }
        }

        // ---- progression
        public void OnCollected(Pickup p)
        {
            var prog = SaveGame.Current.World(World.worldId);
            if (p.kind == Pickup.Kind.Shard)
            {
                var area = World.Area(p.area);
                if (area != null && area.requirement > 0 && !prog.necklaceAwake && AreaShards(area.id) >= area.requirement) StartCoroutine(WakeNecklace(area));
            }
            else if (p.kind == Pickup.Kind.StoryShard) StartCoroutine(OpenPortal());
        }

        IEnumerator WakeNecklace(AreaData area)
        {
            Busy = true;
            var prog = SaveGame.Current.World(World.worldId); prog.necklaceAwake = true;
            PlayLog.Line($"area {area.id}: {AreaShards(area.id)} of {area.requirement} shards, the necklace wakes");
            yield return new WaitForSeconds(.4f);
            var player = Party.I.Player.transform;
            var to = area.storyShard - player.position; to.y = 0;
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

        public void OnAreaChanged() { var a = Area; Hud.Message(a.name, 2.5f); PlayLog.Line("area " + a.id); }

        // ---- the editor's film render (Editor/CutsceneRender.cs): play one cutscene in the playground without touching the save
        public IEnumerator Film(string name)
        {
            SaveGame.Disabled = true; Cutscenes.AllowSkip = false;
            Menu.I.Close(); titleCam.Priority = -10; State = Mode.Playing;
            var a = World.Area("A");
            Vector3 at; Quaternion facing;
            if (name == "PortalOpens") { at = PortalA.transform.position; facing = PortalA.transform.rotation; }
            else { at = new Vector3(10f, 0, 8f); var to = a.storyShard - at; to.y = 0; facing = Quaternion.LookRotation(to); }
            Party.I.PlaceAll(at, facing); CameraRig.I.SnapBehind();
            yield return new WaitForSeconds(.5f);
            yield return Cutscenes.Play(name, at, facing);
        }
    }
}
