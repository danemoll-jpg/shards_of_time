using UnityEngine;

namespace ShardsOfTime
{
    // One hero (Dan, Abi, Kira): stats, motor, body, attack. Controlled by the Party: as the player (input) or as a follower.
    // Body: the graybox shapes ("Graybox") and, when the hero has a real model, the model ("Model ...": Round 2, Kira's forms; Round 3,
    // Dan's and Abi's outfits); the graybox stays as the fallback when a model is missing.
    public sealed class Hero : MonoBehaviour
    {
        public const int PlayerLayer = 8, FollowerLayer = 9, EnemyLayer = 10;
        public CharacterStats Stats { get; private set; }
        public CharacterMotor Motor { get; private set; }
        public Follower Follower { get; private set; }
        public Transform Body { get; private set; }
        public Transform NecklaceGem { get; private set; }
        public Transform Graybox { get; private set; }
        public GameObject Model { get; private set; }
        public CreatureAnimator Anim { get; private set; }
        public KiraForm Form { get; private set; }          // Kira only (Round 2, item 7)
        public string Outfit { get; private set; }          // Dan and Abi (Round 3): "home", "iceage" -> model <name>_<outfit>
        public bool IsPerson => Stats.displayName != "Kira";
        public bool IsPlayer { get; private set; }
        public int Attacks { get; private set; }
        float attackTimer; Transform swipe; float swipeTimer;
        float squash, swimLift, pushBack;

        public const float HardLanding = 14f;   // m/s: a fall of about 3.3 m or more lands hard (Round 3)

        public static Hero Create(CharacterStats stats, Vector3 pos)
        {
            var go = new GameObject(stats.displayName);
            go.transform.position = pos;
            var cc = go.AddComponent<CharacterController>();
            var h = go.AddComponent<Hero>();
            h.Stats = stats;
            h.Motor = go.AddComponent<CharacterMotor>(); h.Motor.Configure(stats);
            h.Follower = go.AddComponent<Follower>(); h.Follower.Hero = h;
            h.Body = Placeholders.HeroBody(stats, go.transform);
            h.Graybox = new GameObject("Graybox").transform; h.Graybox.SetParent(h.Body, false);
            for (int i = h.Body.childCount - 1; i >= 0; i--) { var c = h.Body.GetChild(i); if (c != h.Graybox) c.SetParent(h.Graybox, false); }
            if (stats.displayName == "Dan")
            {
                h.NecklaceGem = NecklaceModel.Attach(h.Body, stats);
                var cube = h.Graybox.Find("Necklace");
                if (h.NecklaceGem) { if (cube) Object.Destroy(cube.gameObject); }
                else h.NecklaceGem = cube;   // no necklace model: the Round 1 gem
            }
            if (stats.displayName == "Kira") h.SetForm(KiraForm.Load(KiraForm.Home));
            else h.SetOutfit(Outfits.Home);
            Health.Hurt += h.OnHurt;
            h.Motor.Event += h.OnMotorEvent;
            h.swipe = Placeholders.Swipe(go.transform, stats);
            _ = cc;
            return h;
        }

        void OnDestroy() => Health.Hurt -= OnHurt;
        void OnHurt() { if (IsPlayer && Anim) Anim.Trigger("hurt"); }

        public bool HasAbility(string id) => (Form && Form.Has(id)) || ((Stats.lockedAbility == id || Abilities.Learns(Stats.displayName, id)) && SaveGame.Current.Has(id));   // a form's, or this hero's learned move (Round 5; Round 8: Dan learns two)
        public bool IsMuscleMan => Stats.displayName == "MuscleMan";   // Round 8: the companion
        public void Cheer() { if (Anim) Anim.Trigger(IsMuscleMan && Anim.Has("mm_cheer") ? "mm_cheer" : "cheer"); }

