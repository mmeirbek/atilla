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
        [SerializeField] private string era;
        [SerializeField] private string tagline;
        [SerializeField] private string chapterId;
        public string HeroId => heroId;
        public string DisplayName => displayName;
        public string Description => description;
        public int ProgressPercent => progressPercent;
        public Sprite Portrait => portrait;
        /// <summary>Look id under Resources/Data/Looks used for the in-world sprite and dialogue portrait.</summary>
        public string VisualSetId => visualSetId;
        public string Era => era;
        public string Tagline => tagline;
        public string ChapterId => chapterId;
    }
}
