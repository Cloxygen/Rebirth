using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ActionBarView : MonoBehaviour
{
    [SerializeField] private ActionBar actionBar;
    [SerializeField] private Color missingIconTint = new Color(0.42f, 0.38f, 0.48f, 1.0f);

    [Header("Picker")]
    [SerializeField] private float pickerEntrySize = 34.0f;
    [SerializeField] private float pickerGap = 4.0f;
    [SerializeField] private float pickerOffset = 8.0f;
    [SerializeField] private Color pickerBackground = new Color(0.043f, 0.031f, 0.063f, 0.92f);

    private UIDocument document;
    private VisualElement root;
    private VisualElement[] slotElements;
    private VisualElement[] artElements;
    private VisualElement[] plusIcons;
    private Label[] keyLabels;
    private CssBox[] cooldownBoxes;
    private float[] lastSweep;
    private bool[] lastFilled;
    private float findCooldown;

    private VisualElement picker;
    private int pickerSlotIndex = -1;

    private void OnEnable()
    {
        document = GetComponent<UIDocument>();
        slotElements = null;
    }

    private void OnDisable()
    {
        ClosePicker();
        root = null;
    }

    private void Update()
    {
        FindActionBarIfNeeded();
        CacheSlots();

        if (slotElements == null || actionBar == null) return;

        IReadOnlyList<ActionBar.AbilitySlot> slots = actionBar.Slots;

        for (int index = 0; index < slotElements.Length; index++)
        {
            ActionBar.AbilitySlot slot = index < slots.Count ? slots[index] : null;
            bool filled = slot != null && slot.IsFilled;

            if (keyLabels[index] != null && slot != null && !string.IsNullOrEmpty(slot.keyLabel))
            {
                if (keyLabels[index].text != slot.keyLabel) keyLabels[index].text = slot.keyLabel;
            }

            float sweep = filled ? slot.CooldownFraction : 0.0f;
            if (cooldownBoxes[index] != null && !Mathf.Approximately(sweep, lastSweep[index]))
            {
                lastSweep[index] = sweep;
                cooldownBoxes[index].SetSweep(sweep);
            }

            if (lastFilled[index] != filled || filled)
            {
                lastFilled[index] = filled;
                ApplyIcon(artElements[index], slot, filled);

                if (plusIcons[index] != null)
                {
                    plusIcons[index].style.display = filled ? DisplayStyle.None : DisplayStyle.Flex;
                }
            }

            if (slotElements[index].pickingMode != PickingMode.Position)
            {
                slotElements[index].pickingMode = PickingMode.Position;
            }
        }
    }

    private void FindActionBarIfNeeded()
    {
        if (actionBar != null) return;

        findCooldown -= Time.deltaTime;
        if (findCooldown > 0.0f) return;
        findCooldown = 1.0f;

        actionBar = LocalPlayer.Get<ActionBar>();
    }

    private void CacheSlots()
    {
        if (slotElements != null) return;

        root = document != null ? document.rootVisualElement : null;
        if (root == null) return;

        VisualElement bar = root.Q("action-bar");
        if (bar == null) return;

        List<VisualElement> found = bar.Query(className: "slot").ToList();
        if (found.Count == 0) return;

        slotElements = found.ToArray();
        keyLabels = new Label[slotElements.Length];
        cooldownBoxes = new CssBox[slotElements.Length];
        artElements = new VisualElement[slotElements.Length];
        plusIcons = new VisualElement[slotElements.Length];
        lastSweep = new float[slotElements.Length];
        lastFilled = new bool[slotElements.Length];

        for (int index = 0; index < slotElements.Length; index++)
        {
            keyLabels[index] = slotElements[index].Q<Label>(className: "slot-key");
            cooldownBoxes[index] = slotElements[index].Q<CssBox>(className: "slot-cooldown");
            artElements[index] = slotElements[index].Q(className: "slot-art");
            plusIcons[index] = slotElements[index].Q(className: "slot-icon");
            lastSweep[index] = -1.0f;
            lastFilled[index] = true;

            slotElements[index].pickingMode = PickingMode.Position;

            int captured = index;
            slotElements[index].RegisterCallback<PointerDownEvent>(evt =>
            {
                TogglePicker(captured);
                UIPointer.ConsumeClick();
                evt.StopPropagation();
            });
        }
    }

    private void ApplyIcon(VisualElement art, ActionBar.AbilitySlot slot, bool filled)
    {
        if (art == null) return;

        if (!filled)
        {
            art.style.display = DisplayStyle.None;
            return;
        }

        art.style.display = DisplayStyle.Flex;

        Sprite icon = slot.Icon;
        if (icon != null)
        {
            art.style.backgroundImage = new StyleBackground(icon);
            art.style.backgroundColor = new StyleColor(Color.clear);
        }
        else
        {
            art.style.backgroundImage = new StyleBackground(StyleKeyword.None);
            art.style.backgroundColor = new StyleColor(missingIconTint);
        }
    }

    private void TogglePicker(int slotIndex)
    {
        if (pickerSlotIndex == slotIndex)
        {
            ClosePicker();
            return;
        }

        OpenPicker(slotIndex);
    }

    private void OpenPicker(int slotIndex)
    {
        ClosePicker();

        if (root == null || actionBar == null) return;

        LearnedAbilities learned = actionBar.GetComponent<LearnedAbilities>();
        if (learned == null) return;

        picker = new VisualElement();
        picker.style.position = Position.Absolute;
        picker.style.flexDirection = FlexDirection.Row;
        picker.style.backgroundColor = new StyleColor(pickerBackground);
        picker.style.paddingLeft = pickerGap;
        picker.style.paddingRight = pickerGap;
        picker.style.paddingTop = pickerGap;
        picker.style.paddingBottom = pickerGap;

        AddPickerEntry(slotIndex, null);
        foreach (AbilityDefinition ability in learned.Available)
        {
            AddPickerEntry(slotIndex, ability);
        }

        root.Add(picker);
        pickerSlotIndex = slotIndex;

        picker.RegisterCallback<GeometryChangedEvent>(_ => PlacePicker(slotIndex));
    }

    private void AddPickerEntry(int slotIndex, AbilityDefinition ability)
    {
        VisualElement entry = new VisualElement();
        entry.style.width = pickerEntrySize;
        entry.style.height = pickerEntrySize;
        entry.style.marginRight = pickerGap;

        if (ability != null && ability.icon != null)
        {
            entry.style.backgroundImage = new StyleBackground(ability.icon);
        }
        else
        {
            entry.style.backgroundColor = new StyleColor(
                ability == null ? new Color(0.0f, 0.0f, 0.0f, 0.45f) : missingIconTint);
        }

        entry.RegisterCallback<PointerDownEvent>(evt =>
        {
            actionBar.RequestBindAbility(slotIndex, ability != null ? ability.name : string.Empty);
            ClosePicker();
            UIPointer.ConsumeClick();
            evt.StopPropagation();
        });

        picker.Add(entry);
    }

    private void PlacePicker(int slotIndex)
    {
        if (picker == null || slotElements == null) return;
        if (slotIndex < 0 || slotIndex >= slotElements.Length) return;

        Rect slotBounds = slotElements[slotIndex].worldBound;
        float width = picker.resolvedStyle.width;
        float height = picker.resolvedStyle.height;

        picker.style.left = slotBounds.center.x - width * 0.5f;
        picker.style.top = slotBounds.yMin - height - pickerOffset;
    }

    private void ClosePicker()
    {
        if (picker != null && picker.parent != null) picker.RemoveFromHierarchy();

        picker = null;
        pickerSlotIndex = -1;
    }
}
