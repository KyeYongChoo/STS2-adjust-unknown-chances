using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using MegaCrit.Sts2.Core.Rooms;

namespace AdjustUnknownChances;

/// <summary>
/// The odds settings controls. Built in code from stock Godot nodes plus the game's own tickbox (mod assemblies
/// can't register custom node scripts), so the same panel can be hosted by the vanilla mod list and by BaseLib's
/// Mod Configuration menu.
/// </summary>
public sealed class ModMenuPanel
{
    private static readonly Color TextColor = new(1f, 0.964706f, 0.886275f);
    private static readonly Color GoldColor = new(0.937f, 0.784f, 0.318f);
    private static readonly Color WarningColor = new(1f, 0.333333f, 0.333333f);

    private readonly NTickbox _enabledToggle;
    private readonly List<OddsRow> _rows = new();
    private readonly Label _eventLabel;
    private readonly Label _statusLabel;
    private readonly Font? _font;
    private readonly Font? _boldFont;
    private readonly int _fontSize;
    private bool _dirty;

    private sealed record OddsRow(RoomType Type, Func<OddsConfig, float> Get, Action<OddsConfig, float> Set, Label Label, HSlider Slider, SpinBox SpinBox);

    /// <summary>The panel's root node; add it to any container.</summary>
    public Control Root { get; }

    public ModMenuPanel(int fontSize = 24)
    {
        _fontSize = fontSize;
        _font = LoadFont("res://themes/kreon_regular_shared.tres");
        _boldFont = LoadFont("res://themes/kreon_bold_shared.tres") ?? _font;

        var layout = new VBoxContainer { Name = "AdjustUnknownChancesPanel" };
        layout.AddThemeConstantOverride("separation", 8);
        Root = layout;

        // Header: enable toggle (the game's own tickbox) + label + reset button
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 12);
        _enabledToggle = CreateGameTickbox();
        _enabledToggle.Connect(NTickbox.SignalName.Toggled, Callable.From<NTickbox>(tickbox =>
        {
            OddsSettings.Current.Enabled = tickbox.IsTicked;
            SaveNow();
            Refresh();
        }));
        // IsTicked needs the tickbox's child nodes, which are only looked up once it enters the tree.
        _enabledToggle.Ready += () => _enabledToggle.IsTicked = OddsSettings.Current.Enabled;

        var enabledLabel = new Label
        {
            Text = "Use custom odds",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        StyleText(enabledLabel, bold: true);
        // Clicking the label toggles too, like a normal checkbox.
        enabledLabel.GuiInput += e =>
        {
            if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
                _enabledToggle.ForceToggleTick();
        };

        var resetButton = new Button { Text = "Reset to default" };
        StyleText(resetButton);
        resetButton.Pressed += () =>
        {
            OddsSettings.ResetToVanilla();
            SaveNow();
            Refresh();
        };
        header.AddChild(_enabledToggle);
        header.AddChild(enabledLabel);
        header.AddChild(resetButton);
        layout.AddChild(header);

        AddRow(layout, RoomType.Monster, c => c.Monster, (c, v) => c.Monster = v);
        AddRow(layout, RoomType.Elite, c => c.Elite, (c, v) => c.Elite = v);
        AddRow(layout, RoomType.Treasure, c => c.Treasure, (c, v) => c.Treasure = v);
        AddRow(layout, RoomType.Shop, c => c.Shop, (c, v) => c.Shop = v);

        // Remaining Event chance box
        var eventBox = new PanelContainer();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0.35f),
            BorderColor = GoldColor,
            ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 6, ContentMarginBottom = 6,
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(4);
        eventBox.AddThemeStyleboxOverride("panel", style);
        _eventLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        StyleText(_eventLabel, bold: true, size: fontSize + 4);
        _eventLabel.AddThemeColorOverride("font_color", GoldColor);
        eventBox.AddChild(_eventLabel);
        layout.AddChild(eventBox);

