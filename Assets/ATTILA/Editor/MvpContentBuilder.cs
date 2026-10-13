using System.Collections.Generic;
using System.IO;
using ATTILA.Characters;
using ATTILA.Data;
using UnityEditor;
using UnityEngine;

namespace ATTILA.Editor
{
    /// <summary>
    /// Writes the three MVP chapters, dialogues, encyclopedia and wardrobe text, and the three role assets.
    /// Every historical statement is Draft with a [ПРОВЕРИТЬ] source note until checked against a source.
    /// Real people never speak: fictional NPCs talk, and the hero's decisions are the player's choices.
    /// </summary>
    public static class MvpContentBuilder
    {
        private const string ContentPath = "Assets/ATTILA/Resources/Data/Stage2Content.asset";
        private const string RolesFolder = "Assets/ATTILA/Resources/Data/Roles/";
        private const string Check = "[ПРОВЕРИТЬ] ";
        private static Stage2Content content;

        /// <summary>One click for a fresh checkout: import the generated art, then rebuild the content.</summary>
        [MenuItem("ATTILA/Build MVP (Art + Content)")]
        public static void BuildAll()
        {
            MvpArtImporter.Import();
            Build();
        }

        [MenuItem("ATTILA/Generate MVP Content")]
        public static void Build()
        {
            content = AssetDatabase.LoadAssetAtPath<Stage2Content>(ContentPath);
            if (content == null) { content = ScriptableObject.CreateInstance<Stage2Content>(); AssetDatabase.CreateAsset(content, ContentPath); }
            content.npcs = Npcs();
            content.dialogues = new List<DialogueDefinition>();
            content.chapters = new List<ChapterDefinition> { Kerey(), Abylai(), Kabanbay() };
            IdleDialogues();
            content.encyclopediaEntries = Entries();
            content.outfitItems = Outfits();
            EditorUtility.SetDirty(content);
            BuildRoles();
            AssetDatabase.SaveAssets();
            Debug.Log($"ATTILA CONTENT PASS: {content.chapters.Count} chapters, {content.dialogues.Count} dialogues, {content.encyclopediaEntries.Count} entries.");
        }

        // ------------------------------------------------------------ helpers

        private static DialogueDefinition Say(string id, string speaker, string look, params string[] lines)
        {
            var dialogue = new DialogueDefinition { id = id, speakerName = speaker, speakerLook = look, lines = lines };
            content.dialogues.Add(dialogue);
            return dialogue;
        }

        private static DialogueDefinition History(DialogueDefinition dialogue, string note)
        {
            dialogue.status = ContentStatus.Draft;
            dialogue.sourceNote = Check + note;
            return dialogue;
        }

        private static DialogueChoice Choice(string label, string next, string flag = null, int xp = 0, int unity = 0) =>
            new() { label = label, nextDialogueId = next, setFlag = flag, xp = xp, unity = unity };

        private static NpcPlacement At(string npcId, float x, float z, FacingHint facing = FacingHint.South, LocationId location = LocationId.Aul) =>
            new() { npcId = npcId, position = new Vector2(x, z), facing = facing, location = location };

        private static NpcPlacement Figure(string look, float x, float z, FacingHint facing = FacingHint.South, LocationId location = LocationId.Aul) =>
            new() { look = look, position = new Vector2(x, z), facing = facing, location = location };

        /// <summary>Moves a beat to another place; the player has to ride there before it can start.</summary>
        private static BeatDefinition In(BeatDefinition beat, LocationId location)
        {
            beat.location = location;
            return beat;
        }

        private static BeatDefinition Beat(string npc, string objective, string dialogue, int xp, string checkpoint, BeatAction action = BeatAction.None, params string[] unlock) =>
            new() { npcId = npc, objective = objective, dialogueId = dialogue, xp = xp, checkpoint = checkpoint, action = action, unlockEntryIds = unlock };

        private static BeatDefinition TaskBeat(string npc, string objective, string dialogue, int xp, string checkpoint, BeatAction action, TaskDefinition task, params string[] unlock)
        {
            var beat = Beat(npc, objective, dialogue, xp, checkpoint, action, unlock);
            beat.task = task;
            return beat;
        }

        private static TaskDefinition Job(string title, string hint, string itemLabel, string[] props, Vector2[] points, int required = 1, Vector2 goal = default) =>
            new() { title = title, hint = hint, itemLabel = itemLabel, props = props, points = points, required = required, goal = goal };

        private static CampfireTopic Topic(string label, string dialogue, string entry = null) => new() { label = label, dialogueId = dialogue, unlockEntryId = entry };

        private static NpcDefinition Npc(string id, string name, string look, float radius) => new()
        {
            id = id, displayName = name, look = look, interactionRadius = radius,
            idleDialogueId = "DIA_IDLE_" + id, repeatDialogueId = "DIA_AGAIN_" + id
        };

        private static List<NpcDefinition> Npcs() => new()
        {
            Npc("NPC_BI", "Би", "bi", 2.3f),
            Npc("NPC_AISHA", "Айша", "woman", 2.2f),
            Npc("NPC_CHILDREN", "Балалар", "child", 2.4f),
            Npc("NPC_ELDER", "Ақсақал", "elder", 2.4f),
            Npc("NPC_HERDER", "Малшы", "resident", 2.0f),
            Npc("NPC_ENVOY_WEST", "Кіші жүз елшісі", "envoy", 2.3f),
            Npc("NPC_ENVOY_SOUTH", "Ұлы жүз елшісі", "envoy2", 2.3f),
            Npc("NPC_COMMANDER", "Жүзбасы", "warrior", 2.3f),
            Npc("NPC_SMITH", "Ұста", "resident", 2.2f),
            Npc("NPC_SCOUT", "Барлаушы", "envoy", 2.3f)
        };

        /// <summary>First greeting, then a short line so the same speech never plays twice.</summary>
        private static void Chat(string npcId, string name, string look, string first, string again)
        {
            Say("DIA_IDLE_" + npcId, name, look, first);
            Say("DIA_AGAIN_" + npcId, name, look, again);
        }

