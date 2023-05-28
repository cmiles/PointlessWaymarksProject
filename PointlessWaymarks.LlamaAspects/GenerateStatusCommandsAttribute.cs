using CommunityToolkit.Mvvm.Input;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace PointlessWaymarks.LlamaAspects;

public class GenerateStatusCommandsAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        foreach (var method in builder.Target.Methods.Where(p =>
                     p.Attributes.Any(typeof(BlockingCommandAttribute)) ||
                     p.Attributes.Any(typeof(NonBlockingCommandAttribute))))
            builder.Advice.IntroduceAutomaticProperty(method.DeclaringType, $"{method.Name}Command",
                typeof(RelayCommand), IntroductionScope.Default, OverrideStrategy.Ignore,
                propertyBuilder => propertyBuilder.Accessibility = Accessibility.Public);

        builder.Advice.IntroduceMethod(builder.Target, "BuildCommands");
    }

    [Template]
    public void BuildCommands()
    {
        foreach (var loopMethods in meta.Target.Type.Methods.Where(p =>
                     p.Attributes.Any(typeof(BlockingCommandAttribute))))
            meta.InsertStatement(
                $"{loopMethods.Name}Command = StatusContext.RunBlockingTaskCommand({loopMethods.Name});");

        foreach (var loopMethods in meta.Target.Type.Methods.Where(p =>
                     p.Attributes.Any(typeof(NonBlockingCommandAttribute))))
            meta.InsertStatement(
                $"{loopMethods.Name}Command = StatusContext.RunNonBlockingTaskCommand({loopMethods.Name});");
    }
}