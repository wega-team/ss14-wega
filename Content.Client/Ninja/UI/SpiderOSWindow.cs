using Content.Shared.Ninja.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using System;
using System.Numerics;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Ninja.UI;

public sealed class SpiderOSWindow : DefaultWindow
{
    // ── Colour palette ────────────────────────────────────────────────────────
    private static readonly Color GhostBg     = Color.FromHex("#0D1B2B");
    private static readonly Color GhostHdr    = Color.FromHex("#1A3050");
    private static readonly Color GhostAccent = Color.FromHex("#6090D8");

    private static readonly Color SnakeBg     = Color.FromHex("#0D1C0D");
    private static readonly Color SnakeHdr    = Color.FromHex("#193519");
    private static readonly Color SnakeAccent = Color.FromHex("#50B050");

    private static readonly Color SteelBg     = Color.FromHex("#1E0D0D");
    private static readonly Color SteelHdr    = Color.FromHex("#351515");
    private static readonly Color SteelAccent = Color.FromHex("#C04040");

    private static readonly Color WinBg       = Color.FromHex("#080E08");
    private static readonly Color HdrBg       = Color.FromHex("#0C1A0C");
    private static readonly Color TealColor   = Color.FromHex("#00FF88");
    private static readonly Color DimGreen    = Color.FromHex("#00AA55");
    private static readonly Color FieldLabel  = Color.FromHex("#88BB88");
    private static readonly Color SelBorder   = Color.White;
    private static readonly Color UnselBorder = Color.FromHex("#1E2A1E");

    private static readonly Color[] ColBg     = [GhostBg,     SnakeBg,     SteelBg    ];
    private static readonly Color[] ColHdr    = [GhostHdr,    SnakeHdr,    SteelHdr   ];
    private static readonly Color[] ColAccent = [GhostAccent, SnakeAccent, SteelAccent];
    private static readonly string[] ColNames = ["Призрак",   "Змей",      "Сталь"    ];
    private static readonly string[] ColDescs =
    [
        "Скрывайтесь среди врагов,\nнападайте из тени и будьте\nнезримой угрозой, всё для того\nчтобы о вас никто не узнал!\nБудьте незаметны как призрак!",
        "Удивляйте! Трюки, ловушки, щиты.\nПокажите им, что такое бой\nс настоящим убийцем.\nИзвивайтесь и изворачивайтесь\nнаходя выход из любой ситуации.\nВраги лишь грызуны,\nчьё логово навестил змей!",
        "Ярость не доступная обычным людям.\nСила, скорость и орудия\nвыше их понимания.\nРазите их как хищник что разит\nсвою добычу.\nПокажите им холодный вкус стали!",
    ];

    // ── Ability icon states (all from ninja.rsi) ──────────────────────────────
    private static readonly string[,] IconStates =
    {
        { "smoke",            "kunai",           "shuriken"        }, // row 0
        { "ninja_cloak",      "chem_injector",   "adrenal"         }, // row 1
        { "ninja_clones",     "emergency_blink", "emp"             }, // row 2
        { "chameleon",        "caltrop",         "energynet"       }, // row 3
        { "ninja_spirit_form","cloning",         "spider_red"      }, // row 4
    };

    // ── Ability names shown in tooltips ───────────────────────────────────────
    private static readonly string[,] AbilityNames =
    {
        { "Дымовая завеса",        "Встроенное джохьё",       "Энергетические сюрикены" }, // row 0
        { "Невидимость",           "Исцеляющий коктейль",     "Всплеск адреналина"      }, // row 1
        { "Энергетические клоны",  "Экстренная телепортация", "Электромагнитный взрыв"  }, // row 2
        { "Хамелеон",              "Электро-чеснок",          "Энергетическая сеть"     }, // row 3
        { "Форма духа",            "Второй шанс",             "Боевое искусство вдовы"  }, // row 4
    };

