using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Noesis;
using static SeaPower.SharedUi.NativeUi;

namespace SeaPower.SharedSettings;

public class SettingsDraft<T> where T : class, new()
{
    private readonly SettingsDraft draft;
    private readonly SettingsSchema<T> schema;
    public T Values => schema.Read(draft.Values);
    public bool CanEdit => draft.CanEdit;
    public IDictionary<string, string> Errors => draft.Errors;

    internal SettingsDraft(SettingsDraft draft, SettingsSchema<T> schema) => (this.draft, this.schema) = (draft, schema);

    public void Set<TValue>(Expression<Func<T, TValue>> property, TValue value)
    {
        var field = schema.Find(property);
        draft.Set(SettingsSchema<T>.Section, field.Property.Name, field.Encode(value));
    }

    public CheckBox CheckEditor(Expression<Func<T, bool>> property, Action? changed = null)
    {
        var field = schema.Find(property);
        var read = property.Compile();
        return draft.Editor(Check(field.Description.Label, read(Values), value =>
        {
            if (draft.IsBuilding || !CanEdit || read(Values) == value) return;
            Set(property, value);
            changed?.Invoke();
        }));
    }

    public TextBox NumberEditor<TNumber>(Expression<Func<T, TNumber>> property) where TNumber : struct =>
        schema.Find(property).NumberEditor(draft);

    public ComboBox ChoiceEditor<TEnum>(Expression<Func<T, TEnum>> property, Func<TEnum, string>? label = null) where TEnum : struct, Enum
    {
        schema.Find(property);
        var options = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToArray();
        int selected = Array.IndexOf(options, property.Compile()(Values));
        return draft.Editor(Choice(options, label ?? (value => value.ToString()), Math.Max(0, selected), value => Set(property, value)));
    }

    public TControl Editor<TControl>(TControl control) where TControl : FrameworkElement => draft.Editor(control);
    public void Refresh() => draft.Refresh();
}

internal class TypedSettingsProvider<T>(ISettingsProvider<T> provider) : IScriptedSettingsProvider where T : class, new()
{
    private readonly SettingsSchema<T> schema = new(provider.Defaults);
    public string Id => provider.Id;
    public string Title => provider.Title;
    public SettingsValues Defaults => schema.WriteDefaults();
    public void Apply(SettingsValues values) => provider.Apply(schema.Read(values));

    public void Build(StackPanel panel, SettingsDraft draft)
    {
        if (provider is IScriptedSettingsProvider<T> scripted) scripted.Build(panel, new(draft, schema));
        else Array.ForEach(schema.Fields, field => panel.Children.Add(field.CreateEditor(draft)));
    }
}

// Reflection is confined to the model/persistence boundary.
internal class SettingsSchema<T> where T : class, new()
{
    internal const string Section = "Values";
    internal Field[] Fields { get; }
    private readonly T defaults;

    internal SettingsSchema(T defaults)
    {
        this.defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
        Fields = typeof(T).GetProperties().Where(property => property.IsDefined(typeof(SettingAttribute)))
            .OrderBy(property => property.MetadataToken).Select(property => new Field(property)).ToArray();
        if (Fields.Length == 0) throw new ArgumentException($"{typeof(T).Name} has no properties annotated with [Setting].");
    }

    internal SettingsValues WriteDefaults()
    {
        var values = new SettingsValues();
        foreach (var field in Fields)
        {
            double value = field.Encode(field.Property.GetValue(defaults));
            if (field.Normalize(value, double.NaN) != value) throw new ArgumentException($"Default for {field.Property.Name} is outside its bounds.");
            values.Set(Section, field.Property.Name, value);
        }
        return values;
    }

    internal T Read(SettingsValues values)
    {
        var model = new T();
        foreach (var field in Fields)
        {
            double fallback = field.Encode(field.Property.GetValue(defaults));
            double value = field.Normalize(values.Get(Section, field.Property.Name, fallback), fallback);
            field.Property.SetValue(model, field.Decode(value));
        }
        return model;
    }

