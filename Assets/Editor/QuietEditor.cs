using UnityEditor;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 5 (Dan's testing rule, widened: every process Code starts): a batch-mode editor (builds, imports, Tools/unity_batch.py)
    // never makes a sound: the editor's own audio is muted and the listener volume is 0 from the moment scripts load.
    [InitializeOnLoad]
    static class QuietEditor
    {
        static QuietEditor()
        {
            if (!Application.isBatchMode) return;
            EditorUtility.audioMasterMute = true;
            AudioListener.volume = 0;
        }
    }
}