        private static void IdleDialogues()
        {
            Chat("NPC_BI", "Би", "bi", "Асықпа. Әр істің өз уақыты бар.", "Иә?");
            Chat("NPC_AISHA", "Айша", "woman", "Ауылда шаруа көп: жүк те, бала да менде.", "Шаруам бітпей жатыр.");
            Chat("NPC_CHILDREN", "Балалар", "child", "Біз ойнап жүрміз! Кейін бізге қосылыңыз.", "Ойнаймыз ба?");
            Chat("NPC_ELDER", "Ақсақал", "elder", "Кешке от басында әңгіме айтамын.", "Кешке кел.");
            Chat("NPC_HERDER", "Малшы", "resident", "Жылқылар жайылымнан оралды. Бәрі аман.", "Жылқы аман.");
            Chat("NPC_ENVOY_WEST", "Кіші жүз елшісі", "envoy", "Шешіміңізді күтеміз.", "Күтіп тұрмыз.");
            Chat("NPC_ENVOY_SOUTH", "Ұлы жүз елшісі", "envoy2", "Ұлы жүз сізге сәлем айтты.", "Сәлем.");
            Chat("NPC_COMMANDER", "Жүзбасы", "warrior", "Жасақ сапта тұр, батыр.", "Бұйрық күтемін.");
            Chat("NPC_SMITH", "Ұста", "resident", "Балғам тынбайды — қару-жарақ көп.", "Жұмыс көп.");
            Chat("NPC_SCOUT", "Барлаушы", "envoy", "Жау жақын. Сақ болыңыз.", "Сақ бол.");
        }

        // ------------------------------------------------------------ chapter I: Kerey, "Көш"

        private static ChapterDefinition Kerey()
        {
            var bi = Say("DIA_K_BI", "Би", "bi",
                "Керей сұлтан, Әбілқайыр ордасында тыныштық жоқ. Көп ауыл ауа көшуге бел буды.",
                "Жәнібек сұлтан да осы ойда. Шу бойында, Жетісуда жер кең, жайылым мол.",
                "Ел сіздің шешіміңізді күтіп отыр. Не айтасыз?");
            History(bi, "откочёвка от Абулхаира к Жетысу — сверить с учебником «Қазақстан тарихы»");
            bi.choices.Add(Choice("Көшті бастаймыз", "DIA_K_BI_GO", "bold", 10));
            bi.choices.Add(Choice("Алдымен елдің ойын тыңдайық", "DIA_K_BI_LISTEN", "listen", 20, 20));
            Say("DIA_K_BI_GO", "Би", "bi", "Батыл шешім. Бірақ қарт пен баланы ұмытпаңыз — көш солардың қарқынымен жүреді.");
            Say("DIA_K_BI_LISTEN", "Би", "bi", "Дана сөз. Елдің келісімі — көштің берік қазығы. Айша дайындықты бастап қойды.");
            Say("DIA_K_AISHA", "Айша", "woman",
                "Көш алдында от жағамыз, бірақ отын жетпей тұр.",
                "Ауыл маңынан төрт бума отын жинап әкелші.");
            Say("DIA_K_AISHA_DONE", "Айша", "woman",
                "Отын жетеді, рақмет. Қазан асылады.",
                "Енді малшыға көмектес: жаңа қонысқа киіз үй тігу керек.");
            Say("DIA_K_HERDER", "Малшы", "resident",
                "Киіз үйдің бөліктері шашылып жатыр: кереге, уық, шаңырақ, киіз.",
                "Ретімен әкел: алдымен кереге, сосын уық, шаңырақ, ең соңында киіз.");
            Say("DIA_K_CHILDREN", "Балалар", "child",
                "Сұлтан аға, көш алдында бізбен ойнаңызшы!",
                "Сіз «теке» боласыз: көзіңізді байлаймыз, ал біз шапалақ соғамыз!");
            Say("DIA_K_ELDER", "Ақсақал", "elder", "Көш алдындағы соңғы түн. Отқа жақын отырыңыз, әңгіме айтайын.");
            History(Say("DIA_K_T1", "Ақсақал", "elder",
                "Әбілқайыр хан тұсында ру басылары арасында алауыздық көбейді.",
                "Елдің бір бөлігі Керей мен Жәнібекке еріп, Моғолстан шекарасына қарай көшті.",
                "Осы көш Қазақ хандығының бастауы болды деп айтылады."), "причины откочёвки и её направление");
            Say("DIA_K_T2", "Ақсақал", "elder",
                "Жетісу — жеті өзеннің өлкесі. Тауы биік, жайлауы шүйгін.",
                "Шу мен Талас бойында малға да жер жетеді.");
            History(Say("DIA_K_T4", "Ақсақал", "elder",
                "Ұлытау — даланың кіндігі саналады.",
                "Ол жақта көне қорымдар мен кесенелер бар."), "памятники Улытау");
            Say("DIA_K_T3", "Ақсақал", "elder",
                "Соқыр теке — баланың құлағын, сезімін шыңдайтын ойын.",
                "Көзі байлаулы бала дыбысқа қарап жол табады.");
            History(Say("DIA_K_ROUTE", "Би", "bi",
                "Ауыл далаға шықты, жүк артылды.",
                "Көш бағыты — Шу бойы, Қозыбасы."), "Қозыбасы как место откочёвки");

            var chapter = new ChapterDefinition
            {
                id = "CH_KEREY", roleId = "ROLE_KEREY", title = "I тарау · Көш", subtitle = "XV ғасыр",
                intro = "Әбілқайыр ордасында алауыздық күшейді. Керей мен Жәнібек сұлтандар елді жаңа қонысқа бастауға бел буды.",
                finaleTitle = "I ТАРАУ АЯҚТАЛДЫ",
                finaleText = "Көш Жетісуға аман жетті. Жаңа қонысқа орныққан ел кейін Қазақ хандығы деген атпен тарихта қалды.",
                finaleAltText = "Көш Жетісуға аман жетті. Елдің ойын тыңдаған сұлтанға халық сенді — жаңа қоныста Қазақ хандығының іргетасы қаланды.",
                finaleAltMinUnity = 20,
                worldTint = new Color(1f, .93f, .82f), skyColor = new Color(.93f, .78f, .62f), banner = "red",
                allyMinUnity = 20, allyMessage = "Көрші ауыл да көшке қосылды"
            };
            chapter.allies.AddRange(new[] { Figure("resident", 3.2f, -4.2f, FacingHint.West), Figure("woman", 4.6f, -3.2f, FacingHint.West), Figure("child", 4f, -5.4f) });
            chapter.npcs.AddRange(new[]
            {
                At("NPC_BI", -4f, 6f), At("NPC_AISHA", -8f, 4.5f, FacingHint.East), At("NPC_CHILDREN", 5f, 7f), At("NPC_ELDER", -5.6f, -1.2f, FacingHint.East),
                At("NPC_HERDER", 10.5f, -1.5f, FacingHint.West), Figure("child_girl", 6.3f, 6.2f, FacingHint.West), Figure("woman", -10f, 12.4f), Figure("resident", 8f, 12f, FacingHint.West),
                At("NPC_BI", -1.6f, 9.4f, FacingHint.South, LocationId.Steppe), Figure("woman", 3.4f, 8.2f, FacingHint.West, LocationId.Steppe),
                Figure("resident", -4.2f, 6.6f, FacingHint.East, LocationId.Steppe), Figure("child", 1.2f, 5.4f, FacingHint.North, LocationId.Steppe)
            });
            chapter.beats.Add(Beat("NPC_BI", "Бимен кеңес", "DIA_K_BI", 50, "KEREY_COUNCIL"));
            chapter.beats.Add(TaskBeat("NPC_AISHA", "Айшаға отын жина", "DIA_K_AISHA", 60, "KEREY_FIREWOOD", BeatAction.Collect,
                Job("ОТЫН ЖИНАУ", "Ауыл маңындағы төрт бума отынды жина.", "Отын", new[] { "firewood" },
                    new[] { new Vector2(-6.5f, 11.5f), new Vector2(6.8f, -9.5f), new Vector2(-9.6f, -3.2f), new Vector2(9.2f, 8.6f), new Vector2(-14.5f, 4.5f) }, 4)));
            chapter.beats.Add(Beat("NPC_AISHA", "Отынды Айшаға апар", "DIA_K_AISHA_DONE", 30, "KEREY_PREPARE", BeatAction.None, "kosh"));
            chapter.beats.Add(TaskBeat("NPC_HERDER", "Малшымен киіз үй тік", "DIA_K_HERDER", 80, "KEREY_YURT", BeatAction.Build,
                Job("КИІЗ ҮЙ ТІГУ", "Бөліктерді ретімен апар: кереге, уық, шаңырақ, киіз.", "Бөлік",
                    new[] { "kerege", "uyk", "shanyrak", "felt" },
                    new[] { new Vector2(-11.5f, 2.6f), new Vector2(-5.4f, -4.2f), new Vector2(-10.8f, -1.8f), new Vector2(-5.6f, 3.4f) }, 4, new Vector2(-8.4f, .4f)),
                "kiiz_ui"));
            chapter.beats.Add(Beat("NPC_CHILDREN", "Балалармен «Соқыр теке» ойна", "DIA_K_CHILDREN", 100, "KEREY_GAME", BeatAction.SokyrTeke, "sokyr_teke"));
            var night = Beat("NPC_ELDER", "Кешкі от басына бар", "DIA_K_ELDER", 50, "KEREY_NIGHT", BeatAction.Campfire);
            night.campfire = new CampfireDefinition { title = "Көш алдындағы түн" };
            night.campfire.topics.AddRange(new[] { Topic("Ел неге көшеді?", "DIA_K_T1", "kazakh_khanate"), Topic("Жетісу жері", "DIA_K_T2", "zhetysu"), Topic("Бабалар ойыны", "DIA_K_T3", "sokyr_teke"), Topic("Ұлытау жайлы", "DIA_K_T4", "ulytau") });
            chapter.beats.Add(night);
            var route = In(Beat("NPC_BI", "Далада көшті бастау", "DIA_K_ROUTE", 100, "KEREY_COMPLETE", BeatAction.Map, "kerey_khan", "kazakh_khanate", "kosh", "saryarka"), LocationId.Steppe);
            route.map = new MapDefinition { title = "Көш жолы", goal = "Шу бойына, Жетісуға жету", startLabel = "Сырдария бойы", endLabel = "Шу, Қозыбасы",
                route = new[] { new Vector2(.39f, .30f), new Vector2(.47f, .34f), new Vector2(.55f, .31f), new Vector2(.64f, .27f) } };
            chapter.beats.Add(route);
            return chapter;
        }

