using System.Runtime.CompilerServices;
using System.Reflection;
using HarmonyLib;
using Noesis;
using SeapowerUI;
using UnityEngine;
using SeaPower.SharedUi;
using static SeaPower.SharedUi.NativeUi;

namespace SeaPower.SharedSettings;

public class SettingsDraft(SettingsValues values, Action? changed = null, Action? refresh = null, string modId = "")
{
    private static readonly Brush FieldBackground = new SolidColorBrush(Noesis.Color.FromRgb(28, 30, 33));
    private static readonly Brush FieldBorder = new SolidColorBrush(Noesis.Color.FromRgb(83, 88, 94));
    private static readonly Brush InvalidFieldBorder = new SolidColorBrush(Noesis.Color.FromRgb(206, 107, 93));
    private readonly Dictionary<string, string> invalidNumbers = new();
    private bool building;
    private readonly ModIdentity activityOwner = string.IsNullOrWhiteSpace(modId) ? default : new(modId);
    public SettingsValues Values { get; } = values;
    public Dictionary<string, string> Errors { get; } = new();
    public bool CanEdit => changed != null;
    internal bool IsBuilding => building;

    public void Set(string section, string key, bool value) => Set(section, key, value ? 1 : 0);

    public void Set(string section, string key, double value)
    {
        if (changed == null || building || Values.Get(section, key, double.NaN) == value) return;
        using var activity = ModActivity.Begin(activityOwner, ModActivityKind.SettingsEdit, $"{section}/{key}");
        Values.Set(section, key, value);
        changed();
    }

    public void Remove(string section, string key)
    {
        if (changed == null || building || !Values.Remove(section, key)) return;
        string field = $"{section}/{key}";
        Errors.Remove(field);
        invalidNumbers.Remove(field);
        changed();
    }

    public void Refresh()
    {
        if (!building) refresh?.Invoke();
    }

    internal void Build(StackPanel panel, Action<StackPanel, SettingsDraft> build, SettingsDraft? previous)
    {
        building = true;
        if (previous != null) previous.building = true;
        try
        {
            panel.Children.Clear();
            build(panel, this);
        }
        finally
        {
            building = false;
            if (previous != null) previous.building = false;
        }
    }

    public CheckBox CheckEditor(string section, string key, string label) =>
        Editor(Check(label, Values.GetBoolean(section, key), value => Set(section, key, value)));

    public TextBox NumberEditor(string section, string key, string label, double min, double max, bool wholeNumbers = false) =>
        NumberEditor(section, key, label, Math.Clamp(Values.Get(section, key, min), min, max), min, max, wholeNumbers);

    public ComboBox ChoiceEditor(string section, string key, IEnumerable<(string Label, double Value)> choices)
    {
        var items = choices.ToArray();
        if (items.Length == 0) throw new ArgumentException("Supply at least one choice.", nameof(choices));
        int selected = Array.FindIndex(items, item => item.Value == Values.Get(section, key));
        return Editor(Choice(items, item => item.Label, Math.Max(0, selected), item => Set(section, key, item.Value)));
    }

    public T Editor<T>(T control) where T : FrameworkElement
    {
        control.IsEnabled = CanEdit;
        if (control is TextBox field)
        {
            field.Background = FieldBackground;
            field.BorderBrush = FieldBorder;
            field.BorderThickness = new Thickness(1);
        }
        return control;
    }

    public TextBox NumberEditor(string section, string key, string label, double value, double min, double max, bool wholeNumbers = false)
    {
        string field = $"{section}/{key}";
        TextBox input = null!;
        input = Editor(Number(value, min, max, number =>
        {
            if (!wholeNumbers || number == Math.Truncate(number)) Set(section, key, number);
        }, error =>
        {
            if (error == null && wholeNumbers && double.TryParse(input.Text, out double number) && number != Math.Truncate(number))
                error = "Enter a whole number.";
            input.ToolTip = error;
            input.BorderBrush = error == null ? FieldBorder : InvalidFieldBorder;
            if (error == null)
            {
                Errors.Remove(field);
                invalidNumbers.Remove(field);
            }
            else
            {
                Errors[field] = $"{label}. {error}";
                invalidNumbers[field] = input.Text;
            }
        }));
        if (invalidNumbers.TryGetValue(field, out string text)) input.Text = text;
        return input;
    }

