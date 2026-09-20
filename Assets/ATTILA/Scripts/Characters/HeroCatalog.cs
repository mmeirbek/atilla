using ATTILA.Save;
using UnityEngine;

namespace ATTILA.Characters
{
    /// <summary>The three playable roles, in the order the character select carousel shows them.</summary>
    public static class HeroCatalog
    {
        public static readonly string[] Order = { "ROLE_KEREY", "ROLE_ABYLAI", "ROLE_KABANBAY" };

        public static HeroDefinition Get(string heroId) =>
            string.IsNullOrEmpty(heroId) ? null : Resources.Load<HeroDefinition>("Data/Roles/" + heroId);

        public static HeroDefinition Selected => Get(SaveService.Current.selectedHeroId) ?? Get(Order[0]);
    }
}