        // ------------------------------------------------------------ chapter II: Abylai, "Бірлік"

        private static ChapterDefinition Abylai()
        {
            var bi = Say("DIA_A_BI", "Би", "bi",
                "Абылай сұлтан, жоңғар қаупі әлі сейілген жоқ. Шекарада шапқыншылар жиі көрінеді.",
                "Қолда жасақ бар. Бірақ ел бытыраңқы — әр жүз өз қамымен жүр.",
                "Қалай істейміз: бірінші болып соққы береміз бе, әлде күшімізді біріктіреміз бе?");
            History(bi, "обстановка перед избранием Абылая ханом");
            bi.choices.Add(Choice("Бірінші болып соққы береміз", "DIA_A_BI_STRIKE", "strike", 10));
            bi.choices.Add(Choice("Күшімізді біріктірейік", "DIA_A_BI_UNITE", "unite", 20, 30));
            Say("DIA_A_BI_STRIKE", "Би", "bi", "Жүрек батыл. Бірақ жалғыз қолдан шапалақ шықпас — алдымен елшілерді тыңдаңыз.");
            Say("DIA_A_BI_UNITE", "Би", "bi", "Дұрыс шешім. Үш жүздің басы қосылса, жау алмайтын қамал жоқ. Елшілер сізді күтіп тұр.");
            var west = Say("DIA_A_ENVOY_W", "Кіші жүз елшісі", "envoy",
                "Сұлтан, біздің ауылдар жайылым үшін көршілерімен дауласып жүр.",
                "Әділ төрелік айтсаңыз, ел сізге құлақ асады.");
            west.choices.Add(Choice("Екі жақты да тыңдап, әділ бітім айтамын", "DIA_A_ENVOY_W_FAIR", "fair", 20, 30));
            west.choices.Add(Choice("Күштінің сөзі өтеді", "DIA_A_ENVOY_W_FORCE", "force", 10));
            Say("DIA_A_ENVOY_W_FAIR", "Кіші жүз елшісі", "envoy", "Бұл — бидің де, ханның да жолы. Біз бірлікке қосыламыз.");
            Say("DIA_A_ENVOY_W_FORCE", "Кіші жүз елшісі", "envoy", "Күшпен тоқтатылған дау кейін қайта өршиді... Бірақ бұйрығыңызды жеткіземін.");
            var south = Say("DIA_A_ENVOY_S", "Ұлы жүз елшісі", "envoy2",
                "Оңтүстікте керуен жолы қауіпті. Түркістан маңы тыныш болуы керек.",
                "Біз сізден қолдау күтеміз.");
            south.choices.Add(Choice("Жасақ жіберіп, жолды қорғаймыз", "DIA_A_ENVOY_S_GUARD", "guard", 20, 20));
            south.choices.Add(Choice("Кеңесіп, бірге шешейік", "DIA_A_ENVOY_S_COUNCIL", "council", 20, 30));
            Say("DIA_A_ENVOY_S_GUARD", "Ұлы жүз елшісі", "envoy2", "Рақмет. Батырлар жолды күзетсе, керуен де, ел де тынады.");
            Say("DIA_A_ENVOY_S_COUNCIL", "Ұлы жүз елшісі", "envoy2", "Кеңесті ел азбайды. Түркістандағы жиында кездесеміз.");
            Say("DIA_A_HERDER", "Малшы", "resident",
                "Елшінің аты бос жүр, сұлтаным.",
                "Атты бағанаға дейін жетелеп апарыңызшы — соңыңыздан ереді.");
            Say("DIA_A_BAIGE", "Малшы", "resident",
                "Жиын алдында бәйге бар, сұлтаным. Ауыл жігіті сізбен жарысқысы келеді.",
                "Бес белгіні ретімен алып, мәреге бірінші жетсеңіз — жеңіс сіздікі.");
            Say("DIA_A_CHILDREN", "Балалар", "child",
                "Сұлтан аға, сақаңызды алыңыз! Асық атып көрейік.",
                "Бес рет атасыз. Үш асықты шеңберден шығарсаңыз — жеңіс сіздікі!");
            Say("DIA_A_ELDER", "Ақсақал", "elder", "Ертең ұзақ жол. Бүгін от басында отырып, бабалар сөзін тыңдайық.");
            History(Say("DIA_A_T1", "Ақсақал", "elder",
                "1723 жылы жоңғарлар кенеттен шабуыл жасады. Ел ауыр күйге түсті.",
                "Халық ол жылдарды «Ақтабан шұбырынды» деп атады."), "1723 год и название «Ақтабан шұбырынды»");
            History(Say("DIA_A_T2", "Ақсақал", "elder",
                "Құрметті кісіге шапан жабу — ізет пен сыйдың белгісі.",
                "Шапанның сәні де, жылуы да — шеберінің қолында."), "обычай «шапан жабу»");
            History(Say("DIA_A_T4", "Ақсақал", "elder",
                "Ауыр жылдары ел жаппай босыпты.",
                "Халық ол кезеңді «Ақтабан шұбырынды» деп атаған."), "происхождение выражения");
            History(Say("DIA_A_T5", "Ақсақал", "elder",
                "Хан сайлауда ел өкілдері жиналады.",
                "Жаңа ханды ақ киізге көтеріп, жұртқа таныстырады."), "обряд поднятия на белой кошме");
            Say("DIA_A_T3", "Ақсақал", "elder",
                "Асық — ауыл баласының сүйікті ойыны.",
                "Сақаны дәл ату үшін көз де, шыдам да керек.");
            History(Say("DIA_A_ROUTE", "Би", "bi",
                "Қала қақпасы ашық, үш жүздің өкілдері жиналып қалды.",
                "Ел алдына шығатын уақыт жетті."), "место избрания — Түркістан");

            var chapter = new ChapterDefinition
            {
                id = "CH_ABYLAI", roleId = "ROLE_ABYLAI", title = "II тарау · Бірлік", subtitle = "XVIII ғасыр",
                intro = "Жоңғар шапқыншылығы елді әлсіретті. Абылай сұлтан үш жүздің басын қосып, ел бірлігін нығайтуы керек.",
                finaleTitle = "II ТАРАУ АЯҚТАЛДЫ",
                finaleText = "Түркістанда ел жиналды. Дәстүр бойынша Абылайды ақ киізге көтеріп, хан сайлады.",
                finaleAltText = "Түркістанда үш жүздің өкілдері бір дастарқанға жиналды. Бірлікті таңдаған Абылайды ел ақ киізге көтеріп, хан сайлады.",
                finaleAltMinUnity = 60,
                worldTint = new Color(1f, 1f, .96f), skyColor = new Color(.66f, .82f, .92f), banner = "blue",
                allyMinUnity = 50, allyMessage = "Кіші жүз бен Ұлы жүз жасағы келді", duelAllyMinUnity = 50
            };
            chapter.allies.AddRange(new[] { Figure("warrior", -4.6f, -4.6f, FacingHint.East), Figure("warrior", -3.2f, -6f, FacingHint.North), Figure("envoy2", 4.2f, -5f, FacingHint.West) });
            chapter.npcs.AddRange(new[]
            {
                At("NPC_BI", -4f, 6f), At("NPC_ENVOY_WEST", -8.5f, 3.5f, FacingHint.East), At("NPC_ENVOY_SOUTH", 8.4f, 3.2f, FacingHint.West),
                At("NPC_CHILDREN", 5f, 7f), At("NPC_ELDER", -5.6f, -1.2f, FacingHint.East), At("NPC_HERDER", 10.5f, -1.5f, FacingHint.West),
                Figure("child_girl", 6.3f, 6.2f, FacingHint.West), Figure("warrior", 2.6f, 12.4f), Figure("warrior", -2.6f, 12.4f), Figure("woman", -10f, 12.4f),
                At("NPC_ENVOY_SOUTH", -2.4f, 6.6f, FacingHint.East, LocationId.City), At("NPC_BI", 2.6f, 7.2f, FacingHint.West, LocationId.City),
                Figure("envoy", -6.4f, 2.6f, FacingHint.East, LocationId.City), Figure("warrior", 6.2f, 2.4f, FacingHint.West, LocationId.City),
                Figure("resident", -8.6f, -4.4f, FacingHint.North, LocationId.City), Figure("woman", 8.4f, -3.6f, FacingHint.North, LocationId.City),
                Figure("child", -3.6f, -8.4f, LocationId.City == LocationId.City ? FacingHint.North : FacingHint.South, LocationId.City)
            });
            chapter.beats.Add(Beat("NPC_BI", "Бимен кеңес", "DIA_A_BI", 50, "ABYLAI_COUNCIL"));
            chapter.beats.Add(Beat("NPC_ENVOY_WEST", "Кіші жүз елшісін тыңда", "DIA_A_ENVOY_W", 40, "ABYLAI_WEST"));
            chapter.beats.Add(Beat("NPC_ENVOY_SOUTH", "Ұлы жүз елшісімен сөйлес", "DIA_A_ENVOY_S", 40, "ABYLAI_SOUTH"));
            chapter.beats.Add(TaskBeat("NPC_HERDER", "Елшінің атын бағанаға жетеле", "DIA_A_HERDER", 60, "ABYLAI_HORSE", BeatAction.Escort,
                Job("АТТЫ ЖЕТЕЛЕУ", "Ат соңыңнан ереді. Оны бағанаға дейін апар.", "Ат", new[] { "post" },
                    new[] { new Vector2(11.6f, 6.4f) }, 1, new Vector2(6.4f, -10.6f))));
            chapter.beats.Add(TaskBeat("NPC_HERDER", "Бәйгеге қосыл", "DIA_A_BAIGE", 120, "ABYLAI_BAIGE", BeatAction.Baige,
                Job("БӘЙГЕ", "Бес белгіні ретімен ал, қарсыласыңнан озып мәреге жет.", "Белгі", new[] { "post" },
                    new[] { new Vector2(16f, 12f), new Vector2(18f, -8f), new Vector2(0f, -18f), new Vector2(-18f, -8f), new Vector2(-16f, 12f) },
                    5, new Vector2(0f, -14f)),
                "baige"));
            chapter.beats.Add(Beat("NPC_CHILDREN", "Балалармен асық ат", "DIA_A_CHILDREN", 100, "ABYLAI_GAME", BeatAction.Asyk, "asyk"));
            var night = Beat("NPC_ELDER", "Кешкі от басына бар", "DIA_A_ELDER", 50, "ABYLAI_NIGHT", BeatAction.Campfire);
            night.campfire = new CampfireDefinition { title = "Түркістан алдындағы түн" };
            night.campfire.topics.AddRange(new[] { Topic("Жоңғар заманы", "DIA_A_T1", "dzungar_wars"), Topic("Ақтабан шұбырынды", "DIA_A_T4", "aktaban"), Topic("Хан қалай сайланады?", "DIA_A_T5", "khan_saylau"), Topic("Асық ойыны", "DIA_A_T3", "asyk") });
            chapter.beats.Add(night);
            var route = In(Beat("NPC_BI", "Түркістандағы жиынға қатыс", "DIA_A_ROUTE", 100, "ABYLAI_COMPLETE", BeatAction.Map, "abylai_khan", "turkistan", "syrdariya", "karatau"), LocationId.City);
            route.map = new MapDefinition { title = "Түркістанға жол", goal = "Үш жүздің жиынына жету", startLabel = "Сарыарқа", endLabel = "Түркістан",
                route = new[] { new Vector2(.56f, .76f), new Vector2(.50f, .60f), new Vector2(.45f, .42f), new Vector2(.45f, .25f) } };
            chapter.beats.Add(route);
            return chapter;
        }

