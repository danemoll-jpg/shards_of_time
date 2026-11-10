namespace ShardsOfTime
{
    // Round 5: a cutscene's sound effects (its Timeline audio track) at the effects volume
    public sealed class CutsceneAudio : UnityEngine.MonoBehaviour
    {
        void OnEnable() { var s = GetComponent<UnityEngine.AudioSource>(); if (s) s.volume = GameSettings.S.effectsVolume / 100f; }
    }
}
