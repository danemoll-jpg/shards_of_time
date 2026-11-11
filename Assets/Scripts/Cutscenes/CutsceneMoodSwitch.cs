namespace ShardsOfTime
{
    // Round 9: Act I goes from the living room's light to the Ice Age sky in the middle of the cutscene. While this object's local scale x
    // is over 0.5 the world's own look is used, otherwise the cutscene's CutsceneMood (keyed like ScreenTint).
    public sealed class CutsceneMoodSwitch : UnityEngine.MonoBehaviour
    {
        public CutsceneMood mood;
        int last = -1;
        void LateUpdate()
        {
            int s = transform.localScale.x > .5f ? 1 : 0;
            if (s != last) { last = s; Look.Mood(s == 1 ? null : mood); }
        }
    }
}