    internal void DiscardErrors()
    {
        Errors.Clear();
        invalidNumbers.Clear();
    }
}

public static class GameplaySettings
{
    private const string HostName = "CrimeanTomGameplaySettings";
    private static readonly ConditionalWeakTable<OptionsMenuView, List<(Func<bool> Validate, Func<bool> Save)>> Apply = new();
    private static readonly Dictionary<string, Action<OptionsMenuView, Grid, TabControl>> Pages = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, (IScriptedSettingsProvider Provider, ModSettings Settings)> Providers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConditionalWeakTable<ISettingsProvider, IScriptedSettingsProvider> Adapters = new();
    private static bool installed;

    /// <summary>Registers either a declarative or a scripted provider and delivers its saved values immediately.</summary>
    public static void Register<T>(ISettingsProvider<T> provider) where T : class, new() =>
        RegisterCore(Adapters.GetValue(provider, _ => new TypedSettingsProvider<T>(provider)));

    // Anchor Chain hands us an instance whose generic model type is known only at runtime.
    public static void Register(ISettingsProvider provider)
    {
        var contract = provider.GetType().GetInterfaces().SingleOrDefault(type => type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(ISettingsProvider<>))
            ?? throw new ArgumentException("Implement ISettingsProvider<T> with a concrete settings model.", nameof(provider));
        typeof(GameplaySettings).GetMethod(nameof(RegisterTyped), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(contract.GetGenericArguments()[0]).Invoke(null, [provider]);
    }

    private static void RegisterTyped<T>(ISettingsProvider provider) where T : class, new() => Register((ISettingsProvider<T>)provider);

    private static void RegisterCore(IScriptedSettingsProvider provider)
    {
        if (Providers.TryGetValue(provider.Id, out var existing))
        {
            if (!ReferenceEquals(existing.Provider, provider)) throw new ArgumentException($"Settings ID '{provider.Id}' is already registered.", nameof(provider));
            return;
        }
        var settings = new ModSettings(provider.Id, provider.Defaults);
        provider.Apply(settings.Current);
        settings.Changed += () => provider.Apply(settings.Current);
        Register(provider.Title, settings, provider.Build);
        Providers.Add(provider.Id, (provider, settings));
    }

    public static void Register(string title, ModSettings settings, Action<StackPanel, SettingsDraft> build) =>
        Register(new Harmony("crimean-tom.controls.settings"), title, settings, build);

    // Retained for mods that compile the source package into their own assembly.
    public static void Register(Harmony harmony, string title, ModSettings settings, Action<StackPanel, SettingsDraft> build)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Supply a settings tab title.", nameof(title));
        if (!installed)
        {
            harmony.Patch(AccessTools.Constructor(typeof(OptionsMenuView)),
                postfix: new HarmonyMethod(typeof(GameplaySettings), nameof(Attach)));
            harmony.Patch(AccessTools.Method(typeof(OptionsMenuView), "ButtonAccept_Click"),
                prefix: new HarmonyMethod(typeof(GameplaySettings), nameof(Accept)));
            installed = true;
        }
        Pages[settings.Id] = (view, host, tabs) => AddPage(view, host, tabs, title, settings, build);
    }

    private static void Attach(OptionsMenuView __instance)
    {
        __instance.Loaded += (_, _) =>
        {
            if (Apply.TryGetValue(__instance, out _)) return;
            try
            {
                if (Find<TabControl>(__instance) is not { } outer || outer.Items.Count < 3
                    || outer.Items[2] is not TabItem gameplay || gameplay.Content is not FrameworkElement native)
                    throw new InvalidOperationException("Could not find Sea Power's Gameplay tab.");

                var host = native as Grid;
                if (host?.Name != HostName)
                {
                    gameplay.Content = null;
                    host = BuildHost(native);
                    gameplay.Content = host;
                }
                var tabs = (TabControl)host.Children[0];
                Apply.GetOrCreateValue(__instance);
                foreach (var addPage in Pages.Values)
                {
                    try { addPage(__instance, host, tabs); }
                    catch (Exception error) { Debug.LogError($"[Controls & Settings] Could not build settings page: {error}"); }
                }
                tabs.SelectedIndex = 0;
            }
            catch (Exception error) { Debug.LogError($"[Controls & Settings] {error}"); }
        };
    }