    // ── Ability descriptions shown in tooltips ────────────────────────────────
    private static readonly string[,] AbilityDescs =
    {
        {
            "Вы создаёте большое облако дыма чтобы запутать своих врагов.\nЭта способность отлично сочетается с вашим визором в режиме термального сканера.\nА так же автоматически применяется многими другими модулями если вы того пожелаете.\nСтоимость активации: 95 ед. энергии.\nПерезарядка: 10 секунд.",
            "Так же известно как Шэнбяо или просто Кинжал на цепи.\nИнтегрированное в костюм устройство запуска позволит вам поймать и притянуть к себе жертву за доли секунды.\nОружие не очень годится для долгих боёв, но отлично подходит для вытягивания одной жертвы — на расстояние удара!\nГлавное не промахиваться при стрельбе.\nСтоимость выстрела: 20 ед. энергии.\nПерезарядка: 5 секунд.",
            "Активирует пусковое устройство скрытое в перчатках костюма.\nУстройство выпускает по три сюрикена, сделанных из сжатой энергии, очередью.\nСюрикены постепенно изнуряют врагов и наносят слабый ожоговый урон.\nТак же они пролетают через стекло, как и обычные лазерные снаряды.\nСтоимость выстрела: 14 ед. энергии."
        },
        {
            "Вы формируете вокруг себя маскировочное поле скрывающее вас из виду и приглушающее ваши шаги.\nПоле довольно хрупкое и может разлететься от любого резкого действия или удара.\nАктивация поля занимает 2 секунды. Хоть поле и скрывает вас полностью, настоящий убийца должен быть хладнокровен.\nНе стоит недооценивать внимательность других людей.\nАктивная невидимость слабо увеличивает пассивный расход энергии.\nПерезарядка: 15 секунд.",
            "Вводит в вас экспериментальную лечебную смесь. Способную залечить даже сломанные кости и оторванные конечности.\nПрепарат вызывает пространственно-временные парадоксы и очень медленно выводится из организма!\nПри передозировке они становятся слишком опасны для пользователя. Не вводите больше 30 ед. препарата в ваш организм!\nВместо траты энергии имеет 3 заряда. Их можно восстановить вручную с помощью цельных кусков блюспейс кристаллов помещённых в костюм.",
            "Мгновенно вводит в вас мощную экспериментальную сыворотку ускоряющую вас в бою и помогающую быстрее оклематься от оглушающих эффектов.\nКостюм производит сыворотку с использованием урана. Что к сожалению даёт неприятный негативный эффект, в виде накопления радия в организме пользователя.\nВместо траты энергии может быть использовано лишь один раз, пока не будет перезаряжено вручную с помощью цельных кусков урана помещённых в костюм."
        },
        {
            "Создаёт двух клонов готовых помочь в битве и дезориентировать противника.\nТак же в процессе смещает вас и ваших клонов в случайном направлении в радиусе пары метров.\nПользуйтесь осторожно. Случайное смещение может запереть вас за 4-мя стенами. Будьте к этому готовы.\nКлоны существуют примерно 20 секунд. Клоны имеют шанс размножиться атакуя противников.\nСтоимость активации: 300 ед. энергии.\nПерезарядка: 25 секунд.",
            "При использовании мгновенно телепортирует пользователя в случайную зону в радиусе около 4 метров.\nМожно использовать без сознания.\nСтоимость активации: 150 ед. энергии.\nПерезарядка: 10 секунд.",
            "Электромагнитные волны выключают, подрывают или иначе повреждают — киборгов, дронов, КПБ, энергетическое оружие, портативные светошумовые устройства, устройства связи и т.д.\nЭтот взрыв может как помочь вам в бою, так и невероятно навредить. Внимательно осматривайте местность перед применением.\nНе забывайте о защищающем от света режиме вашего визора. Он может помочь не ослепнуть при подрыве подобных устройств.\nВзрыв прерывает пассивные эффекты наложенные на вас. Например невидимость.\nСтоимость активации: 180 ед. энергии."
        },
        {
            "Вы формируете вокруг себя голографическое поле искажающее визуальное и слуховое восприятие других существ.\nВас будут видеть и слышать как человека которого вы просканируете специальным устройством.\nЭто даёт вам огромный простор по внедрению и имитации любого члена экипажа.\nПоле довольно хрупкое и может разлететься от любого резкого действия или удара.\nАктивация поля занимает 2 секунды.\nАктивный хамелеон слабо увеличивает пассивный расход энергии.\nПерезарядка: Отсутствует.",
            "Чаще их называют просто калтропы, из-за запутывающих ассоциаций с более съестным чесноком.\nПри использовании раскидывает позади вас сделанные из спрессованной энергии ловушки.\nЛовушки существуют примерно 10 секунд. Так же они пропадают — если на них наступить.\nБоль от случайного шага на них настигнет даже роботизированные конечности.\nВы не защищены от них. Не наступайте на свои же ловушки!\nСтоимость активации: 100 ед. энергии.\nПерезарядка: 5 секунд.",
            "Мгновенно ловит выбранную вами цель в обездвиживающую ловушку.\nИз ловушки легко выбраться просто сломав её любым предметом.\nОтлично подходит для временной нейтрализации одного врага.\nК тому же в неё можно поймать агрессивных животных или надоедливых охранных ботов.\nУчитывайте, что сеть не мешает жертве отстреливаться от вас.\nАктивация сети прерывает пассивные эффекты наложенные на вас. Например невидимость.\nСтоимость активации: 200 ед. энергии."
        },
        {
            "Вы воздействуете на стабильность собственного тела посредством этой экспериментальной технологии.\nДелая ваше тело нестабильным эта способность дарует вам возможность проходить сквозь стены.\nЭта экспериментальная технология не сделает вас неуязвимым для пуль и лезвий!\nНо позволит вам снять с себя наручники, болы и даже вылезти из гроба или ящика, окажись вы там заперты...\nАктивация способности мгновенна.\nАктивная форма духа потребляет 6% от максимального заряда батареи в секунду!\nПерезарядка: 25 секунд.",
            "В прошлом многие убийцы проваливая свои миссии совершали самоубийства или оказывались в лапах врага.\nСейчас же есть довольно дорогая альтернатива. Мощное устройство способное достать вас практически с того света.\nЭта машина позволит вам получить второй шанс, телепортировав вас к себе и излечив любые травмы.\nМы слышали про сомнения завязанные на идее, что это просто устройство для клонирования членов клана. Но уверяем вас, это не так.\nК сожалению из-за больших затрат на лечение и телепортацию. Устройство спасёт вас лишь один раз.\nУстройство активируется автоматически, когда вы будете при смерти.",
            "Боевое искусство ниндзя сосредоточенное на накоплении концентрации для использования приёмов.\nВ учение входят следующие приёмы:\nВыворачивание руки — заставляет жертву выронить своё оружие.\nУдар ладонью — откидывает жертву на несколько метров от вас, лишая равновесия.\nПеререзание шеи — мгновенно обезглавливает лежачую жертву катаной во вспомогательной руке.\nЭнергетическое торнадо — раскидывает врагов вокруг вас и создаёт облако дыма при наличии активного дымового устройства и энергии.\nТак же вы обучаетесь с определённым шансом отражать снаряды врагов обратно."
        },
    };

