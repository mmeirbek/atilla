using UnityEngine;

namespace ATTILA.UI
{
    /// <summary>Presentation-only references for the Main Menu. Replacing sprites does not alter navigation.</summary>
    [CreateAssetMenu(fileName = "MainMenuVisuals", menuName = "ATTILA/UI/Main Menu Visuals")]
    public sealed class MenuVisualDefinition : ScriptableObject
    {
        public Sprite background;
        public Sprite logo;
        public Sprite playButton;
        public Sprite wardrobeButton;
        public Sprite encyclopediaButton;
        public Sprite settingsButton;
        public Sprite quitButton;
        public Sprite kereyKhanCard;
        public Sprite character;
        public Sprite khan;
        public Material characterMaterial;
    }
}