        // ------------------------------------------------------------ chapter III: Kabanbay, "Қорғаныс"

        private static ChapterDefinition Kabanbay()
        {
            var council = Say("DIA_B_COUNCIL", "Жүзбасы", "warrior",
                "Батыр, барлаушылар хабар әкелді: жау қолы асудан өтуге дайындалып жатыр.",
                "Жасақ дайын. Бұйрығыңызды күтеміз.");
            council.choices.Add(Choice("Жасақты бір қолға біріктір", "DIA_B_FORTIFY", "united", 20, 40));
            council.choices.Add(Choice("Екі топқа бөлініп, тосқауыл құр", "DIA_B_AMBUSH", "split", 20, 10));
            Say("DIA_B_FORTIFY", "Жүзбасы", "warrior",
                "Құп болады! Барлық жасақ бір тудың астына жиналады.",
                "Жекпе-жекке шыққанда артыңда жасақтас тұрады. Алдымен ұстаға соғып, қаруыңызды алыңыз.");
            Say("DIA_B_AMBUSH", "Жүзбасы", "warrior",
                "Құп болады! Жасақты екіге бөлеміз, сайға тосқауыл қоямыз.",
                "Бірақ жекпе-жекке жалғыз шығасыз. Алдымен ұстаға соғып, қаруыңызды алыңыз.");
            Say("DIA_B_SMITH", "Ұста", "resident",
                "Сауытыңыз жөнделді, найзаңыз қайралды.",
                "Мына шапанды да алыңыз — таулы жерде түн суық.");
            Say("DIA_B_SCOUT_TRACK", "Барлаушы", "envoy",
                "Батыс жақтағы тастардың арасынан жау ізі көрінді.",
                "Барып өз көзіңмен қара — біз мұнда күземіз.");
            Say("DIA_B_AISHA", "Айша", "woman",
                "Ана мен бала таудағы жасырын жайлауға көшті.",
                "Ауыл сізге сенеді, батыр. Аман оралыңыз.");
            Say("DIA_B_SCOUT", "Барлаушы", "envoy",
                "Жау батыры екі қолдың алдына шығып, жекпе-жекке шақырды!",
                "Ата дәстүр бойынша ұрыс жекпе-жектен басталады.");
            Say("DIA_B_ELDER", "Ақсақал", "elder", "Жеңіспен оралдың, батыр. Отқа жақын кел.");
            History(Say("DIA_B_T1", "Ақсақал", "elder",
                "Бұланты мен Аңырақай шайқастарында қазақ жасақтары бірігіп соғысты.",
                "Бұл жеңістер елдің рухын көтерді."), "битвы при Бұланты и Аңырақай, их даты");
            Say("DIA_B_T2", "Ақсақал", "elder",
                "Батыр — ел қорғаны.",
                "Оның күші тек білегінде емес, елге деген адалдығында.");
            History(Say("DIA_B_T4", "Ақсақал", "elder",
                "Аңырақай — Балқаштың оңтүстігіндегі дала.",
                "Сол жердегі шайқаста үш жүздің жасағы бірге қимылдаған."), "дата и место Аңырақай");
            History(Say("DIA_B_T5", "Ақсақал", "elder",
                "Қанжығалы Бөгенбай да сол замандағы қолбасшы.",
                "Ел оның атын Қабанбаймен қатар атайды."), "годы жизни Бөгенбая");
            History(Say("DIA_B_T3", "Ақсақал", "elder",
                "Жеңіспен оралған батырға шапан жабады.",
                "Бұл — ел алғысының белгісі."), "обычай «шапан жабу» для батыра");
            History(Say("DIA_B_ROUTE", "Жүзбасы", "warrior", "Жасақтар бірігіп жатыр. Келесі жиын — Аңырақай даласында."), "место сбора — Аңырақай");

            var chapter = new ChapterDefinition
            {
                id = "CH_KABANBAY", roleId = "ROLE_KABANBAY", title = "III тарау · Қорғаныс", subtitle = "XVIII ғасыр",
                intro = "Жау қолы шекараға жақындады. Қабанбай батыр жасақты бастап, елді қорғауға аттанады.",
                finaleTitle = "III ТАРАУ АЯҚТАЛДЫ",
                finaleText = "Біріккен қазақ жасақтары жауға тойтарыс берді. Аңырақай даласындағы жеңіс халық жадында сақталды.",
                worldTint = new Color(.94f, .84f, .86f), skyColor = new Color(.62f, .52f, .66f), banner = "dark",
                allyMinUnity = 40, allyMessage = "Жасақ бір тудың астына жиналды", duelAllyMinUnity = 40,
                finaleAltText = "Бір тудың астына жиналған жасақ жауды тойтарды. Аңырақай даласындағы жеңісте әр рудың үлесі болды.",
                finaleAltMinUnity = 40
            };
            chapter.allies.AddRange(new[] { Figure("warrior", -4.8f, -4.4f, FacingHint.East), Figure("warrior", -3.4f, -6.2f, FacingHint.North), Figure("warrior", 3.6f, -5.2f, FacingHint.West) });
            chapter.npcs.AddRange(new[]
            {
                At("NPC_COMMANDER", -4f, 6f), At("NPC_SMITH", 8.4f, 3.2f, FacingHint.West), At("NPC_AISHA", -8f, 4.5f, FacingHint.East),
                At("NPC_SCOUT", 5f, 7f), At("NPC_ELDER", -5.6f, -1.2f, FacingHint.East),
                Figure("warrior", 2.6f, 12.4f), Figure("warrior", -2.6f, 12.4f), Figure("warrior", 3.4f, 9.6f, FacingHint.West), Figure("resident", 10.8f, -2f, FacingHint.West),
                At("NPC_SCOUT", -1.8f, 9.6f, FacingHint.South, LocationId.Steppe), Figure("warrior", 3.2f, 8.4f, FacingHint.West, LocationId.Steppe),
                Figure("warrior", -4.6f, 7.2f, FacingHint.East, LocationId.Steppe), At("NPC_COMMANDER", 1.6f, 5.2f, FacingHint.North, LocationId.Steppe)
            });
            chapter.beats.Add(Beat("NPC_COMMANDER", "Әскери кеңеске қатыс", "DIA_B_COUNCIL", 50, "KABANBAY_COUNCIL"));
            chapter.beats.Add(Beat("NPC_SMITH", "Ұстадан қару-жарақ ал", "DIA_B_SMITH", 30, "KABANBAY_SMITH", BeatAction.None, "sauyt"));
            chapter.beats.Add(TaskBeat("NPC_SCOUT", "Батыстан жау ізін тап", "DIA_B_SCOUT_TRACK", 70, "KABANBAY_TRACK", BeatAction.Find,
                Job("ЖАУ ІЗІН ТАБУ", "Батыстағы үлкен тастардың арасын қара.", "Жау ізі", new[] { "torsyk" },
                    new[] { new Vector2(-16.2f, -6.4f) })));
            chapter.beats.Add(Beat("NPC_AISHA", "Ауылдың аман екенін біл", "DIA_B_AISHA", 30, "KABANBAY_AUL"));
            chapter.beats.Add(In(Beat("NPC_SCOUT", "Далаға шығып, жекпе-жекке тұр", "DIA_B_SCOUT", 150, "KABANBAY_DUEL", BeatAction.Duel, "dzungar_wars", "zhekpe_zhek"), LocationId.Steppe));
            var night = Beat("NPC_ELDER", "Жеңіс түні: от басына бар", "DIA_B_ELDER", 50, "KABANBAY_NIGHT", BeatAction.Campfire);
            night.campfire = new CampfireDefinition { title = "Жеңіс түні" };
            night.campfire.topics.AddRange(new[] { Topic("Соғыстар мен шайқастар", "DIA_B_T1", "dzungar_wars"), Topic("Аңырақай даласы", "DIA_B_T4", "anyrakay"), Topic("Батыр кім?", "DIA_B_T2", "sauyt"), Topic("Бөгенбай туралы", "DIA_B_T5", "bogenbay_batyr") });
            chapter.beats.Add(night);
            var route = In(Beat("NPC_COMMANDER", "Аңырақайға аттан", "DIA_B_ROUTE", 100, "KABANBAY_COMPLETE", BeatAction.Map, "kabanbay_batyr", "balkash", "anyrakay"), LocationId.Steppe);
            route.map = new MapDefinition { title = "Аңырақайға жол", goal = "Біріккен қолға қосылу", startLabel = "Сарыарқа", endLabel = "Аңырақай",
                route = new[] { new Vector2(.56f, .76f), new Vector2(.52f, .62f), new Vector2(.56f, .48f), new Vector2(.63f, .37f) } };
            chapter.beats.Add(route);
            return chapter;
        }

