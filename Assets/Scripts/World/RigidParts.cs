using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 12 (its own file: cutscene prefabs carry it)
    // ---- rigid parts of a static model, animated: a propeller or a brush spins, a wheel rolls, arms wave, eye stalks and lids wobble
    public sealed class RigidParts : MonoBehaviour
    {
        public float wave = 0f, roll = 0f, wobble = 1f, propeller = 720f;   // wave: arms (0 = still; >0 = waving), roll: wheel speed (deg/s)
        readonly Dictionary<string, (Transform t, Quaternion q)> parts = new Dictionary<string, (Transform, Quaternion)>();
        float seed;
        void Start()
        {
            seed = Random.value * 10f;
            foreach (var t in GetComponentsInChildren<Transform>()) foreach (var n in new[] { "Propeller", "ArmL", "ArmR", "Wheel", "Top", "Eyes" }) if (t.name.StartsWith(n) && !parts.ContainsKey(n)) parts[n] = (t, t.localRotation);
        }
        void Update()
        {
            float tt = Time.time + seed;
            if (parts.TryGetValue("Propeller", out var p)) p.t.localRotation = p.q * Quaternion.Euler(0, 0, tt * propeller);
            if (parts.TryGetValue("Wheel", out var w)) w.t.localRotation = w.q * Quaternion.Euler(tt * roll, 0, 0);
            if (parts.TryGetValue("ArmL", out var l)) l.t.localRotation = l.q * Quaternion.Euler(0, Mathf.Sin(tt * 7f) * 25f * wave, 10f + Mathf.Sin(tt * 2.1f) * 8f + wave * 40f);
            if (parts.TryGetValue("ArmR", out var r)) r.t.localRotation = r.q * Quaternion.Euler(0, 0, -10f - Mathf.Sin(tt * 2.3f) * 8f);
            if (parts.TryGetValue("Top", out var tp)) tp.t.localRotation = tp.q * Quaternion.Euler(Mathf.Sin(tt * 5f) * 4f * wobble, 0, Mathf.Sin(tt * 3.3f) * 5f * wobble);
            if (parts.TryGetValue("Eyes", out var e)) e.t.localRotation = e.q * Quaternion.Euler(Mathf.Sin(tt * 4.2f) * 10f * wobble, 0, Mathf.Sin(tt * 3.1f) * 12f * wobble);
        }
    }

}