    // ── Tips data ─────────────────────────────────────────────────────────────
    private static readonly (string Icon, string Title, string Content)[] TipsData =
    [
        ("headset_green",
         "Ваш наушник",
         "В отличии от стандартных наушников\n" +
         "корпораций — наш вариант создан\n" +
         "для помощи в вашем внедрении.\n" +
         "В него встроен специальный канал\n" +
         "для общения с вашим боргом или\n" +
         "другими членами клана.\n\n" +
         "К тому же он способен\n" +
         "просканировать любые наушники\n" +
         "и скопировать доступные каналы\n" +
         "их ключей. Благодаря этому\n" +
         "вы можете накапливать\n" +
         "необходимые местные каналы связи.\n\n" +
         "Так же наушник улавливает\n" +
         "и переводит бинарные сигналы\n" +
         "синтетиков при общении друг\n" +
         "с другом. Позволяя вам\n" +
         "самим общаться с ними."),
        ("ninja_sleeper",
         "Похищение экипажа",
         "Порой клану нужны сведения,\n" +
         "которыми обладают люди\n" +
         "работающие на объекте вашей миссии.\n" +
         "В такой ситуации вам доступно\n" +
         "устройство для сканирования\n" +
         "чужого разума.\n" +
         "Даже если нужного человека\n" +
         "не найдёте, можно собрать\n" +
         "информацию по крупицам.\n\n" +
         "Для успешного похищения:\n" +
         "на шаттле есть скафандры,\n" +
         "на базе — наручники и баллоны.\n" +
         "Так же ваши перчатки способны\n" +
         "направлять электрический импульс,\n" +
         "эффективно станя цель."),
        ("ai_face",
         "Саботаж ИИ",
         "Иногда у нас заказывают саботаж ИИ.\n" +
         "Это процесс сложный и требующий\n" +
         "подготовки.\n\n" +
         "Предпочитаемый кланом метод —\n" +
         "создание уязвимости прямо\n" +
         "в загрузочной для законов,\n" +
         "позволяющей вывести ИИ из строя.\n" +
         "В итоге подходят только\n" +
         "консоли в загрузочной.\n\n" +
         "Взлом — задача нелёгкая.\n" +
         "Системы защиты везде.\n" +
         "ИИ будет противодействовать\n" +
         "вашим попыткам."),
        ("ninja_borg",
         "Саботаж роботов",
         "Иногда мы даём вам «Улучшающий»\n" +
         "прибор для роботов, встроенный\n" +
         "в ваши перчатки.\n\n" +
         "При взломе киборга вы получите\n" +
         "лояльного клану слугу способного\n" +
         "помочь в саботаже или лечении.\n\n" +
         "Робот будет оснащён личной\n" +
         "катаной, устройством маскировки,\n" +
         "пинпоинтером и генератором\n" +
         "электрических сюрикенов.\n" +
         "Катана робота не способна\n" +
         "на блюспейс транслокацию!"),
        ("server",
         "Саботаж исследований",
         "На научных объектах всегда есть\n" +
         "команда учёных и данные которые\n" +
         "хранятся на серверах.\n" +
         "Корпорации вечно грызутся\n" +
         "за знания — нам на руку.\n\n" +
         "Мы разработали вирус который\n" +
         "будет записан на ваши перчатки\n" +
         "перед миссией такого рода.\n" +
         "Вам нужно лишь загрузить его\n" +
         "на научный сервер и все их\n" +
         "исследования будут утеряны.\n\n" +
         "Загрузка вируса требует времени.\n" +
         "Системы защиты не дремлют.\n" +
         "Местный ИИ будет оповещён."),
        ("buckler",
         "Защита цели",
         "Иногда богатые шишки платят\n" +
         "за защиту определённого человека.\n" +
         "Помните:\n\n" +
         " • Защищаемый обязан дожить\n" +
         "   до конца смены!\n" +
         " • Скорее всего он не знает\n" +
         "   о вашей задаче. И лучше\n" +
         "   чтобы так и оставалось!\n" +
         " • Для объекта вы всегда\n" +
         "   нежеланное лицо.\n" +
         "   Не раскрывайте себя!\n\n" +
         "Клан не одобряет варварские\n" +
         "методы «Защиты». Не портите\n" +
         "нашу репутацию перед клиентами!"),
        ("cash",
         "Кража денег",
         "Как бы это не было тривиально —\n" +
         "иногда клан нуждается в деньгах.\n" +
         "Или вы задолжали нам.\n" +
         "В таком случае мы дадим задачу\n" +
         "достать деньги на вашей миссии.\n\n" +
         "Вы натренированы в искусстве\n" +
         "незаметных карманных краж.\n" +
         "Крадите чужие карты и\n" +
         "обналичивайте их счета.\n" +
         "Либо метьте выше — ограбьте\n" +
         "хранилища самого объекта миссии.\n\n" +
         "Самое главное.\n" +
         "Достаньте эти деньги!"),
        ("handcuff",
         "Подставить человека",
         "В некоторых ситуациях чужой\n" +
         "позор гораздо интереснее\n" +
         "чем смерть. Вам придётся\n" +
         "проявить креативность и добиться\n" +
         "того, чтобы жертву по законным\n" +
         "основаниям упекли за решётку.\n\n" +
         "Просто вписать цели срок\n" +
         "в консоли — не метод.\n" +
         "Цель легко оправдают в суде.\n\n" +
         "У вас достаточно инструментов,\n" +
         "чтобы совершить преступление\n" +
         "под личиной цели. Главное —\n" +
         "обойтись без больших последствий.\n" +
         "Лишние трупы увеличивают\n" +
         "шансы провала."),
    ];

