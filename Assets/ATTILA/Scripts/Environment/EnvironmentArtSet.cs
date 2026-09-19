using UnityEngine;

namespace ATTILA.Environment
{
    // Visual references only. Source models remain in ThirdParty; these are ATTILA wrappers.
    [CreateAssetMenu(menuName = "ATTILA/Environment Art Set")]
    public sealed class EnvironmentArtSet : ScriptableObject
    {
        public GameObject tree;
        public GameObject smallTree;
        public GameObject bush;
        public GameObject smallBush;
        public GameObject grass;
        public GameObject rock;
        public GameObject smallRock;
        public GameObject fence;
        public GameObject logs;
    }
}