        // ------------------------------------------------------------ encyclopedia and wardrobe

        private static EncyclopediaEntryDefinition Entry(string id, string category, string title, string look, string icon, string text, string note, bool open = false) =>
            new()
            {
                id = id, category = category, title = title, look = look, icon = icon, openFromStart = open,
                lockedDescription = "Ойын барысында ашылады.", unlockedDescription = text,
                status = ContentStatus.Draft, sourceNote = Check + note
            };

        private static List<EncyclopediaEntryDefinition> Entries() => new()
        {
            // ---------------- ТҰЛҒАЛАР
            Entry("kerey_khan", "ТҰЛҒАЛАР", "Керей хан", "kerey", null,
                "Қазақ хандығының негізін Жәнібекпен бірге қалаған алғашқы хан. Ел оның тұсында Шу бойы мен Қозыбасы өңіріне қоныстанды.",
                "годы правления Керея, роль Жәнібека, Қозыбасы"),
            Entry("zhanibek_khan", "ТҰЛҒАЛАР", "Жәнібек хан", "abylai", null,
                "Керейдің серігі, хандықтың екінші негізін қалаушы. Екеуі бір мезгілде ел басқарған деп айтылады.",
                "происхождение Жәнібека, совместное правление", true),
            Entry("abylai_khan", "ТҰЛҒАЛАР", "Абылай хан", "abylai", null,
                "XVIII ғасырдағы қазақ ханы, қолбасшы әрі дипломат. Жоңғар, Цин және Ресей арасында елдің дербестігін сақтауға тырысты. 1771 жылы Түркістанда хан сайланды.",
                "годы жизни (1711–1781), избрание в 1771 г. в Туркестане"),
            Entry("kabanbay_batyr", "ТҰЛҒАЛАР", "Қабанбай батыр", "kabanbay", null,
                "XVIII ғасырдағы атақты батыр, қолбасшы. Жоңғар шапқыншылығына қарсы соғыстарда қол бастады. Халық оны «Дарабоз» деп атаған.",
                "годы жизни, прозвище «Дарабоз», участие в сражениях"),
            Entry("bogenbay_batyr", "ТҰЛҒАЛАР", "Бөгенбай батыр", "warrior", null,
                "Қанжығалы Бөгенбай — XVIII ғасырдағы қолбасшы батыр. Аңырақай шайқасына қатысқан деп айтылады.",
                "годы жизни, участие в Аңырақай"),
            Entry("bi_institute", "ТҰЛҒАЛАР", "Би және билік", "bi", null,
                "Би — дауды шешетін, ел ішінде әділдік айтатын кісі. Оның беделі байлықпен емес, сөзімен және әділдігімен өлшенген.",
                "институт биев, судебная практика", true),
            Entry("aksakal", "ТҰЛҒАЛАР", "Ақсақал", "elder", null,
                "Ауылдың ең үлкені әрі ақылшысы. Дау-дамай, көш, той — бәрі ақсақалдың батасымен басталған.",
                "роль старейшин в общине", true),
            Entry("kazakh_woman", "ТҰЛҒАЛАР", "Қазақ әйелі", "woman", null,
                "Үй шаруасы, бала тәрбиесі, киіз үйдің жиналуы мен тігілуі — көш шаруашылығының көбі әйел қолында болған.",
                "хозяйственная роль женщины в кочевье", true),

            // ---------------- ОҚИҒАЛАР
            Entry("kazakh_khanate", "ОҚИҒАЛАР", "Қазақ хандығы", null, "banner",
                "XV ғасырдың ортасында Керей мен Жәнібек сұлтандар елін Жетісудың батысына бастап көшті. Осы көштен кейін, 1465 жылы, Қазақ хандығы құрылды.",
                "дата основания (в источниках 1456/1465), маршрут откочёвки"),
            Entry("kosh", "ОҚИҒАЛАР", "Көш", null, "horse",
                "Көш — ауылдың жайлауға не қыстауға көшуі. Киіз үй жығылып, түйеге артылады; алдыңғы легте қарттар мен балалар жүреді.",
                "порядок перекочёвки, сезонные маршруты"),
            Entry("dzungar_wars", "ОҚИҒАЛАР", "Жоңғар шапқыншылығы", null, "swords",
                "1723 жылғы жойқын шабуыл халық жадында «Ақтабан шұбырынды» деген атпен қалды. Кейін Бұланты мен Аңырақай шайқастарында қазақ жасақтары жеңіске жетті.",
                "даты: 1723, Бұланты (1727/1728), Аңырақай (1729/1730)"),
            Entry("aktaban", "ОҚИҒАЛАР", "Ақтабан шұбырынды", null, "swords",
                "Ауыр жылдардың халықтық атауы: ел жаппай босып, мал мен жайылымнан айырылған. Бұл кезең жыр мен күйде де сақталған.",
                "происхождение выражения, хронология"),
            Entry("anyrakay", "ОҚИҒАЛАР", "Аңырақай шайқасы", null, "swords",
                "Балқаштың оңтүстігіндегі даладағы ірі шайқас. Үш жүздің жасағы бірігіп қимылдағаны айтылады.",
                "дата (1729/1730), место, состав войск"),
            Entry("turkistan", "ОҚИҒАЛАР", "Түркістан", null, "yurt",
                "Сырдария бойындағы ежелгі қала. Хандық тұсында саяси және рухани орталық саналған; мұнда жиындар өткен.",
                "статус Туркестана как столицы, Қожа Ахмет Ясауи кесенесі"),
            Entry("zhekpe_zhek", "ОҚИҒАЛАР", "Жекпе-жек", null, "naiza",
                "Үлкен шайқас алдында екі жақтың батыры жекпе-жекке шығатын дәстүр. Жеңіс жасақтың рухын көтерген.",
                "традиция поединков перед сражением"),
            Entry("khan_saylau", "ОҚИҒАЛАР", "Хан сайлау", null, "banner",
                "Жаңа ханды ел өкілдері сайлап, ақ киізге көтерген. Салт-жораның бір бөлігі — ханның киімін жұртқа үлестіру.",
                "обряд поднятия на белой кошме"),

            // ---------------- ЖЕР-СУ
            Entry("saryarka", "ЖЕР-СУ", "Сарыарқа", null, "horse",
                "Орталық Қазақстанның кең даласы: ұсақ шоқылар, өзен аңғарлары, жазда шүйгін жайылым.",
                "границы Сарыарки, рельеф", true),
            Entry("zhetysu", "ЖЕР-СУ", "Жетісу", null, "yurt",
                "Жеті өзеннің өлкесі. Тау бөктері мен өзен аңғарлары малға да, егінге де қолайлы болған.",
                "какие именно семь рек", true),
            Entry("syrdariya", "ЖЕР-СУ", "Сырдария", null, "banner",
                "Оңтүстіктің басты өзені. Бойында Сығанақ, Отырар, Түркістан сияқты қалалар тұрған.",
                "города на Сырдарье", true),
            Entry("balkash", "ЖЕР-СУ", "Балқаш", null, "yurt",
                "Батыс жағы тұщы, шығыс жағы тұзды деп айтылатын үлкен көл. Жағасы қыстауға ыңғайлы болған.",
                "гидрология Балхаша", true),
            Entry("ulytau", "ЖЕР-СУ", "Ұлытау", null, "swords",
                "Сарыарқаның ортасындағы таулы өлке. Көне қорымдар мен кесенелер осында сақталған.",
                "памятники Улытау"),
            Entry("karatau", "ЖЕР-СУ", "Қаратау", null, "horse",
                "Сырдария мен даланы бөліп тұрған таулы жота. Көш жолдары осы жотаның асуларымен өткен.",
                "перевалы и маршруты через Каратау"),
            Entry("zhaylau", "ЖЕР-СУ", "Жайлау мен қыстау", null, "yurt",
                "Жазда биік жайлау, қыста ықтасын қыстау. Мал шаруашылығының жылдық айналымы осы екі мекенге негізделген.",
                "сезонный цикл кочевья", true),

            // ---------------- МӘДЕНИЕТ
            Entry("kiiz_ui", "МӘДЕНИЕТ", "Киіз үй", null, "yurt",
                "Кереге, уық және шаңырақтан құралатын көшпелі баспана. Тез жиналады, жылы әрі желге төзімді.",
                "конструкция и части юрты", true),
            Entry("zhuzder", "МӘДЕНИЕТ", "Үш жүз", null, "zhuz",
                "Қазақ халқы дәстүрлі түрде Ұлы, Орта және Кіші жүз болып бөлінеді. Әр жүздің өз аймағы мен руларының тізбегі бар.",
                "происхождение и территория жузов", true),
            Entry("dombyra", "МӘДЕНИЕТ", "Домбыра", null, "dombra",
                "Екі ішекті ұлттық аспап. Күй арқылы оқиға да, көңіл-күй де айтылады.",
                "устройство домбры, жанр күй", true),
            Entry("shapan", "МӘДЕНИЕТ", "Шапан", null, "shapan",
                "Ұзын, кең етекті сырт киім. Құрметті қонаққа шапан жабу — ізет пен сыйластықтың белгісі.",
                "крой и обычай «шапан жабу»", true),
            Entry("sauyt", "МӘДЕНИЕТ", "Сауыт", null, "sauyt",
                "Батырдың темір не былғары сауыты. Дулыға, шарайна және белбеумен толықтырылған.",
                "виды доспеха и их названия"),
            Entry("dastarkhan", "МӘДЕНИЕТ", "Дастарқан", null, "dastarkhan",
                "Қонақ келгенде жайылатын дастарқан. Ет асу, шай құю, бата беру — бәрінің өз реті бар.",
                "этикет дастархана", true),
            Entry("asyk", "МӘДЕНИЕТ", "Асық ату", null, "asyk",
                "Қойдың асығымен ойналатын ұлттық ойын. Ойыншы сақамен атып, сызықтағы асықтарды ұтып алады.",
                "правила игры и название «сақа»"),
            Entry("baige", "МӘДЕНИЕТ", "Бәйге", null, "horse",
                "Ат жарысы. Қашықтығы бірнеше шақырымға созылатын бәйгеге ауыл-ауылдан ат қосылған, жеңімпаз атқа да, баптаушысына да абырой әкелген.",
                "дистанции и правила байге"),
            Entry("sokyr_teke", "МӘДЕНИЕТ", "Соқыр теке", null, "blindfold",
                "Балалар ойыны. Көзі байланған «теке» дыбысқа қарап басқаларды ұстауға тырысады. Ұсталған ойыншы келесі «теке» болады.",
                "правила игры")
        };