    private const string NinjaRsi = "/Textures/_Wega/Actions/ninja.rsi";

    // ── State ─────────────────────────────────────────────────────────────────
    private readonly SpriteSystem   _sprites;
    private ContainerButton[][]?    _cells;
    private readonly int[]          _choices = [0, 0, 0, 0, 0];
    private bool                    _locked;

    // ── Controls (set by builder methods) ────────────────────────────────────
    private readonly OptionButton _genderOption;
    private readonly OptionButton _styleOption;
    private readonly OptionButton _colorOption;
    private readonly SpriteView   _spriteView;
    private readonly Button       _activateButton;
    private readonly Label        _statusLabel;
    private readonly Label        _errorLabel;

    private OptionButton? _genderRef, _styleRef, _colorRef;
    private SpriteView?   _spriteRef;
    private Button?       _activateBtnRef;
    private Label?        _statusRef, _errorRef;
    private Button?       _btnOpenShuttleRef;
    private Label?        _coordLabelRef;

    private Control      _mainPanel    = default!;
    private Control      _loadingPanel = default!;
    private BoxContainer _terminalBox  = default!;
    private ScrollContainer _terminalScroll   = default!;
    private ProgressBar     _terminalProgress = default!;
    private int             _terminalLineCount;
    private const int       TotalInitPhases = 14;

    public bool AllowClose { get; set; } = true;

    public override void Close()
    {
        if (!AllowClose)
            return;
        base.Close();
    }

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action<int, int>? OnStyleChanged;
    public event Action<int, int>? OnAbilityChanged;
    public event Action?           OnActivate;
    public event Action?           OnOpenShuttleConsole;