    private static Grid BuildHost(FrameworkElement native)
    {
        var host = new Grid { Name = HostName };
        host.RowDefinitions.Add(new() { Height = GridLength.Auto });
        host.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
        var tabs = Tabs(("General", new Border()));
        tabs.Name = "ModGameplayTabs";
        host.Children.Add(tabs);
        Grid.SetRow(native, 1);
        host.Children.Add(native);
        tabs.SelectionChanged += (_, _) =>
        {
            for (int i = 1; i < host.Children.Count; i++)
                host.Children[i].Visibility = i == tabs.SelectedIndex + 1 ? Visibility.Visible : Visibility.Collapsed;
        };
        return host;
    }

    private static void AddPage(OptionsMenuView view, Grid host, TabControl tabs, string title, ModSettings settings,
        Action<StackPanel, SettingsDraft> build)
    {
        bool useDefaults = settings.UseDefaults;
        bool dirty = false;
        bool refreshPending = false;
        SettingsDraft custom = null!;
        SettingsDraft? displayed = null;
        var body = new StackPanel { Margin = new Thickness(12, 8, 12, 8) };
        var editors = new StackPanel();
        var message = Text("");
        var reset = Button("Reset to mod defaults", () =>
        {
            custom = Edit(settings.Defaults.Copy());
            dirty = true;
            Refresh();
        });
        reset.Visibility = useDefaults ? Visibility.Collapsed : Visibility.Visible;
        var mode = Check("Use mod defaults", useDefaults, value =>
        {
            if (useDefaults == value) return;
            useDefaults = value;
            dirty = true;
            custom.DiscardErrors();
            reset.Visibility = useDefaults ? Visibility.Collapsed : Visibility.Visible;
            Refresh();
        });
        var actions = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(reset, Dock.Right);
        actions.Children.Add(reset);
        actions.Children.Add(mode);
        body.Children.Add(actions);
        body.Children.Add(message);
        body.Children.Add(editors);
        custom = Edit(settings.Edit());
        Refresh();

        var page = Scroll(body);
        page.Visibility = Visibility.Collapsed;
        Grid.SetRow(page, 1);
        int index = 1;
        while (index < tabs.Items.Count && tabs.Items[index] is TabItem item
            && string.CompareOrdinal(item.Header?.ToString(), title) < 0) index++;
        host.Children.Insert(index + 1, page);
        var tab = new TabItem { Header = title, Tag = settings.Id };
        tabs.Items.Insert(index, tab);
        Apply.GetOrCreateValue(view).Add((Validate, Save));

        SettingsDraft Edit(SettingsValues values) => new(values, () => dirty = true, RequestRefresh, settings.Id);

        void RequestRefresh()
        {
            if (refreshPending) return;
            refreshPending = true;
            // A checkbox may be the control being removed. Wait until its native
            // input event has unwound before rebuilding the visual tree.
            editors.Dispatcher.BeginInvoke(() =>
            {
                refreshPending = false;
                if (editors.View == null) return;
                try { Refresh(); }
                catch (Exception error)
                {
                    message.Text = error.Message;
                    Debug.LogError($"[Controls & Settings] Could not refresh {title}: {error}");
                }
            });
        }

        void Refresh()
        {
            using var activity = ModActivity.Begin(new ModIdentity(settings.Id), ModActivityKind.SettingsRebuild, title);
            message.Text = "";
            var draft = useDefaults ? new(settings.Defaults.Copy(), refresh: RequestRefresh, modId: settings.Id) : custom;
            draft.Build(editors, build, displayed);
            displayed = draft;
        }

        bool Validate()
        {
            if (useDefaults || custom.Errors.Values.FirstOrDefault() is not { } error) return true;
            ShowError(error);
            return false;
        }

        void ShowError(string error)
        {
            message.Text = error;
            tabs.SelectedItem = tab;
            page.ScrollToVerticalOffset(0);
        }

        bool Save()
        {
            try
            {
                if (!dirty) return true;
                settings.Apply(useDefaults, custom.Values);
                dirty = false;
                return true;
            }
            catch (Exception error)
            {
                ShowError(error.Message);
                return false;
            }
        }
    }

    private static bool Accept(OptionsMenuView __instance) => !Apply.TryGetValue(__instance, out var actions)
        || actions.All(action => action.Validate()) && actions.All(action => action.Save());
}
