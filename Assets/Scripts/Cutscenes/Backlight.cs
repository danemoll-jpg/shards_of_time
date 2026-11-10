namespace ShardsOfTime
{
    // Round 5: the arrival's silhouette shot: the sun behind the subject, the sky bright, the light on the near side down. The amount is
    // this object's local scale x (0..1), keyed like ScreenTint.
    public sealed class Backlight : UnityEngine.MonoBehaviour
    {
        float last = -1;
        void LateUpdate() { float a = transform.localScale.x; if (!UnityEngine.Mathf.Approximately(a, last)) { last = a; Look.Backlit(a); } }
        void OnDisable() { last = -1; Look.Backlit(0); }
    }
}