        // Dan's / Abi's outfit (Round 3): swaps the model (Resources/Characters/<name>_<outfit>.fbx), the same way Kira's form does; Dan's
        // necklace moves onto the new model's chest. Missing model: the graybox capsule (and its necklace) stays.
        public void SetOutfit(string outfit)
        {
            if (!IsPerson || string.IsNullOrEmpty(outfit) || outfit == Outfit) return;
            string model = Stats.displayName.ToLowerInvariant() + "_" + outfit;
            // Round 7: Bethlehem's clothes until their models are in: Egypt's (simple robes), not the home pyjamas
            if (!CharacterModel.Exists(model) && outfit == Outfits.Bethlehem) model = Stats.displayName.ToLowerInvariant() + "_" + Outfits.Egypt;
            if (!CharacterModel.Exists(model) && outfit == Outfits.Pirate) model = Stats.displayName.ToLowerInvariant() + "_" + Outfits.Medieval;   // Round 10: until the pirate models are in
            if (!CharacterModel.Exists(model) && outfit == Outfits.Steampunk) model = Stats.displayName.ToLowerInvariant() + "_" + Outfits.Pirate;   // Round 11: until the steampunk models are in
            if (!CharacterModel.Exists(model) && outfit == Outfits.Steampunk) model = Stats.displayName.ToLowerInvariant() + "_" + Outfits.Medieval;
            // Rounds 12-13: until Dan's Mixamo rigs are in, Tomorrowland and the Moon wear the steampunk clothes (then pirate)
            if (!CharacterModel.Exists(model) && (outfit == Outfits.Tomorrowland || outfit == Outfits.Moon)) model = Stats.displayName.ToLowerInvariant() + "_" + Outfits.Steampunk;
            if (!CharacterModel.Exists(model) && (outfit == Outfits.Tomorrowland || outfit == Outfits.Moon)) model = Stats.displayName.ToLowerInvariant() + "_" + Outfits.Pirate;
            if (!CharacterModel.Exists(model) && IsMuscleMan) model = "muscleman_medieval";   // Round 8: Muscle Man has one outfit, every world
            if (Model && Model.name == "Model " + model) { Outfit = outfit; return; }
            if (!CharacterModel.Exists(model)) { if (Outfit == null) PlayLog.Line($"{Stats.displayName}: no model {model}, graybox"); Outfit = outfit; return; }
            Outfit = outfit;
            if (Model) { Model.SetActive(false); Destroy(Model); Model = null; Anim = null; }
            Model = CharacterModel.Build(model, Body, Motor);
            if (outfit == Outfits.Moon) Helmet.Attach(Model, Helmet.KindFor(model));   // Round 13: the bubble helmet
            Anim = Model.GetComponent<CreatureAnimator>();
            Anim.AttackClip = Stats.attackName == "Kick" ? "kick" : "punch";
            Graybox.gameObject.SetActive(false);
            if (Stats.displayName == "Dan")
            {
                NecklaceModel.Remove(Body);
                NecklaceGem = NecklaceModel.Attach(Body, Stats, Model);
                PlayLog.Line(NecklaceModel.LastFit);
            }
            PlayLog.Line($"{Stats.displayName}'s outfit: {outfit} (model {model}, {Anim.ClipCount} clips, hips {Anim.HipsHeight:0.00} m, humanoid {Anim.Humanoid})");
        }

        // Kira's form (Round 2, item 7): swaps her model and her abilities. Same form again: nothing happens.
        public void SetForm(KiraForm f)
        {
            if (!f || (Form && Form.id == f.id)) return;
            Form = f;
            if (Model) { Destroy(Model); Model = null; Anim = null; }
            Model = CharacterModel.Build(CharacterModel.Exists(f.model) ? f.model : "kira", Body, Motor);   // Round 11: a form whose model isn't in yet: home Kira
            if (f.id == KiraForm.Space) Helmet.Attach(Model, "kira");   // Round 13: her silver-rimmed bubble
            Anim = Model ? Model.GetComponent<CreatureAnimator>() : null;
            Graybox.gameObject.SetActive(!Model);
            if (Motor) Motor.CanGlide = HasAbility(Abilities.Glide);   // Round 8: dragon Kira's wings
            if (Motor) { Motor.Wheels = HasAbility(Abilities.WheelDash); Motor.CanFloat = HasAbility(Abilities.Float); }   // Rounds 12-13: robot Kira's wheels, space Kira's float
            if (Motor) { Motor.CanBellySlide = HasAbility(Abilities.BellySlide) || HasAbility(Abilities.WheelDash); Motor.Otter = HasAbility(Abilities.OtterSwim); Motor.CanDeepDive = SaveGame.Current.Has(Abilities.DeepDive) || Motor.Otter; if (Motor.CanBellySlide) Motor.CanPounce = false; else if (Stats.displayName == "Kira") Motor.CanPounce = SaveGame.Current.Has(Abilities.Pounce); }   // Round 10: otter Kira
            PlayLog.Line($"Kira's form: {f.displayName} ({(Model ? "model " + f.model : "graybox: no model")}, abilities [{string.Join(", ", f.abilities)}])");
        }

