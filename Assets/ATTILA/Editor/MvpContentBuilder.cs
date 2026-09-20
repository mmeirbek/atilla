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

        private static NpcPlacement At(string npcId, float x, float z, FacingHint facing = FacingHint.South) =>
            new() { npcId = npcId, position = new Vector2(x, z), facing = facing };

        private static NpcPlacement Figure(string look, float x, float z, FacingHint facing = FacingHint.South) =>
            new() { look = look, position = new Vector2(x, z), facing = facing };

        private static BeatDefinition Beat(string npc, string objective, string dialogue, int xp, string checkpoint, BeatAction action = BeatAction.None, params string[] unlock) =>
            new() { npcId = npc, objective = objective, dialogueId = dialogue, xp = xp, checkpoint = checkpoint, action = action, unlockEntryIds = unlock };

        private static CampfireTopic Topic(string label, string dialogue, string entry = null) => new() { label = label, dialogueId = dialogue, unlockEntryId = entry };

        private static List<NpcDefinition> Npcs() => new()
        {
            new NpcDefinition { id = "NPC_BI", displayName = "Би", look = "bi", interactionRadius = 2.3f, idleDialogueId = "DIA_IDLE_BI" },
            new NpcDefinition { id = "NPC_AISHA", displayName = "Айша", look = "woman", interactionRadius = 2.2f, idleDialogueId = "DIA_IDLE_AISHA" },
            new NpcDefinition { id = "NPC_CHILDREN", displayName = "Балалар", look = "child", interactionRadius = 2.4f, idleDialogueId = "DIA_IDLE_CHILDREN" },
            new NpcDefinition { id = "NPC_ELDER", displayName = "Ақсақал", look = "elder", interactionRadius = 2.4f, idleDialogueId = "DIA_IDLE_ELDER" },
            new NpcDefinition { id = "NPC_HERDER", displayName = "Малшы", look = "resident", interactionRadius = 2.0f, idleDialogueId = "DIA_IDLE_HERDER" },
            new NpcDefinition { id = "NPC_ENVOY_WEST", displayName = "Кіші жүз елшісі", look = "envoy", interactionRadius = 2.3f, idleDialogueId = "DIA_IDLE_ENVOY_WEST" },
            new NpcDefinition { id = "NPC_ENVOY_SOUTH", displayName = "Ұлы жүз елшісі", look = "envoy2", interactionRadius = 2.3f, idleDialogueId = "DIA_IDLE_ENVOY_SOUTH" },
            new NpcDefinition { id = "NPC_COMMANDER", displayName = "Жүзбасы", look = "warrior", interactionRadius = 2.3f, idleDialogueId = "DIA_IDLE_COMMANDER" },
            new NpcDefinition { id = "NPC_SMITH", displayName = "Ұста", look = "resident", interactionRadius = 2.2f, idleDialogueId = "DIA_IDLE_SMITH" },
            new NpcDefinition { id = "NPC_SCOUT", displayName = "Барлаушы", look = "envoy", interactionRadius = 2.3f, idleDialogueId = "DIA_IDLE_SCOUT" }
        };

        private static void IdleDialogues()
        {
            Say("DIA_IDLE_BI", "Би", "bi", "Асықпа. Әр істің өз уақыты бар.");
            Say("DIA_IDLE_AISHA", "Айша", "woman", "Ауылда шаруа көп. Кейінірек келіңіз.");
            Say("DIA_IDLE_CHILDREN", "Балалар", "child", "Біз ойнап жүрміз! Кейін бізге қосылыңыз.");
            Say("DIA_IDLE_ELDER", "Ақсақал", "elder", "Кешке от басында әңгіме айтамын.");
            Say("DIA_IDLE_HERDER", "Малшы", "resident", "Жылқылар жайылымнан оралды. Бәрі аман.");
            Say("DIA_IDLE_ENVOY_WEST", "Кіші жүз елшісі", "envoy", "Шешіміңізді күтеміз.");
            Say("DIA_IDLE_ENVOY_SOUTH", "Ұлы жүз елшісі", "envoy2", "Ұлы жүз сізге сәлем айтты.");
            Say("DIA_IDLE_COMMANDER", "Жүзбасы", "warrior", "Жасақ сапта тұр, батыр.");
            Say("DIA_IDLE_SMITH", "Ұста", "resident", "Балғам тынбайды — қару-жарақ көп.");
            Say("DIA_IDLE_SCOUT", "Барлаушы", "envoy", "Жау жақын. Сақ болыңыз.");
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
                "Киіз үйді жығып, түйеге артамыз. Кереге, уық, шаңырақ — бәрі өз орнында.",
                "Көш жолы ұзақ. Бала-шаға мен қарттар алдыңғы легте жүрсін.");
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
            Say("DIA_K_T3", "Ақсақал", "elder",
                "Соқыр теке — баланың құлағын, сезімін шыңдайтын ойын.",
                "Көзі байлаулы бала дыбысқа қарап жол табады.");
            History(Say("DIA_K_ROUTE", "Би", "bi", "Жүк артылды, ел жиналды. Көш бағыты — Шу бойы, Қозыбасы."), "Қозыбасы как место откочёвки");

            var chapter = new ChapterDefinition
            {
                id = "CH_KEREY", roleId = "ROLE_KEREY", title = "I тарау · Көш", subtitle = "XV ғасыр",
                finaleTitle = "I ТАРАУ АЯҚТАЛДЫ",
                finaleText = "Көш Жетісуға аман жетті. Жаңа қонысқа орныққан ел кейін Қазақ хандығы деген атпен тарихта қалды.",
                finaleAltText = "Көш Жетісуға аман жетті. Елдің ойын тыңдаған сұлтанға халық сенді — жаңа қоныста Қазақ хандығының іргетасы қаланды.",
                finaleAltMinUnity = 20,
                worldTint = new Color(1f, .93f, .82f), skyColor = new Color(.93f, .78f, .62f), banner = "red"
            };
            chapter.npcs.AddRange(new[]
            {
                At("NPC_BI", -4f, 6f), At("NPC_AISHA", -8f, 4.5f, FacingHint.East), At("NPC_CHILDREN", 5f, 7f), At("NPC_ELDER", -5.6f, -1.2f, FacingHint.East),
                At("NPC_HERDER", 10.5f, -1.5f, FacingHint.West), Figure("child_girl", 6.3f, 6.2f, FacingHint.West), Figure("woman", -10f, 12.4f), Figure("resident", 8f, 12f, FacingHint.West)
            });
            chapter.beats.Add(Beat("NPC_BI", "Бимен кеңес", "DIA_K_BI", 50, "KEREY_COUNCIL"));
            chapter.beats.Add(Beat("NPC_AISHA", "Айшадан көш дайындығын біл", "DIA_K_AISHA", 30, "KEREY_PREPARE"));
            chapter.beats.Add(Beat("NPC_CHILDREN", "Балалармен «Соқыр теке» ойна", "DIA_K_CHILDREN", 100, "KEREY_GAME", BeatAction.SokyrTeke, "sokyr_teke"));
            var night = Beat("NPC_ELDER", "Кешкі от басына бар", "DIA_K_ELDER", 50, "KEREY_NIGHT", BeatAction.Campfire);
            night.campfire = new CampfireDefinition { title = "Көш алдындағы түн" };
            night.campfire.topics.AddRange(new[] { Topic("Ел неге көшеді?", "DIA_K_T1", "kazakh_khanate"), Topic("Жетісу жері", "DIA_K_T2"), Topic("Бабалар ойыны", "DIA_K_T3", "sokyr_teke") });
            chapter.beats.Add(night);
            var route = Beat("NPC_BI", "Биге көшке белгі бер", "DIA_K_ROUTE", 100, "KEREY_COMPLETE", BeatAction.Map, "kerey_khan", "kazakh_khanate");
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
            Say("DIA_A_CHILDREN", "Балалар", "child",
                "Сұлтан аға, сақаңызды алыңыз! Асық атып көрейік.",
                "Үш рет атасыз. Екі асықты шеңберден шығарсаңыз — жеңіс сіздікі!");
            Say("DIA_A_ELDER", "Ақсақал", "elder", "Ертең ұзақ жол. Бүгін от басында отырып, бабалар сөзін тыңдайық.");
            History(Say("DIA_A_T1", "Ақсақал", "elder",
                "1723 жылы жоңғарлар кенеттен шабуыл жасады. Ел ауыр күйге түсті.",
                "Халық ол жылдарды «Ақтабан шұбырынды» деп атады."), "1723 год и название «Ақтабан шұбырынды»");
            History(Say("DIA_A_T2", "Ақсақал", "elder",
                "Құрметті кісіге шапан жабу — ізет пен сыйдың белгісі.",
                "Шапанның сәні де, жылуы да — шеберінің қолында."), "обычай «шапан жабу»");
            Say("DIA_A_T3", "Ақсақал", "elder",
                "Асық — ауыл баласының сүйікті ойыны.",
                "Сақаны дәл ату үшін көз де, шыдам да керек.");
            History(Say("DIA_A_ROUTE", "Би", "bi", "Үш жүздің өкілдері Түркістанға жиналуда. Жолға шығатын уақыт жетті."), "место избрания — Түркістан");

            var chapter = new ChapterDefinition
            {
                id = "CH_ABYLAI", roleId = "ROLE_ABYLAI", title = "II тарау · Бірлік", subtitle = "XVIII ғасыр",
                finaleTitle = "II ТАРАУ АЯҚТАЛДЫ",
                finaleText = "Түркістанда ел жиналды. Дәстүр бойынша Абылайды ақ киізге көтеріп, хан сайлады.",
                finaleAltText = "Түркістанда үш жүздің өкілдері бір дастарқанға жиналды. Бірлікті таңдаған Абылайды ел ақ киізге көтеріп, хан сайлады.",
                finaleAltMinUnity = 60,
                worldTint = new Color(1f, 1f, .96f), skyColor = new Color(.66f, .82f, .92f), banner = "blue"
            };
            chapter.npcs.AddRange(new[]
            {
                At("NPC_BI", -4f, 6f), At("NPC_ENVOY_WEST", -8.5f, 3.5f, FacingHint.East), At("NPC_ENVOY_SOUTH", 8.4f, 3.2f, FacingHint.West),
                At("NPC_CHILDREN", 5f, 7f), At("NPC_ELDER", -5.6f, -1.2f, FacingHint.East), At("NPC_HERDER", 10.5f, -1.5f, FacingHint.West),
                Figure("child_girl", 6.3f, 6.2f, FacingHint.West), Figure("warrior", 2.6f, 12.4f), Figure("warrior", -2.6f, 12.4f), Figure("woman", -10f, 12.4f)
            });
            chapter.beats.Add(Beat("NPC_BI", "Бимен кеңес", "DIA_A_BI", 50, "ABYLAI_COUNCIL"));
            chapter.beats.Add(Beat("NPC_ENVOY_WEST", "Кіші жүз елшісін тыңда", "DIA_A_ENVOY_W", 40, "ABYLAI_WEST"));
            chapter.beats.Add(Beat("NPC_ENVOY_SOUTH", "Ұлы жүз елшісімен сөйлес", "DIA_A_ENVOY_S", 40, "ABYLAI_SOUTH"));
            chapter.beats.Add(Beat("NPC_CHILDREN", "Балалармен асық ат", "DIA_A_CHILDREN", 100, "ABYLAI_GAME", BeatAction.Asyk, "asyk"));
            var night = Beat("NPC_ELDER", "Кешкі от басына бар", "DIA_A_ELDER", 50, "ABYLAI_NIGHT", BeatAction.Campfire);
            night.campfire = new CampfireDefinition { title = "Түркістан алдындағы түн" };
            night.campfire.topics.AddRange(new[] { Topic("Жоңғар заманы", "DIA_A_T1", "dzungar_wars"), Topic("Шапан — құрмет белгісі", "DIA_A_T2", "shapan"), Topic("Асық ойыны", "DIA_A_T3", "asyk") });
            chapter.beats.Add(night);
            var route = Beat("NPC_BI", "Түркістанға аттан", "DIA_A_ROUTE", 100, "ABYLAI_COMPLETE", BeatAction.Map, "abylai_khan");
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
            council.choices.Add(Choice("Асуды бекітіңдер", "DIA_B_FORTIFY", "fortify", 20));
            council.choices.Add(Choice("Тосқауыл қойып, жасырынып күтеміз", "DIA_B_AMBUSH", "ambush", 20));
            Say("DIA_B_FORTIFY", "Жүзбасы", "warrior", "Құп болады! Тас қалап, асуды бекітеміз. Алдымен ұстаға соғып, қаруыңызды алыңыз.");
            Say("DIA_B_AMBUSH", "Жүзбасы", "warrior", "Құп болады! Сайдың бойына жасырынамыз. Алдымен ұстаға соғып, қаруыңызды алыңыз.");
            Say("DIA_B_SMITH", "Ұста", "resident",
                "Сауытыңыз жөнделді, найзаңыз қайралды.",
                "Мына шапанды да алыңыз — таулы жерде түн суық.");
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
            History(Say("DIA_B_T3", "Ақсақал", "elder",
                "Жеңіспен оралған батырға шапан жабады.",
                "Бұл — ел алғысының белгісі."), "обычай «шапан жабу» для батыра");
            History(Say("DIA_B_ROUTE", "Жүзбасы", "warrior", "Жасақтар бірігіп жатыр. Келесі жиын — Аңырақай даласында."), "место сбора — Аңырақай");

            var chapter = new ChapterDefinition
            {
                id = "CH_KABANBAY", roleId = "ROLE_KABANBAY", title = "III тарау · Қорғаныс", subtitle = "XVIII ғасыр",
                finaleTitle = "III ТАРАУ АЯҚТАЛДЫ",
                finaleText = "Біріккен қазақ жасақтары жауға тойтарыс берді. Аңырақай даласындағы жеңіс халық жадында сақталды.",
                worldTint = new Color(.94f, .84f, .86f), skyColor = new Color(.62f, .52f, .66f), banner = "dark"
            };
            chapter.npcs.AddRange(new[]
            {
                At("NPC_COMMANDER", -4f, 6f), At("NPC_SMITH", 8.4f, 3.2f, FacingHint.West), At("NPC_AISHA", -8f, 4.5f, FacingHint.East),
                At("NPC_SCOUT", 5f, 7f), At("NPC_ELDER", -5.6f, -1.2f, FacingHint.East),
                Figure("warrior", 2.6f, 12.4f), Figure("warrior", -2.6f, 12.4f), Figure("warrior", 3.4f, 9.6f, FacingHint.West), Figure("resident", 10.8f, -2f, FacingHint.West)
            });
            chapter.beats.Add(Beat("NPC_COMMANDER", "Әскери кеңеске қатыс", "DIA_B_COUNCIL", 50, "KABANBAY_COUNCIL"));
            chapter.beats.Add(Beat("NPC_SMITH", "Ұстадан қару-жарақ ал", "DIA_B_SMITH", 30, "KABANBAY_SMITH", BeatAction.None, "shapan"));
            chapter.beats.Add(Beat("NPC_AISHA", "Ауылдың аман екенін біл", "DIA_B_AISHA", 30, "KABANBAY_AUL"));
            chapter.beats.Add(Beat("NPC_SCOUT", "Жекпе-жекке шық", "DIA_B_SCOUT", 150, "KABANBAY_DUEL", BeatAction.Duel, "dzungar_wars"));
            var night = Beat("NPC_ELDER", "Жеңіс түні: от басына бар", "DIA_B_ELDER", 50, "KABANBAY_NIGHT", BeatAction.Campfire);
            night.campfire = new CampfireDefinition { title = "Жеңіс түні" };
            night.campfire.topics.AddRange(new[] { Topic("Соғыстар мен шайқастар", "DIA_B_T1", "dzungar_wars"), Topic("Батыр кім?", "DIA_B_T2"), Topic("Бабалар дәстүрі", "DIA_B_T3", "shapan") });
            chapter.beats.Add(night);
            var route = Beat("NPC_COMMANDER", "Аңырақайға аттан", "DIA_B_ROUTE", 100, "KABANBAY_COMPLETE", BeatAction.Map, "kabanbay_batyr");
            route.map = new MapDefinition { title = "Аңырақайға жол", goal = "Біріккен қолға қосылу", startLabel = "Сарыарқа", endLabel = "Аңырақай",
                route = new[] { new Vector2(.56f, .76f), new Vector2(.52f, .62f), new Vector2(.56f, .48f), new Vector2(.63f, .37f) } };
            chapter.beats.Add(route);
            return chapter;
        }

        // ------------------------------------------------------------ encyclopedia and wardrobe

        private static EncyclopediaEntryDefinition Entry(string id, string category, string title, string look, string icon, string locked, string text, string note) =>
            new() { id = id, category = category, title = title, look = look, icon = icon, lockedDescription = locked, unlockedDescription = text, status = ContentStatus.Draft, sourceNote = Check + note };

        private static List<EncyclopediaEntryDefinition> Entries() => new()
        {
            Entry("kerey_khan", "ТҰЛҒАЛАР", "Керей хан", "kerey", null, "«Көш» тарауында ашылады.",
                "Қазақ хандығының негізін Жәнібекпен бірге қалаған алғашқы хан. Ел оның тұсында Шу бойы мен Қозыбасы өңіріне қоныстанды.",
                "годы правления Керея, роль Жәнібека, Қозыбасы"),
            Entry("abylai_khan", "ТҰЛҒАЛАР", "Абылай хан", "abylai", null, "«Бірлік» тарауында ашылады.",
                "XVIII ғасырдағы қазақ ханы, қолбасшы және дипломат. Жоңғар, Цин және Ресей арасында елдің дербестігін сақтауға тырысты. 1771 жылы Түркістанда хан сайланды.",
                "годы жизни (1711–1781), избрание в 1771 г. в Туркестане"),
            Entry("kabanbay_batyr", "ТҰЛҒАЛАР", "Қабанбай батыр", "kabanbay", null, "«Қорғаныс» тарауында ашылады.",
                "XVIII ғасырдағы атақты батыр, қолбасшы. Жоңғар шапқыншылығына қарсы соғыстарда қол бастады. Халық оны «Дарабоз» деп атаған.",
                "годы жизни, прозвище «Дарабоз», участие в сражениях"),
            Entry("kazakh_khanate", "ОҚИҒАЛАР", "Қазақ хандығы", null, "banner", "«Көш» тарауында ашылады.",
                "XV ғасырдың ортасында Керей мен Жәнібек сұлтандар елін Жетісудың батысына бастап көшті. Осы көштен кейін Қазақ хандығы құрылды. Оқулықтарда көбіне 1465 жыл аталады.",
                "дата основания (в источниках 1456/1465), маршрут откочёвки"),
            Entry("dzungar_wars", "ОҚИҒАЛАР", "Жоңғар шапқыншылығы", null, "swords", "«Бірлік» не «Қорғаныс» тарауында ашылады.",
                "1723 жылғы жойқын шабуыл халық жадында «Ақтабан шұбырынды» деген атпен қалды. Кейін Бұланты мен Аңырақай шайқастарында қазақ жасақтары жеңіске жетті.",
                "даты: 1723, Бұланты (1727/1728), Аңырақай (1729/1730)"),
            Entry("shapan", "МӘДЕНИЕТ", "Шапан", null, "shapan", "Шапан туралы әңгімені тыңда.",
                "Ұзын, кең етекті сырт киім. Құрметті қонаққа шапан жабу — ізет пен сыйластықтың белгісі.",
                "описание кроя и обычая «шапан жабу»"),
            Entry("asyk", "МӘДЕНИЕТ", "Асық ату", null, "asyk", "Асық ойынында ашылады.",
                "Қойдың асығымен ойналатын ұлттық ойын. Ойыншы сақамен атып, сызықтағы асықтарды ұтып алады. Ойын көз мөлшері мен шыдамдылықты шыңдайды.",
                "правила игры и название «сақа»"),
            Entry("sokyr_teke", "МӘДЕНИЕТ", "Соқыр теке", null, "blindfold", "«Соқыр теке» ойынында ашылады.",
                "Балалар ойыны. Көзі байланған «теке» дыбысқа қарап басқаларды ұстауға тырысады. Ұсталған ойыншы келесі «теке» болады.",
                "правила игры")
        };

        private static OutfitItemDefinition Outfit(string id, string slot, string title, string text) =>
            new() { id = id, icon = id, slot = slot, title = title, description = text, status = ContentStatus.Draft, sourceNote = Check + "описание одежды" };

        private static List<OutfitItemDefinition> Outfits() => new()
        {
            Outfit("shapan", "СЫРТ КИІМ", "Шапан", "Ұзын, кең етекті сырт киім. Мерекеде де, күнделікті де киілген. Құрметті қонаққа шапан жабу — сый белгісі."),
            Outfit("borik", "БАС КИІМ", "Бөрік", "Жиегі аң терісімен көмкерілген жылы бас киім. Төбесі мақпалмен не шұғамен тігіледі."),
            Outfit("kimeshek", "ӘЙЕЛ БАС КИІМІ", "Кимешек", "Тұрмыс құрған әйелдің ақ матадан тігілген бас киімі. Кестесі мен пішіні өңірге қарай өзгереді."),
            Outfit("kamzol", "ЖЕҢСІЗ КИІМ", "Камзол", "Көйлектің сыртынан киілетін, беліне қонымды жеңсіз киім. Жиегі өрнекпен әшекейленеді."),
            Outfit("etik", "АЯҚ КИІМ", "Етік", "Былғарыдан тігілген биік қонышты аяқ киім. Атқа мінуге ыңғайлы."),
            Outfit("belbeu", "БЕЛДІК", "Белбеу", "Шапан мен көйлекті буатын белдік. Оған пышақ пен қалта ілінген.")
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
