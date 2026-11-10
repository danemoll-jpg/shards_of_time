namespace ShardsOfTime
{
    // Round 4: a full-screen tint a cutscene keys (the white-out into the vortex and out of it): the amount is this object's local
    // scale x (0..1), so it is an ordinary Timeline key like any other. (Round 5: its own file, or the built game can't find it.)
    public sealed class ScreenTint : UnityEngine.MonoBehaviour
    {
        public UnityEngine.Color color = UnityEngine.Color.white;
        void LateUpdate() => Hud.Tint(color, transform.localScale.x);
        void OnDisable() => Hud.Tint(color, 0);
    }
}
