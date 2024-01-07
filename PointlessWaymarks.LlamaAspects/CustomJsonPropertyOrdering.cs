using System.ComponentModel;
using System.Text.Json.Serialization;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace PointlessWaymarks.LlamaAspects;

public class JsonAlphabeticalPropertyOrdering : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)

    {
        var attribute = AttributeConstruction.Create(
            typeof(JsonPropertyOrderAttribute),new object[] { 0 });

        var currentOrder = 0;

        var unassignedProperties = builder.Target.Properties
            .Where(f => f is { Accessibility: Accessibility.Public, IsImplicitlyDeclared: false } && !f.Attributes.OfAttributeType(typeof(JsonIgnoreAttribute)).Any() && !f.Attributes.OfAttributeType(typeof(JsonPropertyOrderAttribute)).Any()).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();

        var assignedProperties = builder.Target.Properties
            .Where(f => f is { Accessibility: Accessibility.Public, IsImplicitlyDeclared: false } && !f.Attributes.OfAttributeType(typeof(JsonIgnoreAttribute)).Any() && f.Attributes.OfAttributeType(typeof(JsonPropertyOrderAttribute)).Any()).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var loopProperty in assignedProperties)
        {

            var existingProperties = loopProperty.Attributes.OfAttributeType(typeof(JsonPropertyOrderAttribute));
            if (!existingProperties.Any()) continue;

            var jsonOrder = loopProperty.Attributes.OfAttributeType(typeof(JsonPropertyOrderAttribute))
                .Select(x => x.ConstructorArguments.First().Value).Cast<int>().Max();

            if(jsonOrder > currentOrder) currentOrder = jsonOrder;
        }

        foreach (var loopProperty in unassignedProperties)
        {
            builder.Advice.IntroduceAttribute(loopProperty, AttributeConstruction.Create(
                typeof(JsonPropertyOrderAttribute), new object[] { ++currentOrder }));
        }
    }
}