        private static OutfitItemDefinition Outfit(string id, string slot, string title, string text, params string[] looks) =>
            new() { id = id, icon = id, slot = slot, title = title, description = text, looks = looks, status = ContentStatus.Draft, sourceNote = Check + "описание одежды" };

        private static List<OutfitItemDefinition> Outfits() => new()
        {
            Outfit("shapan", "СЫРТ КИІМ", "Шапан", "Ұзын, кең етекті сырт киім. Мерекеде де, күнделікті де киілген. Құрметті қонаққа шапан жабу — сый белгісі.",
                "kerey", "abylai", "bi", "elder", "kabanbay"),
            Outfit("borik", "БАС КИІМ", "Бөрік", "Жиегі аң терісімен көмкерілген жылы бас киім. Төбесі мақпалмен не шұғамен тігіледі.",
                "kerey", "abylai"),
            Outfit("tymak", "БАС КИІМ", "Тымақ", "Қыстың сақ желінен қорғайтын, құлақшыны бар терi бас киім.",
                "elder", "resident", "envoy"),
            Outfit("kimeshek", "ӘЙЕЛ БАС КИІМІ", "Кимешек", "Тұрмыс құрған әйелдің ақ матадан тігілген бас киімі. Кестесі мен пішіні өңірге қарай өзгереді.",
                "woman"),
            Outfit("koilek", "КӨЙЛЕК", "Көйлек", "Етегі кең, жеңі ұзын әйел көйлегі. Мерекелік көйлектің етегі оюмен көмкеріледі.",
                "woman"),
            Outfit("kamzol", "ЖЕҢСІЗ КИІМ", "Камзол", "Көйлектің сыртынан киілетін, беліне қонымды жеңсіз киім. Жиегі өрнекпен әшекейленеді.",
                "woman", "abylai"),
            Outfit("takiya", "БАС КИІМ", "Тақия", "Жеңіл, жұмсақ бас киім. Бала мен жастың тақиясына үкі тағылған.",
                "child", "child_girl"),
            Outfit("sauyt", "САУЫТ", "Сауыт", "Темір тақтайшалардан құралған батыр сауыты. Соққыны бөліп, салмағын иыққа таратады.",
                "kabanbay", "warrior"),
            Outfit("dulyga", "БАС КИІМ", "Дулыға", "Ұрысқа киілетін темір бас киім. Желкені жауып тұратын жақтаулары болады.",
                "kabanbay", "warrior"),
            Outfit("naiza", "ҚАРУ", "Найза", "Атты жауынгердің негізгі қаруы. Ұшына ту не шашақ байланған.",
                "kabanbay", "warrior"),
            Outfit("etik", "АЯҚ КИІМ", "Етік", "Былғарыдан тігілген биік қонышты аяқ киім. Атқа мінуге ыңғайлы.",
                "kerey", "abylai", "kabanbay", "woman", "bi", "elder", "warrior", "child"),
            Outfit("belbeu", "БЕЛДІК", "Белбеу", "Шапан мен көйлекті буатын белдік. Оған пышақ пен қалта ілінген.",
                "kerey", "abylai", "kabanbay", "bi", "warrior")
        };

