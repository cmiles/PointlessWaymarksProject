using System.ComponentModel;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace PointlessWaymarks.LlamaAspects;

public class NotifyPropertyChangedAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.Advice.ImplementInterface(builder.Target, typeof(INotifyPropertyChanged), OverrideStrategy.Ignore);

        foreach (var property in builder.Target.Properties.Where(p =>
                     p is { IsAbstract: false, Writeability: Writeability.All } &&
                     !p.Attributes.Any(typeof(DoNotGenerateInpc))))
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
        if (value != meta.Target.Property.Value)
        {
            meta.Proceed();
            OnPropertyChanged(meta.Target.Property.Name);
        }

        return value;
    }

    [InterfaceMember] public event PropertyChangedEventHandler? PropertyChanged;
}