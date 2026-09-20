using System;
using System.Collections.Generic;
using UnityEngine;

namespace ATTILA.Data
{
    /// <summary>Every story string and chapter definition lives here so text can be reviewed and replaced without code changes.</summary>
    [CreateAssetMenu(menuName = "ATTILA/Story Content", fileName = "Stage2Content")]
    public sealed class Stage2Content : ScriptableObject
    {
        public List<NpcDefinition> npcs = new();
        public List<DialogueDefinition> dialogues = new();
        public List<ChapterDefinition> chapters = new();
        public List<EncyclopediaEntryDefinition> encyclopediaEntries = new();
        public List<OutfitItemDefinition> outfitItems = new();

        public NpcDefinition Npc(string id) => npcs.Find(value => value.id == id);
        public DialogueDefinition Dialogue(string id) => string.IsNullOrEmpty(id) ? null : dialogues.Find(value => value.id == id);
        public ChapterDefinition Chapter(string id) => chapters.Find(value => value.id == id);
        public EncyclopediaEntryDefinition Entry(string id) => encyclopediaEntries.Find(value => value.id == id);
    }

    /// <summary>Draft is the default so no text reads as historically confirmed until someone verifies it against a source.</summary>
    public enum ContentStatus { Draft, Verified }

    [Serializable]
    public sealed class NpcDefinition
    {
        public string id;
        public string displayName;
        public float interactionRadius = 2f;
        [Tooltip("Sprite look id under Resources/Data/Looks.")] public string look;
        [Tooltip("Shown when the player talks to this NPC while the story wants somebody else.")] public string idleDialogueId;
        [Tooltip("Shown from the second time onwards, so the same speech is not repeated.")] public string repeatDialogueId;
    }

    [Serializable]
    public sealed class DialogueChoice
    {
        public string label;
        public string nextDialogueId;
        public string setFlag;
        public int xp;
        public int unity;
    }

    [Serializable]
    public sealed class DialogueDefinition
    {
        public string id;
        public string speakerName;
        [Tooltip("Look id used for the portrait; empty means no portrait.")] public string speakerLook;
        [TextArea] public string[] lines;
        public List<DialogueChoice> choices = new();
        public string continueLabel = "Жалғастыру";
        public ContentStatus status;
        public string sourceNote;
    }

    public enum BeatAction { None, SokyrTeke, Asyk, Duel, Campfire, Map, Collect, Find, Escort, Build }

    /// <summary>A job done in the world rather than in a dialogue: gather, search, lead or assemble.</summary>
    [Serializable]
    public sealed class TaskDefinition
    {
        public string title;
        [TextArea] public string hint;
        [Tooltip("Prop sprite ids; one per point, or a single id used for every point.")] public string[] props;
        [Tooltip("Pickup, search or start positions in world x/z.")] public Vector2[] points;
        [Tooltip("Where the thing has to end up: the hitching post or the yurt site.")] public Vector2 goal;
        public int required = 1;
        public string itemLabel;
    }

    [Serializable]
    public sealed class CampfireTopic
    {
        public string label;
        public string dialogueId;
        public string unlockEntryId;
    }

    [Serializable]
    public sealed class CampfireDefinition
    {
        public string title;
        public List<CampfireTopic> topics = new();
    }

    [Serializable]
    public sealed class MapDefinition
    {
        public string title;
        public string goal;
        public string startLabel;
        public string endLabel;
        [Tooltip("Route points in 0..1 map space, origin bottom-left.")] public Vector2[] route;
    }

    /// <summary>One story step: walk to an NPC, optionally hear a dialogue, then optionally run an action. Rewards apply on completion.</summary>
    [Serializable]
    public sealed class BeatDefinition
    {
        public string npcId;
        public string objective;
        public string dialogueId;
        public string altDialogueId;
        public string altFlag;
        public BeatAction action;
        public int xp;
        public string[] unlockEntryIds;
        public CampfireDefinition campfire;
        public MapDefinition map;
        public TaskDefinition task;
        public string checkpoint;
    }

    /// <summary>Places an interactive NPC, or a background figure when npcId is empty and look is set.</summary>
    [Serializable]
    public sealed class NpcPlacement
    {
        public string npcId;
        public string look;
        public Vector2 position;
        public FacingHint facing = FacingHint.South;
    }

    public enum FacingHint { South, East, West, North }

    [Serializable]
    public sealed class ChapterDefinition
    {
        public string id;
        public string roleId;
        public string title;
        public string subtitle;
        public List<NpcPlacement> npcs = new();
        public List<BeatDefinition> beats = new();
        public string finaleTitle;
        public string finaleText;
        [Tooltip("Shown instead of finaleText when the role's unity value reaches finaleAltMinUnity (0 disables).")] public string finaleAltText;
        public int finaleAltMinUnity;
        [Header("Atmosphere")]
        [Tooltip("Multiplies the whole pixelated world view: the chapter's time of day.")] public Color worldTint = Color.white;
        public Color skyColor = new(.65f, .82f, .91f);
        public Color ambientColor = new(.90f, .93f, .86f);
        public Color sunColor = Color.white;
        public float sunIntensity = .55f;
        public Vector3 sunEuler = new(50f, -30f, 0f);
        [Tooltip("red, blue or dark")] public string banner = "red";
    }

    [Serializable]
    public sealed class EncyclopediaEntryDefinition
    {
        public string id;
        public string category;
        public string title;
        [Tooltip("Common knowledge: readable before the player unlocks anything.")] public bool openFromStart;
        [Tooltip("Look id whose portrait illustrates the entry.")] public string look;
        [Tooltip("Icon id from the MVP art set, used when no look is given.")] public string icon;
        [TextArea] public string lockedDescription;
        [TextArea] public string unlockedDescription;
        public ContentStatus status;
        public string sourceNote;
    }

    [Serializable]
    public sealed class OutfitItemDefinition
    {
        public string id;
        public string icon;
        [Tooltip("Look ids that wear this item; the wardrobe shows only what the previewed character wears.")] public string[] looks;
        public string slot;
        public string title;
        [TextArea] public string description;
        public ContentStatus status;
        public string sourceNote;
    }
}
