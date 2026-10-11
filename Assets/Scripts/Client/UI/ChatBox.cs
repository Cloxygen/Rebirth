using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public enum ChatChannel
{
    System,
    Say,
    Party,
    Combat,
    Loot,
    Whisper
}

public enum ChatTab
{
    All,
    General,
    Combat,
    Loot
}

[ExecuteAlways]
[RequireComponent(typeof(UIDocument))]
public class ChatBox : MonoBehaviour
{
    private const float CDefWidth = 390f;
    private const float CDefPanelHeight = 186f;
    private const float CDefLeftPercent = 1.6875f;
    private const float CDefHeightAboveBaseline = 26f;
    private const float CDefFontSize = 13f;
    private const float CDefLineSpacing = 1.65f;
    private const float CDefCornerRadius = 3.3f;
    private const float CDefBorderWidth = 1.1f;
    private static readonly Color CDefPanelTop = new Color(0.078f, 0.067f, 0.098f, 0.72f);
    private static readonly Color CDefPanelBottom = new Color(0.043f, 0.039f, 0.055f, 0.85f);
    private static readonly Color CDefBorder = new Color(0.290f, 0.247f, 0.169f, 0.85f);
    private static readonly Color CDefTabActive = new Color(0.937f, 0.906f, 0.839f, 1f);
    private static readonly Color CDefTabIdle = new Color(0.541f, 0.478f, 0.361f, 1f);

    public bool show = true;

    [Header("Plate")]
    [Range(160f, 900f)] public float width = CDefWidth;
    [Range(60f, 600f)] public float panelHeight = CDefPanelHeight;
    public Color panelTopColor = CDefPanelTop;
    public Color panelBottomColor = CDefPanelBottom;
    public Color borderColor = CDefBorder;
    [Range(0f, 4f)] public float borderWidth = CDefBorderWidth;
    [Range(0f, 16f)] public float cornerRadius = CDefCornerRadius;

    [Header("Position")]
    [Range(0f, 40f)] public float leftPercent = CDefLeftPercent;
    [Range(0f, 700f)] public float heightAboveBaseline = CDefHeightAboveBaseline;

    [Header("Text")]
    [Range(7f, 24f)] public float fontSize = CDefFontSize;
    [Range(0f, 12f)] public float lineSpacing = CDefLineSpacing;
    public bool showTimestamps = true;
    public Color timestampColor = new Color(0.612f, 0.580f, 0.518f, 0.75f);

    [Header("Tabs")]
    public Color tabActiveColor = CDefTabActive;
    public Color tabIdleColor = CDefTabIdle;
    public ChatTab startingTab = ChatTab.All;

    [Header("Channel colours")]
    public Color systemColor  = new Color(0.933f, 0.729f, 0.302f, 1f);
    public Color sayColor     = new Color(0.937f, 0.906f, 0.839f, 1f);
    public Color partyColor   = new Color(0.341f, 0.714f, 0.949f, 1f);
    public Color combatColor  = new Color(0.906f, 0.255f, 0.290f, 1f);
    public Color lootColor    = new Color(0.404f, 0.769f, 0.435f, 1f);
    public Color whisperColor = new Color(0.722f, 0.306f, 0.878f, 1f);

    [Header("Keys")]
    [SerializeField] private InputAction openAction;

    [Header("Behaviour")]
    public ChatChannel defaultChannel = ChatChannel.Say;
    public string playerName = "You";
    [Range(20, 500)] public int scrollback = 200;
    public bool clickThroughWhenClosed;
    [Range(8f, 160f)] public float scrollStep = 44f;
    public bool greetOnStart = true;
    public bool seedTestMessages = true;

    public static ChatBox Instance { get; private set; }

    public event Action<ChatChannel, string> Submitted;

    public bool IsOpen => isOpen;
    public ChatChannel ActiveChannel => channel;

    private UIDocument doc;
    private VisualElement root;
    private VisualElement chat;
    private CssBox panel;
    private ScrollView feed;
    private VisualElement entry;
    private Label channelLabel;
    private TextField input;
    private VisualElement tabStrip;
    private readonly List<(ChatTab Tab, Label Element)> tabs = new List<(ChatTab, Label)>();
    private ChatTab activeTab = ChatTab.All;
    private ChatTab lastStartingTab = ChatTab.All;
    private bool stickToBottom = true;
    private float lastScrollY;

    private readonly List<VisualElement> feedChrome = new List<VisualElement>();
    private readonly List<VisualElement> entryChrome = new List<VisualElement>();
    private readonly List<(ChatChannel Channel, string Who, string Text)> pending = new List<(ChatChannel, string, string)>();