        public void SetPlayer(bool player)
        {
            IsPlayer = player;
            gameObject.layer = player ? PlayerLayer : FollowerLayer;
            gameObject.tag = player ? "Player" : "Untagged";
            Follower.enabled = !player;
        }

        void OnMotorEvent(string e)
        {
            if (e == "land") { squash = .18f; if (Anim && Motor.LandSpeed > 4f) Anim.Trigger(Motor.LandSpeed > HardLanding ? "land_hard" : "land"); }
            if (e == "jump" || e == "leap") { squash = -.12f; if (IsPlayer) Sfx.Play("jump", .5f); if (Anim) Anim.Trigger("jump"); }
            if (e == "glide" && IsPlayer) Sfx.Play("wings_flap", .45f);
            if (e == "doublejump") { squash = -.18f; if (IsPlayer) Sfx.Play("jump", .6f, 1.3f); Placeholders.Puff(transform.position, Stats.color); }
            if (e == "splash" && IsPlayer) Sfx.Play("splash", .5f);
            if (e == "airdash") { squash = -.15f; if (IsPlayer) Sfx.Play("air_dash", .6f); Placeholders.Puff(transform.position + Vector3.up * .8f, new Color(.9f, .95f, 1f)); if (Anim) Anim.Trigger("jump"); }   // Round 12
            if (e == "slide" && Motor.Wheels && IsPlayer) Sfx.Play("wheel_whirr", .6f);   // Round 12: robot Kira's wheel dash
            if (e == "boost" && IsPlayer) Sfx.Play("boost_ramp", .7f);
            if (e == "float" && IsPlayer) Sfx.Play("kira_float", .5f);   // Round 13: space Kira floats
            if (e == "outofbreath" && IsPlayer) Health.Damage(1, Vector3.zero, "out of breath");
            if (e == "pound") Pound();
            if (e == "aim") { squash = .22f; if (Anim) Anim.Trigger("land"); }
            if (e == "pounce") { squash = -.2f; if (IsPlayer) Sfx.Play("jump", .6f, 1.5f); if (Anim) Anim.Trigger("jump"); PlayLog.Line($"{Stats.displayName}: pounce from {transform.position}"); }
            // Round 7, item 6: lamb Kira lands on hay or wool: a high springy bounce (anyone else just lands)
            if (e == "land" && Motor.GroundCollider && Motor.GroundCollider.GetComponent<MoonBounce>() is MoonBounce mb) mb.Bounce(this);   // Round 13: a bouncy crater rim
            if (e == "land" && HasAbility(Abilities.WoolBounce) && Motor.GroundCollider && Motor.GroundCollider.GetComponentInParent<BouncePad>() is BouncePad pad && !Motor.Aiming)
                pad.Bounce(this);
        }

        // Round 4: the ground pound lands: a shockwave stuns (and hits) enemies round Dan, and cracked ice under him breaks
        public const float PoundRadius = 3.2f;
        void Pound()
        {
            squash = .3f;
            if (IsPlayer) Sfx.Play("pound", .7f);
            Placeholders.Puff(transform.position + Vector3.up * .1f, new Color(.9f, .95f, 1f));
            foreach (var col in Physics.OverlapSphere(transform.position + Vector3.up * .5f, PoundRadius, ~0, QueryTriggerInteraction.Collide))
            {
                var e = col.GetComponentInParent<Enemy>();
                if (e) { e.Stun(2.5f); var away = e.transform.position - transform.position; e.Hit(1, away, "Ground pound"); }
            }
            var crack = Motor.GroundCollider ? Motor.GroundCollider.GetComponentInParent<CrackedFloor>() : null;
            if (crack) crack.Pound(this);
            PlayLog.Line($"{Stats.displayName}: ground pound at {transform.position}{(crack ? ", cracked ice " + crack.id : "")}");
        }