        _statusLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        StyleText(_statusLabel, size: fontSize - 6);
        layout.AddChild(_statusLabel);

        // Persist anything changed via keyboard on a slider when the panel goes away.
        Root.VisibilityChanged += () => { if (!Root.IsVisibleInTree()) SaveIfDirty(); };
        Root.TreeExiting += SaveIfDirty;

        Refresh();
    }

    private void AddRow(VBoxContainer layout, RoomType type, Func<OddsConfig, float> get, Action<OddsConfig, float> set)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);

        var label = new Label { CustomMinimumSize = new Vector2(_fontSize * 5, 0), VerticalAlignment = VerticalAlignment.Center };
        StyleText(label);

        var slider = new HSlider
        {
            MinValue = 0, MaxValue = 100, Step = 0.5,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(0, 32),
        };
        var spinBox = new SpinBox
        {
            MinValue = 0, MaxValue = 100, Step = 0.5, Suffix = "%",
            CustomMinimumSize = new Vector2(130, 0),
            SelectAllOnFocus = true,
        };
        StyleText(spinBox.GetLineEdit());

        var oddsRow = new OddsRow(type, get, set, label, slider, spinBox);
        slider.ValueChanged += v => OnValueChanged(oddsRow, (float)v, save: false);
        slider.DragEnded += _ => SaveIfDirty();
        spinBox.ValueChanged += v => OnValueChanged(oddsRow, (float)v, save: true);

        row.AddChild(label);
        row.AddChild(slider);
        row.AddChild(spinBox);
        layout.AddChild(row);
        _rows.Add(oddsRow);
    }

    private void OnValueChanged(OddsRow row, float value, bool save)
    {
        OddsConfig config = OddsSettings.Current;
        // Never let Monster + Elite + Treasure + Shop exceed 100%; the overflow would just be ignored by the game.
        float othersTotal = config.NonEventTotal - row.Get(config);
        row.Set(config, Math.Clamp(value, 0f, 100f - othersTotal));
        _dirty = true;
        if (save) SaveIfDirty();
        Refresh();
    }

    /// <summary>Re-read the shared settings, e.g. after they were changed from the other menu.</summary>
    public void Refresh()
    {
        OddsConfig config = OddsSettings.Current;
        if (_enabledToggle.IsNodeReady())
            _enabledToggle.IsTicked = config.Enabled; // setting IsTicked doesn't emit Toggled
        foreach (OddsRow row in _rows)
        {
            row.Label.Text = RoomNames.For(row.Type);
            double value = row.Get(config);
            row.Slider.SetValueNoSignal(value);
            row.SpinBox.SetValueNoSignal(value);
            row.Slider.Editable = config.Enabled;
            row.SpinBox.Editable = config.Enabled;
            row.Slider.Modulate = row.SpinBox.Modulate = config.Enabled ? Colors.White : new Color(1, 1, 1, 0.45f);
        }

        _eventLabel.Text = $"{RoomNames.For(RoomType.Event)}: {config.Event:0.#}%";
        if (!config.Enabled)
        {
            _statusLabel.Text = "Custom odds are off: runs use the game's default odds. The map tooltip works either way.";
            _statusLabel.AddThemeColorOverride("font_color", TextColor);
        }
        else if (config.Event <= 0f)
        {
            _statusLabel.Text = "Event chance is 0%: unknown rooms will never start as events.";
            _statusLabel.AddThemeColorOverride("font_color", WarningColor);
        }
        else
        {
            _statusLabel.Text = "Applies to new runs. Each time a type isn't rolled, its chance grows by the value set here.";
            _statusLabel.AddThemeColorOverride("font_color", TextColor);
        }
    }

    private void SaveIfDirty()
    {
        if (!_dirty) return;
        SaveNow();
    }

    private void SaveNow()
    {
        _dirty = false;
        OddsSettings.Save();
    }

    private void StyleText(Control control, bool bold = false, int? size = null)
    {
        Font? font = bold ? _boldFont : _font;
        if (font != null) control.AddThemeFontOverride("font", font);
        control.AddThemeFontSizeOverride("font_size", size ?? _fontSize);
        control.AddThemeColorOverride("font_color", TextColor);
    }

    private static Font? LoadFont(string path) => ResourceLoader.Exists(path) ? GD.Load<Font>(path) : null;

    /// <summary>
    /// The game's tickbox: an <see cref="NTickbox"/> with the visuals from its settings tickbox scene moved in
    /// (the scene itself has no script attached). Same approach BaseLib uses for its config tickboxes.
    /// </summary>
    private static NTickbox CreateGameTickbox()
    {
        var tickbox = new NTickbox
        {
            Name = "UseCustomOddsTickbox",
            CustomMinimumSize = new Vector2(64, 64),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            FocusMode = Control.FocusModeEnum.All,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };

        Node source = GD.Load<PackedScene>("res://scenes/screens/settings_tickbox.tscn").Instantiate();
        foreach (Node child in source.GetChildren())
        {
            source.RemoveChild(child);
            // The controller selection reticle is laid out for the 320px-wide settings row; skip it.
            if (child.Name == "SelectionReticle")
            {
                child.QueueFree();
                continue;
            }
            tickbox.AddChild(child);
            SetOwnerRecursive(child, tickbox);
        }
        source.QueueFree();

        // NTickbox finds its visuals via the unique name %TickboxVisuals, which is resolved through the owner.
        tickbox.GetNode<Node>("TickboxVisuals").UniqueNameInOwner = true;
        return tickbox;
    }

    private static void SetOwnerRecursive(Node node, Node owner)
    {
        node.Owner = owner;
        foreach (Node child in node.GetChildren())
            SetOwnerRecursive(child, owner);
    }
}

