using UnityEngine;

namespace ATTILA.Characters
{
    [CreateAssetMenu(menuName = "ATTILA/Hero Definition", fileName = "TEMP_HERO")]
    public sealed class HeroDefinition : ScriptableObject
    {
        [SerializeField] private string heroId = "TEMP_HERO";
        [SerializeField] private string displayName = "Белгісіз кейіпкер";
        [SerializeField] private string description = "Уақытша кейіпкер орны";
        [SerializeField, Range(0, 100)] private int progressPercent;
        [SerializeField] private Sprite portrait;
        [SerializeField] private string visualSetId = "TEMP_HERO";
        public string HeroId => heroId;
        public string DisplayName => displayName;
        public string Description => description;
        public int ProgressPercent => progressPercent;
        public Sprite Portrait => portrait;
        public string VisualSetId => visualSetId;
    }
}