        // Round 8, item 7: Dan's charged punch (Party: hold the attack, release): a big lunge-punch that breaks cracked walls (every world)
        // and knocks knights' shields away; it hits harder and reaches further. The fist glows while it's charged.
        public int BigPunches { get; private set; }
        Transform glow;
        public void ChargeGlow(bool on)
        {
            if (on && !glow)
            {
                var anim = Model ? Model.GetComponentInChildren<Animator>() : null;
                var hand = anim && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
                glow = Placeholders.Prim(PrimitiveType.Sphere, hand ? hand : transform, hand ? Vector3.zero : new Vector3(.3f, 1.1f, .4f), Vector3.one * (hand ? .22f : .3f) / (hand ? Mathf.Max(.01f, hand.lossyScale.x) : 1f), Placeholders.Mat(new Color(1f, .75f, .3f), "Glow"), name: "Charge glow").transform;
            }
            if (glow) glow.gameObject.SetActive(on);
        }
        public void ChargedPunch()
        {
            BigPunches++; attackTimer = Stats.attackCooldown;
            if (Anim) Anim.Trigger("attack");
            Sfx.Play("punch_big", .8f);
            Motor.Knockback(transform.forward * 7f + Vector3.up * 1.5f);   // a short lunge forward
            var c = transform.position + Vector3.up * Stats.height * .5f + transform.forward * (Stats.attackRange + .5f);
            Placeholders.Puff(c, new Color(1f, .8f, .4f));
            var seen = new System.Collections.Generic.HashSet<Object>();
            foreach (var col in Physics.OverlapSphere(c, Stats.attackRadius + .7f, ~0, QueryTriggerInteraction.Collide))
            {
                var e = col.GetComponentInParent<Enemy>();
                if (e && seen.Add(e)) e.HitCharged(transform.forward, this);
                var rock = col.GetComponentInParent<RockWall>();
                if (rock && seen.Add(rock)) rock.Punch(this, true);
                var tgt = col.GetComponentInParent<PunchTarget>();
                if (tgt && seen.Add(tgt)) tgt.Punched(this, true);
            }
            PlayLog.Line($"{Stats.displayName}: charged punch at {transform.position}");
        }

        // Round 8, item 6: dragon Kira's fire puff (her attack while a dragon): a little flame forward that lights / burns what it reaches
        // (FireTarget: torches, braziers, ropes, hay, brambles) and scares rats; it never hurts people (knights, archers, the party)
        public int FirePuffs { get; private set; }
        void FirePuff()
        {
            FirePuffs++;
            Sfx.Play("fire_puff", .6f, Random.Range(.95f, 1.1f));
            var from = transform.position + Vector3.up * Stats.height * .55f + transform.forward * .4f;
            for (int i = 0; i < 4; i++) Placeholders.Puff(from + transform.forward * (.5f + i * .7f), Color.Lerp(new Color(1f, .85f, .3f), new Color(1f, .35f, .1f), i / 3f));
            foreach (var col in Physics.OverlapCapsule(from, from + transform.forward * 3.2f, .9f, ~0, QueryTriggerInteraction.Collide))
            {
                var f = col.GetComponentInParent<FireTarget>(); if (f) f.Burn(this);
                var e = col.GetComponentInParent<Enemy>(); if (e) e.Scare(transform.position);
            }
        }

