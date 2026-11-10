using UnityEngine;

namespace ShardsOfTime
{
    // Round 4: a cutscene's own light (on its prefab's root): the living room's warm interior, no fog, no snow falling indoors.
    public sealed class CutsceneMood : MonoBehaviour
    {
        public float sunIntensity = 0f;
        public Color ambientSky = new Color(.5f, .35f, .45f), ambientEquator = new Color(.45f, .3f, .35f), ambientGround = new Color(.25f, .16f, .14f);
        public Color background = new Color(.12f, .1f, .2f);
        public bool fog, snow;
    }
}