        // ------------------------------------------------------------ roles

        private static void BuildRoles()
        {
            Directory.CreateDirectory(RolesFolder);
            Role("ROLE_KEREY", "Керей хан", "Қазақ хандығының негізін қалаушылардың бірі.", "ХАН • XV Ғ.", "Көш. Ел. Бастау.", "kerey", "CH_KEREY", "card_kerey");
            Role("ROLE_ABYLAI", "Абылай хан", "Үш жүздің басын қосқан хан және дипломат.", "ХАН • XVIII Ғ.", "Бірлік. Дипломатия. Ел.", "abylai", "CH_ABYLAI", "card_abylai");
            Role("ROLE_KABANBAY", "Қабанбай батыр", "Жоңғар шапқыншылығына қарсы қол бастаған батыр.", "БАТЫР • XVIII Ғ.", "Ерлік. Қорғаныс. Ел.", "kabanbay", "CH_KABANBAY", "card_kabanbay");
        }

        private static void Role(string id, string name, string description, string era, string tagline, string look, string chapter, string card)
        {
            string path = RolesFolder + id + ".asset";
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(path);
            if (hero == null) { hero = ScriptableObject.CreateInstance<HeroDefinition>(); AssetDatabase.CreateAsset(hero, path); }
            var serialized = new SerializedObject(hero);
            serialized.FindProperty("heroId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = name;
            serialized.FindProperty("description").stringValue = description;
            serialized.FindProperty("era").stringValue = era;
            serialized.FindProperty("tagline").stringValue = tagline;
            serialized.FindProperty("visualSetId").stringValue = look;
            serialized.FindProperty("chapterId").stringValue = chapter;
            serialized.FindProperty("portrait").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ATTILA/Art/Generated/Cards/{card}.png");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