        // Each hero's basic attack: a short sweep in front that hits enemies (Punch, Kick, Claws).
        public bool TryAttack()
        {
            if (attackTimer > 0) return false;
            attackTimer = Stats.attackCooldown; Attacks++;
            swipeTimer = .15f; swipe.gameObject.SetActive(true);
            if (Anim) Anim.Trigger("attack");
            if (HasAbility(Abilities.FirePuff)) { FirePuff(); return true; }   // Round 8: dragon Kira breathes a puff instead of clawing
            Sfx.Play("attack", .4f, Stats.displayName == "Kira" ? 1.4f : Stats.displayName == "Abi" ? 1.15f : .9f);
            var c = transform.position + Vector3.up * Stats.height * .5f + transform.forward * Stats.attackRange;
            foreach (var col in Physics.OverlapSphere(c, Stats.attackRadius, ~0, QueryTriggerInteraction.Collide))
            {
                var e = col.GetComponentInParent<Enemy>();
                if (e) e.Hit(Stats.attackDamage, transform.forward, Stats.attackName);
                var ice = col.GetComponentInParent<IceWall>();
                if (ice) { PlayLog.Line($"{Stats.displayName}'s {Stats.attackName} reaches ice wall {ice.id}"); ice.Claw(this); }
                var rock = col.GetComponentInParent<RockWall>();
                if (rock) rock.Punch(this, false);   // Round 5: only the charged punch breaks it (Round 8)
                var tgt = col.GetComponentInParent<PunchTarget>();
                if (tgt) tgt.Punched(this, false);
            }
            return true;
        }

        float stepTimer;
        void Update()
        {
            attackTimer -= Time.deltaTime;
            // Round 4: footsteps in the snow (the player only)
            if (IsPlayer && Motor.Grounded && Game.Playing && !Game.TestArea)
            {
                var hv = Motor.Velocity; hv.y = 0;
                if (hv.magnitude > 1.5f && (stepTimer -= Time.deltaTime) <= 0)
                {
                    stepTimer = Mathf.Lerp(.42f, .27f, hv.magnitude / Stats.runSpeed);
                    Sfx.Play(Game.InPirate ? (transform.position.y > 1.5f ? "footstep_wood" : "footstep_sand") : Game.InMedieval ? "footstep_cobble" : Game.InEgypt ? "footstep_sand" : Game.InBethlehem ? "footstep_grass" : "footstep_snow", IsPerson ? .3f : .15f, Random.Range(.9f, 1.1f) * (IsPerson ? 1f : 1.5f));
                }
            }
            if (swipeTimer > 0) { swipeTimer -= Time.deltaTime; if (swipeTimer <= 0) swipe.gameObject.SetActive(false); }
            // a little squash and stretch on jumps and landings (placeholder life)
            squash = Mathf.MoveTowards(squash, 0, Time.deltaTime * 1.2f);
            // a follower right in front of the camera (walls pulled it in) hides, so it never blocks the view of the player
            if (!IsPlayer && CameraRig.I && !Game.InCutscene)
                Body.gameObject.SetActive(Vector3.Distance(CameraRig.I.Cam.transform.position, Motor.Center) > Stats.height * .5f + 1.2f);
            else if (!Body.gameObject.activeSelf && !Game.InCutscene) Body.gameObject.SetActive(true);
            float s = Model ? 1f : 1f - squash;   // a real model has its own jump / land clips
            // swimming at the surface: a person's swim clip lies flat at hip height, so the model rises to put the back at the waterline
            if (Model && Anim && Anim.Humanoid)
            {
                bool surface = Motor.Mode == MoveMode.Swim && Motor.Water && transform.position.y > Motor.Water.Top - Stats.height * .75f - .3f && Anim.State == "swim";
                swimLift = Mathf.MoveTowards(swimLift, surface ? Stats.height * .28f : 0f, Time.deltaTime * 2f);
                // the push clip leans into the block: the model steps back so head and hands stay on its face, not in it
                pushBack = Mathf.MoveTowards(pushBack, Anim.State == "push" ? .25f : 0f, Time.deltaTime * 1.5f);
                Model.transform.localPosition = new Vector3(0, swimLift - Motor.Cc.skinWidth, -pushBack);
            }
            Body.localScale = new Vector3(1f / Mathf.Sqrt(Mathf.Max(.5f, s)), s, 1f / Mathf.Sqrt(Mathf.Max(.5f, s)));
        }
    }

    // Dan's and Abi's outfits (Round 3): AreaData.outfit names one per area
    public static class Outfits { public const string Home = "home", IceAge = "iceage", Egypt = "egypt", Bethlehem = "bethlehem", Medieval = "medieval", Pirate = "pirate", Steampunk = "steampunk", Tomorrowland = "tomorrowland", Moon = "moon"; }
}