    // ── Constructor ───────────────────────────────────────────────────────────
    public SpiderOSWindow()
    {
        _sprites = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>();

        Title   = "Ninja Suit";
        SetSize = new Vector2(830, 580);
        MinSize = new Vector2(830, 580);

        var mainPanel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = WinBg }
        };
        _mainPanel = mainPanel;
        ContentsContainer.AddChild(mainPanel);

        var root = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            Margin = new Thickness(6, 4)
        };
        mainPanel.AddChild(root);

        root.AddChild(BuildLeftPanel());
        root.AddChild(new Control { MinSize = new Vector2(6, 0) });
        root.AddChild(BuildRightPanel());

        _loadingPanel = BuildLoadingPanel();
        _loadingPanel.Visible = false;
        ContentsContainer.AddChild(_loadingPanel);

        _genderOption   = _genderRef!;
        _styleOption    = _styleRef!;
        _colorOption    = _colorRef!;
        _spriteView     = _spriteRef!;
        _activateButton = _activateBtnRef!;
        _statusLabel    = _statusRef!;
        _errorLabel     = _errorRef!;

        _genderOption.OnItemSelected += e => OnStyleChanged?.Invoke(0, e.Id);
        _styleOption.OnItemSelected  += e => OnStyleChanged?.Invoke(1, e.Id);
        _colorOption.OnItemSelected  += e => OnStyleChanged?.Invoke(2, e.Id);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LEFT PANEL — ability module grid
    // ─────────────────────────────────────────────────────────────────────────

    private Control BuildLeftPanel()
    {
        var outer = new BoxContainer
        {
            Orientation      = LayoutOrientation.Vertical,
            HorizontalExpand = true
        };

        outer.AddChild(MakeSectionHeader("Модули костюма",
            "Устанавливаемые улучшения\nдля вашего костюма!\n" +
            "Делятся на 3 разных подхода\nдля выполнения вашей миссии.\n" +
            "Из-за больших требований по\nподдержанию работоспособности,\n" +
            "приобретение любого модуля\nблокирует модули одного уровня\nиз соседних столбцов."));

        var colRow = new BoxContainer
        {
            Orientation    = LayoutOrientation.Horizontal,
            VerticalExpand = true
        };
        outer.AddChild(colRow);

        int rows = IconStates.GetLength(0);
        _cells = new ContainerButton[rows][];
        for (var r = 0; r < rows; r++)
            _cells[r] = new ContainerButton[3];

        for (int col = 0; col < 3; col++)
        {
            colRow.AddChild(BuildColumn(col, rows));
            if (col < 2)
                colRow.AddChild(new PanelContainer
                {
                    MinWidth      = 2,
                    PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#050A05") }
                });
        }

        outer.AddChild(BuildShuttleSection());
        return outer;
    }

    private Control BuildColumn(int col, int rows)
    {
        var outer = new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride    = new StyleBoxFlat { BackgroundColor = ColBg[col] }
        };

        var box = new BoxContainer { Orientation = LayoutOrientation.Vertical };
        outer.AddChild(box);

        // Column header
        var hdrPanel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = ColHdr[col] }
        };
        var hdrRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            Margin      = new Thickness(6, 3)
        };
        hdrPanel.AddChild(hdrRow);
        hdrRow.AddChild(new Label
        {
            Text                = ColNames[col],
            HorizontalExpand    = true,
            HorizontalAlignment = Control.HAlignment.Center,
            FontColorOverride   = ColAccent[col]
        });
        var colIdx2 = col;
        var colQuestion = new ContainerButton
        {
            StyleBoxOverride = new StyleBoxFlat { BackgroundColor = Color.Transparent },
            TooltipSupplier  = _ => MakeTipTooltip(ColNames[colIdx2], ColDescs[colIdx2]),
            MouseFilter      = MouseFilterMode.Stop
        };
        colQuestion.AddChild(new Label { Text = "?", FontColorOverride = ColAccent[col], MouseFilter = MouseFilterMode.Ignore });
        hdrRow.AddChild(colQuestion);
        box.AddChild(hdrPanel);

        for (int row = 0; row < rows; row++)
        {
            var cell = BuildCell(row, col, selected: _choices[row] == col);
            _cells![row][col] = cell;
            box.AddChild(cell);

            box.AddChild(new PanelContainer
            {
                MinHeight     = 1,
                PanelOverride = new StyleBoxFlat { BackgroundColor = ColHdr[col] }
            });
        }

        return outer;
    }

    private ContainerButton BuildCell(int row, int col, bool selected)
    {
        var cell = new ContainerButton
        {
            MinSize          = new Vector2(0, 72),
            StyleBoxOverride = CellBox(col, selected),
            TooltipSupplier  = _ => MakeTooltip(row, col)
        };

        var icon    = LoadIcon(IconStates[row, col]);
        var content = new BoxContainer
        {
            Orientation         = LayoutOrientation.Vertical,
            HorizontalAlignment = Control.HAlignment.Center,
            VerticalAlignment   = Control.VAlignment.Center,
            MouseFilter         = MouseFilterMode.Ignore
        };
        cell.AddChild(content);

        if (icon != null)
        {
            content.AddChild(new TextureRect
            {
                Texture             = icon,
                MinSize             = new Vector2(48, 48),
                Stretch             = TextureRect.StretchMode.KeepAspectCentered,
                HorizontalAlignment = Control.HAlignment.Center,
                MouseFilter         = MouseFilterMode.Ignore
            });
        }
        else
        {
            content.AddChild(new Label
            {
                Text                = "◈",
                HorizontalAlignment = Control.HAlignment.Center,
                FontColorOverride   = ColAccent[col],
                MouseFilter         = MouseFilterMode.Ignore
            });
        }

        var rowIdx = row;
        var colIdx = col;
        cell.OnPressed += _ =>
        {
            if (_locked) return;
            SelectCell(rowIdx, colIdx);
            OnAbilityChanged?.Invoke(rowIdx, colIdx);
        };

        return cell;
    }

    private static StyleBoxFlat CellBox(int col, bool selected) =>
        new()
        {
            BackgroundColor = ColBg[col],
            BorderColor     = selected ? SelBorder : UnselBorder,
            BorderThickness = new Thickness(2)
        };

    private void SelectCell(int row, int col)
    {
        if (_cells == null) return;
        _choices[row] = col;
        for (int c = 0; c < _cells[row].Length; c++)
            _cells[row][c].StyleBoxOverride = CellBox(c, c == col);
    }

    // ── Tooltip factory ───────────────────────────────────────────────────────

    private static Control MakeTooltip(int row, int col)
    {
        var panel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#0A100A"),
                BorderColor     = Color.FromHex("#00FF88"),
                BorderThickness = new Thickness(1)
            }
        };

        var box = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Margin      = new Thickness(7, 5)
        };
        panel.AddChild(box);

        box.AddChild(new Label
        {
            Text              = AbilityNames[row, col],
            FontColorOverride = Color.FromHex("#00FF88")
        });

        box.AddChild(new PanelContainer
        {
            MinHeight     = 1,
            Margin        = new Thickness(0, 3, 0, 4),
            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#00AA55") }
        });

        box.AddChild(new Label
        {
            Text              = AbilityDescs[row, col],
            FontColorOverride = Color.FromHex("#88BB88")
        });

        return panel;
    }

    private static Control MakeTipTooltip(string title, string content)
    {
        var panel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#0A100A"),
                BorderColor     = Color.FromHex("#00FF88"),
                BorderThickness = new Thickness(1)
            },
            MaxWidth = 360
        };

        var box = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Margin      = new Thickness(7, 5)
        };
        panel.AddChild(box);

        box.AddChild(new Label
        {
            Text              = title,
            FontColorOverride = TealColor
        });

        box.AddChild(new PanelContainer
        {
            MinHeight     = 1,
            Margin        = new Thickness(0, 3, 0, 4),
            PanelOverride = new StyleBoxFlat { BackgroundColor = DimGreen }
        });

        box.AddChild(new Label
        {
            Text              = content,
            FontColorOverride = FieldLabel
        });

        return panel;
    }

    // ── Shuttle control ───────────────────────────────────────────────────────

    private Control BuildShuttleSection()
    {
        var panel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#060D06"),
                BorderColor     = DimGreen,
                BorderThickness = new Thickness(0, 1, 0, 0)
            }
        };
        var box = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Margin      = new Thickness(6, 4)
        };
        panel.AddChild(box);

        box.AddChild(MakeSectionHeader("Управление шаттлом",
            "Панель для удалённого управления вашим\nличным шаттлом.\n" +
            "Так же показывает вашу текущую позицию\nи позицию самого шаттла!"));

        _coordLabelRef = new Label
        {
            Text              = "GPS: —",
            FontColorOverride = FieldLabel,
            Margin            = new Thickness(0, 3, 0, 0)
        };
        box.AddChild(_coordLabelRef);

        _btnOpenShuttleRef = new Button
        {
            Text             = "▶ Открыть консоль шаттла",
            HorizontalExpand = true,
            Disabled         = true,
            Margin           = new Thickness(0, 3, 0, 0)
        };
        _btnOpenShuttleRef.OnPressed += _ => OnOpenShuttleConsole?.Invoke();
        box.AddChild(_btnOpenShuttleRef);

        return panel;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RIGHT PANEL — tips + personalisation
    // ─────────────────────────────────────────────────────────────────────────

    private Control BuildRightPanel()
    {
        var box = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            MinWidth    = 305
        };

        box.AddChild(BuildTipsPanel());
        box.AddChild(new Control { MinSize = new Vector2(0, 4) });
        box.AddChild(BuildPersonalisationPanel());

        return box;
    }

    private Control BuildTipsPanel()
    {
        var panel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = HdrBg }
        };
        var box = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Margin      = new Thickness(6)
        };
        panel.AddChild(box);

        box.AddChild(MakeSectionHeader("Советы и подсказки",
            "Молодым убийцам часто не легко\nосвоится в полевых условиях,\n" +
            "даже после тренировок.\n" +
            "Этот раздел призван помочь вам\nсоветами по часто возникающим\n" +
            "вопросам касательно миссий,\n" +
            "или рассказать о малоизвестной\nинформации, которую вы можете\nобернуть в свою пользу."));

        var grid = new GridContainer { Columns = 8, Margin = new Thickness(0, 4) };
        box.AddChild(grid);

        foreach (var (iconState, title, content) in TipsData)
        {
            var t = title;
            var c = content;
            var btn = new ContainerButton
            {
                MinSize          = new Vector2(34, 34),
                Margin           = new Thickness(1),
                StyleBoxOverride = new StyleBoxFlat
                {
                    BackgroundColor = Color.FromHex("#111E11"),
                    BorderColor     = Color.FromHex("#1E2E1E"),
                    BorderThickness = new Thickness(1)
                },
                TooltipSupplier = _ => MakeTipTooltip(t, c)
            };
            var icon = LoadIcon(iconState);
            if (icon != null)
            {
                btn.AddChild(new TextureRect
                {
                    Texture             = icon,
                    Stretch             = TextureRect.StretchMode.KeepAspectCentered,
                    HorizontalAlignment = Control.HAlignment.Center,
                    VerticalAlignment   = Control.VAlignment.Center,
                    MouseFilter         = MouseFilterMode.Ignore
                });
            }
            else
            {
                btn.AddChild(new Label
                {
                    Text              = t,
                    Margin            = new Thickness(2),
                    FontColorOverride = FieldLabel,
                    MouseFilter       = MouseFilterMode.Ignore
                });
            }
            grid.AddChild(btn);
        }

        return panel;
    }

    private Control BuildPersonalisationPanel()
    {
        var panel = new PanelContainer
        {
            VerticalExpand = true,
            PanelOverride  = new StyleBoxFlat { BackgroundColor = HdrBg }
        };
        var box = new BoxContainer
        {
            Orientation    = LayoutOrientation.Vertical,
            Margin         = new Thickness(6),
            VerticalExpand = true
        };
        panel.AddChild(box);

        box.AddChild(MakeSectionHeader("Персонализация костюма",
            "Настройка внешнего вида\nвашего костюма!\n" +
            "Наши технологии позволяют вам\nподстроить костюм под себя,\n" +
            "не теряя оборонительных качеств.\n" +
            "Потому что удобство при ношении\nкостюма жизненно важно\nдля настоящего убийцы."));

        _spriteRef = new SpriteView
        {
            Scale               = new Vector2(3, 3),
            MinSize             = new Vector2(90, 110),
            HorizontalAlignment = Control.HAlignment.Center,
            OverrideDirection   = Direction.South,
            Margin              = new Thickness(0, 6, 0, 6)
        };
        box.AddChild(_spriteRef);

        _styleRef  = AddDropdown(box, "Стиль:",           SpiderOSComponent.SuitStyleVariants);
        _colorRef  = AddDropdown(box, "Цвет:",            SpiderOSComponent.SuitColors);
        _genderRef = AddDropdown(box, "Женский/Мужской:", SpiderOSComponent.SuitGenders);

        box.AddChild(new Control { VerticalExpand = true });

        _errorRef = new Label
        {
            FontColorOverride   = Color.FromHex("#FF4444"),
            HorizontalAlignment = Control.HAlignment.Center,
            Visible             = false
        };
        box.AddChild(_errorRef);

        _statusRef = new Label
        {
            Text                = "[ SUIT INACTIVE ]",
            HorizontalAlignment = Control.HAlignment.Center,
            FontColorOverride   = DimGreen,
            Margin              = new Thickness(0, 4)
        };
        box.AddChild(_statusRef);

        _activateBtnRef = new Button
        {
            Text             = "Активировать костюм",
            HorizontalExpand = true,
            Margin           = new Thickness(0, 2),
            TooltipSupplier  = _ => MakeTipTooltip("Активация костюма",
                "Позволяет вам включить костюм\nи получить доступ к применению\nвсех функций в нём заложенных.\n" +
                "Учтите, что вы не сможете\nприобрести любые модули,\nкогда костюм будет активирован.\n" +
                "Так же включённый костюм\nпассивно потребляет заряд\nдля поддержания работы функций.\n" +
                "Активированный костюм нельзя\nснять, пока не будет\nдеактивирован.\n" +
                "Включение занимает некоторое время.\nПодумайте дважды прежде чем\nактивировать его на территории врага!")
        };
        _activateBtnRef.OnPressed += _ => OnActivate?.Invoke();
        box.AddChild(_activateBtnRef);

        return panel;
    }

    private static OptionButton AddDropdown(BoxContainer parent, string label, string[] items)
    {
        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            Margin      = new Thickness(0, 2)
        };
        row.AddChild(new Label
        {
            Text              = label,
            MinWidth          = 130,
            FontColorOverride = FieldLabel,
            VerticalAlignment = Control.VAlignment.Center
        });
        var opt = new OptionButton { HorizontalExpand = true };
        foreach (var s in items) opt.AddItem(s);
        row.AddChild(opt);
        parent.AddChild(row);
        return opt;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    public void SetPlayerEntity(EntityUid uid) => _spriteView.SetEntity(uid);

    public void UpdateState(
        int suitGender, int suitStyleVariant, int suitColor,
        bool isActivated, int[] abilityChoices, string[]? consoleErrors = null,
        bool shuttleLinked = false,
        int coordX = 0, int coordY = 0, bool inNullspace = true)
    {
        _genderOption.SelectId(Math.Clamp(suitGender,      0, _genderOption.ItemCount - 1));
        _styleOption.SelectId(Math.Clamp(suitStyleVariant, 0, _styleOption.ItemCount  - 1));
        _colorOption.SelectId(Math.Clamp(suitColor,        0, _colorOption.ItemCount  - 1));

        for (var row = 0; row < Math.Min(abilityChoices.Length, _choices.Length); row++)
            SelectCell(row, abilityChoices[row]);

        if (isActivated)
            Lock();

        if (consoleErrors is { Length: > 0 })
        {
            _errorLabel.Text    = string.Join("\n", consoleErrors);
            _errorLabel.Visible = true;
        }

        if (_btnOpenShuttleRef is { } btn) btn.Disabled = !shuttleLinked;

        if (_coordLabelRef is { } coordLabel)
            coordLabel.Text = inNullspace ? "GPS: —" : $"GPS: X {coordX}, Y {coordY}";
    }

    private void Lock()
    {
        _locked = true;
        _statusLabel.Text              = "[ SUIT ACTIVATED ]";
        _statusLabel.FontColorOverride = TealColor;
        _activateButton.Text           = "Костюм активирован";
        _activateButton.Disabled       = true;
        _genderOption.Disabled         = true;
        _styleOption.Disabled          = true;
        _colorOption.Disabled          = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Loading / terminal panel
    // ─────────────────────────────────────────────────────────────────────────

    private Control BuildLoadingPanel()
    {
        var panel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#020802") }
        };
        var vbox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Margin      = new Thickness(12, 8)
        };
        panel.AddChild(vbox);

        vbox.AddChild(new Label
        {
            Text              = "SpiderOS v2.0 — инициализация...",
            FontColorOverride = TealColor,
            Margin            = new Thickness(0, 0, 0, 6)
        });

        _terminalProgress = new ProgressBar
        {
            MinValue         = 0f,
            MaxValue         = 1f,
            Value            = 0f,
            HorizontalExpand = true,
            Margin           = new Thickness(0, 0, 0, 6)
        };
        vbox.AddChild(_terminalProgress);

        _terminalScroll = new ScrollContainer
        {
            VerticalExpand   = true,
            HorizontalExpand = true
        };
        vbox.AddChild(_terminalScroll);

        _terminalBox = new BoxContainer { Orientation = LayoutOrientation.Vertical };
        _terminalScroll.AddChild(_terminalBox);

        return panel;
    }

    public void ShowLoadingScreen()
    {
        _loadingPanel.Visible = true;
        _mainPanel.Visible    = false;
    }

    public void ShowMainScreen()
    {
        _loadingPanel.Visible = false;
        _mainPanel.Visible    = true;
    }

    public void AddTerminalLine(string line)
    {
        _terminalLineCount++;
        _terminalBox.AddChild(new Label
        {
            Text              = $"> {line}",
            FontColorOverride = DimGreen,
            Margin            = new Thickness(0, 1)
        });
        _terminalProgress.Value = Math.Min(1f, _terminalLineCount / (float) TotalInitPhases);
        _terminalScroll.SetScrollValue(new Vector2(0, float.MaxValue));
    }

    public void ClearTerminal()
    {
        _terminalBox.RemoveAllChildren();
        _terminalLineCount      = 0;
        _terminalProgress.Value = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private Texture? LoadIcon(string stateName)
    {
        try
        {
            return _sprites.Frame0(new SpriteSpecifier.Rsi(new ResPath(NinjaRsi), stateName));
        }
        catch { return null; }
    }

    private static PanelContainer MakeSectionHeader(string text, string? tooltip = null)
    {
        var panel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = HdrBg }
        };
        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            Margin      = new Thickness(6, 3)
        };
        panel.AddChild(row);
        row.AddChild(new Label
        {
            Text                = text,
            HorizontalExpand    = true,
            HorizontalAlignment = Control.HAlignment.Center,
            FontColorOverride   = TealColor
        });

        if (tooltip != null)
        {
            var t = tooltip;
            var qBtn = new ContainerButton
            {
                StyleBoxOverride = new StyleBoxFlat { BackgroundColor = Color.Transparent },
                TooltipSupplier  = _ => MakeTipTooltip("?", t),
                MouseFilter      = MouseFilterMode.Stop
            };
            qBtn.AddChild(new Label { Text = " ?", FontColorOverride = DimGreen, MouseFilter = MouseFilterMode.Ignore });
            row.AddChild(qBtn);
        }
        else
        {
            row.AddChild(new Label { Text = " ?", FontColorOverride = DimGreen });
        }

        return panel;
    }
}