    private ChatChannel channel;
    private bool isOpen;
    private bool closing;
    private bool awaitingFocus;
    private int closedFrame = -10;

    private void Awake()
    {
        channel = defaultChannel;

        if (openAction.bindings.Count == 0)
        {
            openAction.AddBinding("<Keyboard>/enter");
            openAction.AddBinding("<Keyboard>/numpadEnter");
        }
    }

    private void OnEnable()
    {
        Instance = this;
        if (Application.isPlaying) openAction.Enable();
        CacheTree();
        ApplyStyle();
    }

    private void OnDisable()
    {
        if (Application.isPlaying) openAction.Disable();

        if (isOpen) UITextEntry.Active = false;
        isOpen = false;

        if (Instance == this) Instance = null;
        root = null;
        chat = null;
    }

    private void OnValidate() { CacheTree(); ApplyStyle(); }

    private void Start()
    {
        if (!Application.isPlaying) return;
        if (greetOnStart) Log(ChatChannel.System, "Press Enter to chat. Tab switches channel, Escape cancels.");
        if (seedTestMessages) SeedTestMessages();
    }

    private void Update()
    {
        CacheTree();
        if (chat == null) return;

        ApplyStyle();
        if (!Application.isPlaying) return;

        if (!show)
        {
            if (isOpen) Close();
            return;
        }

        if (!isOpen && !UITextEntry.Active && Time.frameCount != closedFrame && openAction.WasPressedThisFrame()) Open();

        if (isOpen && !awaitingFocus && !InputHasFocus()) Close();

        ApplyPicking();
        if (!FeedIsPickable) ScrollFeedUnderPointer();

        float y = feed != null ? feed.scrollOffset.y : 0f;
        if (IsAtBottom()) stickToBottom = true;
        else if (!Mathf.Approximately(y, lastScrollY)) stickToBottom = false;
        lastScrollY = y;
    }

    private void CacheTree()
    {
        if (doc == null) doc = GetComponent<UIDocument>();
        VisualElement r = doc != null ? doc.rootVisualElement : null;
        if (r == null) { root = null; chat = null; return; }
        if (r == root && chat != null) return;

        root = r;
        chat = root.Q<VisualElement>("chat");
        if (chat == null) return;

        panel = chat.Q<CssBox>("chat-panel");
        feed = chat.Q<ScrollView>("chat-feed");
        tabStrip = chat.Q<VisualElement>("chat-tabs");
        entry = chat.Q<VisualElement>("chat-entry");
        channelLabel = chat.Q<Label>("chat-channel");
        input = chat.Q<TextField>("chat-input");

        feedChrome.Clear();
        if (feed != null)
        {
            feedChrome.Add(feed);
            feedChrome.Add(feed.contentViewport);
            feedChrome.Add(feed.contentContainer);
            feedChrome.Add(feed.verticalScroller);
        }

        tabs.Clear();
        AddTab(ChatTab.All,     "chat-tab-all");
        AddTab(ChatTab.General, "chat-tab-general");
        AddTab(ChatTab.Combat,  "chat-tab-combat");
        AddTab(ChatTab.Loot,    "chat-tab-loot");

        entryChrome.Clear();
        if (entry != null) entryChrome.Add(entry);
        if (input != null)
        {
            entryChrome.Add(input);
            VisualElement inner = input.Q("unity-text-input");
            if (inner != null) entryChrome.Add(inner);
        }

        chat.RegisterCallback<PointerDownEvent>(_ => { if (isOpen) UIPointer.ConsumeClick(); });

        chat.RegisterCallback<NavigationMoveEvent>(e =>
        {
            if (input != null && e.target is VisualElement t && (t == input || input.Contains(t))) return;
            e.StopPropagation();
        }, TrickleDown.TrickleDown);

        if (feed != null) feed.focusable = false;

        feed?.contentContainer.RegisterCallback<GeometryChangedEvent>(_ =>
        {
            if (stickToBottom) PinToBottom();
        });

        if (input != null)
        {
            input.RegisterCallback<KeyDownEvent>(OnEntryKeyDown, TrickleDown.TrickleDown);
            input.RegisterCallback<FocusOutEvent>(_ => { if (isOpen && !closing) Close(); });
        }

        if (Application.isPlaying) feed?.Clear();

        SetOpen(false);
        UpdateChannelLabel();
        UpdateTabs();

        foreach ((ChatChannel c, string w, string t) in pending) Append(c, w, t);
        pending.Clear();
    }

