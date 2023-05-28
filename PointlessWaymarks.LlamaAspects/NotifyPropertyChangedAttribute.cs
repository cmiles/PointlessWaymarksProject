using System.ComponentModel;
using System.Runtime.CompilerServices;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace PointlessWaymarks.LlamaAspects;

public class NotifyPropertyChangedAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.Advice.ImplementInterface(builder.Target, typeof(INotifyPropertyChanged), OverrideStrategy.Ignore);

        foreach (var property in builder.Target.Properties.Where(p =>
                     p is { IsAbstract: false, Writeability: Writeability.All }))
            builder.Advice.OverrideAccessors(property, null, nameof(OverridePropertySetter));
    }

    [Introduce(WhenExists = OverrideStrategy.Ignore)]
    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(meta.This, new PropertyChangedEventArgs(name));
    }

    [Template]
    private dynamic OverridePropertySetter(dynamic value)
    {
        SetField(ref meta.Target.FieldOrProperty.Value, value);

        return value;
    }

    [InterfaceMember] public event PropertyChangedEventHandler? PropertyChanged;

    [Introduce(WhenExists = OverrideStrategy.Ignore)]
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException(nameof(propertyName));

        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}