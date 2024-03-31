using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace PointlessWaymarks.LlamaAspects;

public class StaThreadConstructorGuard : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.Advice.AddInitializer(builder.Target, nameof(this.BeforeInstanceConstructor), InitializerKind.BeforeInstanceConstructor);
    }

    [Template]
    private void BeforeInstanceConstructor()
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new ThreadStateException("The current threads apartment state is not STA - this must run on the Main GUI Thread!");
        }
    }
}