    internal Field Find<TValue>(Expression<Func<T, TValue>> selector) => selector.Body is MemberExpression { Member: PropertyInfo property } member
        && member.Expression == selector.Parameters[0]
        ? Fields.FirstOrDefault(field => field.Property.Equals(property)) ?? throw new ArgumentException("Select an annotated settings property.", nameof(selector))
        : throw new ArgumentException("Select a direct settings property, such as values => values.Enabled.", nameof(selector));

    internal class Field
    {
        internal PropertyInfo Property { get; }
        internal SettingAttribute Description { get; }
        private readonly Type type;

        internal Field(PropertyInfo property)
        {
            Property = property;
            Description = (SettingAttribute)Attribute.GetCustomAttribute(property, typeof(SettingAttribute))!;
            type = property.PropertyType;
            if (property.GetIndexParameters().Length != 0 || property.GetMethod?.IsPublic != true || property.SetMethod?.IsPublic != true
                || !(type == typeof(bool) || type == typeof(int) || type == typeof(float) || type == typeof(double)
                    || type.IsEnum && Enum.GetUnderlyingType(type) == typeof(int)))
                throw new ArgumentException($"{property.Name} must be a readable/writable bool, int, float, double or int-backed enum.");
            if (!double.IsFinite(Description.Minimum) || !double.IsFinite(Description.Maximum) || Description.Minimum > Description.Maximum)
                throw new ArgumentException($"{property.Name} needs finite, ordered bounds.");
            if (type == typeof(int))
            {
                Description.Minimum = Math.Max(int.MinValue, Math.Ceiling(Description.Minimum));
                Description.Maximum = Math.Min(int.MaxValue, Math.Floor(Description.Maximum));
                if (Description.Minimum > Description.Maximum) throw new ArgumentException($"{property.Name} has no integers within its bounds.");
            }
        }

        internal double Encode<TValue>(TValue value) => Convert.ToDouble(value, CultureInfo.InvariantCulture);
        internal object Decode(double value) => type == typeof(bool) ? value != 0 : type.IsEnum ? Enum.ToObject(type, (int)value)
            : Convert.ChangeType(value, type, CultureInfo.InvariantCulture);

        internal double Normalize(double value, double fallback)
        {
            if (!double.IsFinite(value)) return fallback;
            if (type == typeof(bool)) return value == 0 ? 0 : 1;
            if (type.IsEnum) return value >= int.MinValue && value <= int.MaxValue && value == Math.Truncate(value)
                && Enum.IsDefined(type, (int)value) ? value : fallback;
            value = Math.Clamp(value, Description.Minimum, Description.Maximum);
            if (type == typeof(int)) value = Math.Clamp(Math.Round(value), int.MinValue, int.MaxValue);
            if (type == typeof(float)) value = Math.Clamp(value, -float.MaxValue, float.MaxValue);
            return value;
        }

        internal TextBox NumberEditor(SettingsDraft draft)
        {
            if (type == typeof(bool) || type.IsEnum) throw new ArgumentException($"{Property.Name} is not numeric.");
            return draft.NumberEditor(Section, Property.Name, Description.Label, Description.Minimum, Description.Maximum, type == typeof(int));
        }

        internal FrameworkElement CreateEditor(SettingsDraft draft)
        {
            FrameworkElement editor = type == typeof(bool) ? draft.CheckEditor(Section, Property.Name, Description.Label)
                : type.IsEnum ? FormRow(Description.Label, draft.ChoiceEditor(Section, Property.Name,
                    Enum.GetValues(type).Cast<Enum>().Select(value => (value.ToString(), Encode(value)))))
                : FormRow(Description.Label, NumberEditor(draft));
            return string.IsNullOrWhiteSpace(Description.Help) ? editor : WithTooltip(editor, Description.Help!);
        }
    }
}