    [ContextMenu("Reset chat window")]
    public void ResetChatWindow()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Reset chat window");
#endif
        width = CDefWidth;
        panelHeight = CDefPanelHeight;
        leftPercent = CDefLeftPercent;
        heightAboveBaseline = CDefHeightAboveBaseline;
        fontSize = CDefFontSize;
        lineSpacing = CDefLineSpacing;
        cornerRadius = CDefCornerRadius;
        borderWidth = CDefBorderWidth;
        panelTopColor = CDefPanelTop;
        panelBottomColor = CDefPanelBottom;
        borderColor = CDefBorder;
        tabActiveColor = CDefTabActive;
        tabIdleColor = CDefTabIdle;
        showTimestamps = true;
        startingTab = ChatTab.All;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        CacheTree();
        ApplyStyle();
    }

    private void AddTab(ChatTab tab, string elementName)
    {
        Label l = chat.Q<Label>(elementName);
        if (l == null) return;
        tabs.Add((tab, l));
        l.pickingMode = PickingMode.Position;
        l.RegisterCallback<PointerDownEvent>(e =>
        {
            SetTab(tab);
            UIPointer.ConsumeClick();
            e.StopPropagation();
        });
    }

    public void SetTab(ChatTab tab)
    {
        activeTab = tab;
        UpdateTabs();
        ApplyFilter();
    }

    private void UpdateTabs()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            (ChatTab tab, Label element) = tabs[i];
            if (element == null) continue;
            bool on = tab == activeTab;
            element.EnableInClassList("chat-tab-active", on);
            element.style.color = on ? tabActiveColor : tabIdleColor;
        }
    }

    private void ApplyFilter()
    {
        if (feed == null) return;

        int n = feed.childCount;
        for (int i = 0; i < n; i++)
        {
            VisualElement line = feed.ElementAt(i);
            bool visible = !(line.userData is ChatChannel c) || TabShows(activeTab, c);
            line.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        stickToBottom = true;
    }

    private bool IsAtBottom()
    {
        if (feed?.contentContainer == null || feed.contentViewport == null) return true;

        float content = feed.contentContainer.layout.height;
        float view = feed.contentViewport.layout.height;
        if (float.IsNaN(content) || float.IsNaN(view)) return true;

        float max = Mathf.Max(0f, content - view);
        return feed.scrollOffset.y >= max - 4f;
    }

    private void PinToBottom()
    {
        if (feed == null) return;
        feed.scrollOffset = new Vector2(feed.scrollOffset.x, float.MaxValue);
    }

    private static bool TabShows(ChatTab tab, ChatChannel c)
    {
        switch (tab)
        {
            case ChatTab.Combat: return c == ChatChannel.Combat;
            case ChatTab.Loot:   return c == ChatChannel.Loot;
            case ChatTab.General:
                return c == ChatChannel.System || c == ChatChannel.Say
                    || c == ChatChannel.Party  || c == ChatChannel.Whisper;
            default: return true;
        }
    }

    private void ApplyStyle()
    {
        if (chat == null) return;

        chat.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        if (!show) return;

        chat.style.width = width;
        chat.style.left = Length.Percent(leftPercent);
        chat.style.marginBottom = heightAboveBaseline;

        if (startingTab != lastStartingTab)
        {
            lastStartingTab = startingTab;
            SetTab(startingTab);
        }
        UpdateTabs();

        if (panel != null)
        {
            panel.style.height = panelHeight;
            SetRadius(panel, cornerRadius);
            SetBorder(panel, borderColor, borderWidth);
            panel.SetBaseGradient(panelTopColor, panelBottomColor);
        }

        if (entry is CssBox entryBox)
        {
            SetRadius(entryBox, cornerRadius * 0.667f);
            SetBorder(entryBox, borderColor, borderWidth);
        }

        if (channelLabel != null) channelLabel.style.fontSize = fontSize;

        if (input != null)
        {
            VisualElement inner = input.Q("unity-text-input");
            if (inner != null)
            {
                inner.style.fontSize = fontSize;
                inner.style.color = ColorFor(channel);
            }
        }

        if (feed == null) return;

        int n = feed.childCount;
        for (int i = 0; i < n; i++)
        {
            VisualElement line = feed.ElementAt(i);
            line.style.marginBottom = lineSpacing;

            Label time = line.Q<Label>(className: "chat-time");
            if (time != null)
            {
                time.style.display = showTimestamps ? DisplayStyle.Flex : DisplayStyle.None;
                time.style.fontSize = fontSize;
                time.style.color = timestampColor;
            }

            Label text = line.Q<Label>(className: "chat-text");
            if (text != null)
            {
                text.style.fontSize = fontSize;
                if (line.userData is ChatChannel c) text.style.color = ColorFor(c);
            }
        }
    }

    public Color ColorFor(ChatChannel c)
    {
        switch (c)
        {
            case ChatChannel.System:  return systemColor;
            case ChatChannel.Say:     return sayColor;
            case ChatChannel.Party:   return partyColor;
            case ChatChannel.Combat:  return combatColor;
            case ChatChannel.Loot:    return lootColor;
            case ChatChannel.Whisper: return whisperColor;
            default:                  return sayColor;
        }
    }

    private static void SetRadius(VisualElement e, float r)
    {
        e.style.borderTopLeftRadius = r;
        e.style.borderTopRightRadius = r;
        e.style.borderBottomRightRadius = r;
        e.style.borderBottomLeftRadius = r;
    }

    private static void SetBorder(VisualElement e, Color c, float w)
    {
        e.style.borderTopWidth = w;
        e.style.borderRightWidth = w;
        e.style.borderBottomWidth = w;
        e.style.borderLeftWidth = w;
        e.style.borderTopColor = c;
        e.style.borderRightColor = c;
        e.style.borderBottomColor = c;
        e.style.borderLeftColor = c;
    }

    private void OnEntryKeyDown(KeyDownEvent e)
    {
        if (!isOpen) return;

        switch (e.keyCode)
        {
            case KeyCode.Return:
            case KeyCode.KeypadEnter: Submit(); break;
            case KeyCode.Escape:      Close();  break;
            case KeyCode.Tab:         CycleChannel(); break;
            default: return;
        }

        e.StopPropagation();
    }

    private bool FeedIsPickable => isOpen || !clickThroughWhenClosed;

    private void ApplyPicking()
    {
        PickingMode mode = FeedIsPickable ? PickingMode.Position : PickingMode.Ignore;

        for (int i = 0; i < feedChrome.Count; i++)
            if (feedChrome[i] != null) feedChrome[i].pickingMode = mode;

        for (int i = 0; i < entryChrome.Count; i++)
            if (entryChrome[i] != null) entryChrome[i].pickingMode = mode;

        if (tabStrip != null) tabStrip.pickingMode = PickingMode.Ignore;
        for (int i = 0; i < tabs.Count; i++)
            if (tabs[i].Element != null) tabs[i].Element.pickingMode = PickingMode.Position;

        if (feed?.verticalScroller != null)
        {
            feed.verticalScroller.pickingMode = PickingMode.Position;
            if (feed.verticalScroller.slider != null)
                feed.verticalScroller.slider.pickingMode = PickingMode.Position;
            VisualElement dragger = feed.verticalScroller.slider?.Q("unity-dragger");
            if (dragger != null) dragger.pickingMode = PickingMode.Position;
        }
    }

    private void ScrollFeedUnderPointer()
    {
        if (feed?.panel == null || Mouse.current == null) return;

        float wheel = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Approximately(wheel, 0f)) return;

        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(feed.panel, Mouse.current.position.ReadValue());
        if (!feed.worldBound.Contains(panelPos)) return;

        if (feed.verticalScroller != null && feed.verticalScroller.resolvedStyle.display != DisplayStyle.None
            && feed.verticalScroller.worldBound.Contains(panelPos)) return;

        float step = Mathf.Sign(wheel) * scrollStep;
        feed.scrollOffset = new Vector2(feed.scrollOffset.x, feed.scrollOffset.y - step);
    }

    private void Open()
    {
        if (isOpen) return;
        SetOpen(true);
        if (input == null) return;

        input.value = string.Empty;
        ApplyPicking();

        awaitingFocus = true;
        input.schedule.Execute(() =>
        {
            awaitingFocus = false;
            if (!isOpen || input == null) return;
            VisualElement inner = input.Q("unity-text-input");
            (inner ?? input).Focus();
        });
    }

    private bool InputHasFocus()
    {
        if (input?.panel?.focusController == null) return false;
        VisualElement focused = input.panel.focusController.focusedElement as VisualElement;
        return focused != null && (focused == input || input.Contains(focused));
    }

    private void Submit()
    {
        if (!isOpen) return;

        string text = input != null ? input.value : string.Empty;
        Close();

        if (string.IsNullOrWhiteSpace(text)) return;
        text = text.Trim();

        if (!text.StartsWith("/")) Append(channel, playerName, text);

        Submitted?.Invoke(channel, text);
        channel = defaultChannel;
        UpdateChannelLabel();
    }

    private void Close()
    {
        if (!isOpen || closing) return;
        closing = true;

        SetOpen(false);
        closedFrame = Time.frameCount;

        if (input != null)
        {
            input.value = string.Empty;
            input.Blur();
        }

        closing = false;
    }

    private void SetOpen(bool open)
    {
        isOpen = open;
        UITextEntry.Active = open;
        if (entry != null) entry.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void CycleChannel()
    {
        ChatChannel[] all = (ChatChannel[])Enum.GetValues(typeof(ChatChannel));
        channel = all[(Array.IndexOf(all, channel) + 1) % all.Length];
        UpdateChannelLabel();
    }

    private void UpdateChannelLabel()
    {
        if (channelLabel == null) return;
        channelLabel.text = $"[{channel}]";
        channelLabel.style.color = ColorFor(channel);
    }

    public void Log(string message) => Log(ChatChannel.System, message);

    public void Log(ChatChannel messageChannel, string message) => Log(messageChannel, null, message);

    public void Log(ChatChannel messageChannel, string who, string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        if (feed == null) { pending.Add((messageChannel, who, message)); return; }

        Append(messageChannel, who, message);
    }

    private void Append(ChatChannel messageChannel, string who, string message)
    {
        if (feed == null) return;

        VisualElement line = new VisualElement();
        line.AddToClassList("chat-line");
        line.userData = messageChannel;
        line.pickingMode = PickingMode.Ignore;

        Label time = new Label(DateTime.Now.ToString("[HH:mm]"));
        time.AddToClassList("chat-time");
        time.pickingMode = PickingMode.Ignore;
        line.Add(time);

        Label text = new Label(Compose(messageChannel, who, message));
        text.AddToClassList("chat-text");
        text.pickingMode = PickingMode.Ignore;
        line.Add(text);

        bool follow = IsAtBottom();

        feed.Add(line);
        while (feed.childCount > scrollback) feed.RemoveAt(0);

        if (!TabShows(activeTab, messageChannel)) line.style.display = DisplayStyle.None;

        ApplyStyle();

        stickToBottom = follow;
    }

    private static string Compose(ChatChannel c, string who, string message)
    {
        string tag = (c == ChatChannel.Party || c == ChatChannel.Whisper) ? $"[{c}] " : string.Empty;
        return string.IsNullOrEmpty(who) ? tag + message : $"{tag}{who}: {message}";
    }

    [ContextMenu("Seed test messages")]
    public void SeedTestMessages()
    {
        foreach ((ChatChannel c, string who, string text) in TestSession) Log(c, who, text);
    }

    private static readonly (ChatChannel Channel, string Who, string Text)[] TestSession =
    {
        (ChatChannel.System,  null,          "Welcome to Ashen Hollow."),
        (ChatChannel.Say,     "Corvin",      "anyone running the hollow tonight?"),
        (ChatChannel.Say,     "Bracken",     "give me ten minutes, still selling"),
        (ChatChannel.Party,   "Sister Vael", "The shrines are stirring again. Keep off the low road."),
        (ChatChannel.Combat,  null,          "Dreadfang Marauder hits you for 12."),
        (ChatChannel.Combat,  null,          "You strike Dreadfang Marauder for 47. Critical!"),
        (ChatChannel.Loot,    null,          "You receive 240 essence."),
        (ChatChannel.Whisper, "Ilse",        "meet me by the north gate, don't bring the priest"),
        (ChatChannel.Say,     "You",         "on my way"),
        (ChatChannel.Party,   "Sister Vael", "The old road past the second shrine is flooded again — go around by the ridge unless you fancy swimming in something that used to be a person."),
        (ChatChannel.Combat,  null,          "Dreadfang Marauder hits you for 8."),
        (ChatChannel.Combat,  null,          "You strike Dreadfang Marauder for 51."),
        (ChatChannel.System,  null,          "Dreadfang Marauder is slain."),
        (ChatChannel.Loot,    null,          "You receive Ashen Pact Fragment."),
        (ChatChannel.Loot,    null,          "You receive 118 essence."),
        (ChatChannel.System,  null,          "Quest updated: Slay Dreadfang Marauders (8/12)."),
        (ChatChannel.Say,     "Bracken",     "nice drop"),
        (ChatChannel.Whisper, "Ilse",        "still waiting"),
        (ChatChannel.Party,   "Sister Vael", "Two shrines cleansed. One left."),
        (ChatChannel.System,  null,          "You have learned Ashen Bolt."),
        (ChatChannel.Say,     "Corvin",      "regroup at the gate"),
        (ChatChannel.Say,     "You",         "coming"),
    };
}