/// <summary>
/// Hosts the panel in Settings > Mods when this mod is selected, in the mod image slot of
/// <see cref="NModInfoContainer"/> (this mod has no image).
/// </summary>
public static class ModInfoContainerPatches
{
    private const string HostName = "AdjustUnknownChancesSettings";

    // Live panels keyed by their host node; a new modding screen instance creates a new panel.
    private static readonly Dictionary<Control, ModMenuPanel> Panels = new();

    private static void SetVisible(NModInfoContainer container, bool visible)
    {
        Control? host = container.GetNodeOrNull<Control>(HostName);
        if (host == null)
        {
            if (!visible) return;
            host = new MarginContainer
            {
                Name = HostName,
                Position = new Vector2(22, 104),
                Size = new Vector2(620, 380),
                MouseFilter = Control.MouseFilterEnum.Pass,
            };
            var panel = new ModMenuPanel();
            host.AddChild(panel.Root);
            host.TreeExiting += () => Panels.Remove(host);
            container.AddChild(host);
            Panels[host] = panel;
        }
        else if (visible && Panels.TryGetValue(host, out ModMenuPanel? panel))
        {
            panel.Refresh();
        }
        host.Visible = visible;
    }

    [HarmonyPatch(typeof(NModInfoContainer), nameof(NModInfoContainer.Fill))]
    private static class FillPatch
    {
        private static void Postfix(NModInfoContainer __instance, Mod mod)
        {
            try { SetVisible(__instance, mod.manifest?.id == ModEntry.ModId); }
            catch (Exception ex) { GD.PrintErr($"{ModEntry.LogPrefix} Failed to build mod settings panel: {ex}"); }
        }
    }

    [HarmonyPatch(typeof(NModInfoContainer), nameof(NModInfoContainer.Clear))]
    private static class ClearPatch
    {
        private static void Postfix(NModInfoContainer __instance)
        {
            try { SetVisible(__instance, false); }
            catch (Exception ex) { GD.PrintErr($"{ModEntry.LogPrefix} Failed to hide mod settings panel: {ex}"); }
        }
    }
}
