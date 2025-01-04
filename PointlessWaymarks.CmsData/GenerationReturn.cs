using System.Text;
using Serilog;

namespace PointlessWaymarks.CmsData;

public class GenerationReturn
{
    private GenerationReturn()
    {
        CreatedOn = DateTime.Now;
    }

    public Guid? ContentId { get; set; }
    public DateTime? CreatedOn { get; }
    public Exception? Exception { get; set; }
    public string GenerationNote { get; set; } = string.Empty;
    public bool HasError { get; set; }

    public string ToErrorString()
    {
        return $"Generation Return Error Note: {GenerationNote}; {ContentId}; Exception Message: {Exception?.Message}";
    }

    public static GenerationReturn Error(string? generationNote, Guid? contentGuid = null, Exception? e = null)
    {
        Log.Error(e, "Generation Return Error, Content Guid: {0}, Note: {1}", contentGuid, generationNote);
        return new GenerationReturn {HasError = true, GenerationNote = generationNote ?? string.Empty, ContentId = contentGuid};
    }

    public static (bool hasErrors, List<GenerationReturn> errorList) HasErrors(List<GenerationReturn> toFilter)
    {
        if (!toFilter.Any()) return (false, []);

        return (toFilter.Any(x => x.HasError),
            toFilter.Where(x => x.HasError).OrderByDescending(x => x.CreatedOn).ToList());
    }

    public static (bool hasErrors, List<GenerationReturn> errorList) HasErrors(
        List<List<GenerationReturn>> toFilter)
    {
        if (!toFilter.Any()) return (false, []);

        return (toFilter.SelectMany(x => x).Any(x => x.HasError),
            toFilter.SelectMany(x => x).Where(x => x.HasError).OrderByDescending(x => x.CreatedOn).ToList());
    }

    public static GenerationReturn Success(string generationNote, Guid? contentGuid = null)
    {
        Log.Information("Generation Return Success, Content Guid: {0}, Note: {1}", contentGuid, generationNote);
        return new GenerationReturn {HasError = false, GenerationNote = generationNote, ContentId = contentGuid};
    }

    public static GenerationReturn TryCatchToGenerationReturn(Action toRun, string actionDescription,
        Guid? contentGuid = null)
    {
        try
        {
            toRun();
        }
        catch (Exception e)
        {
            return new GenerationReturn
            {
                HasError = true,
                GenerationNote =
                    $"Exception running {(string.IsNullOrWhiteSpace(actionDescription) ? "[Blank Action Description]" : actionDescription)}",
                Exception = e
            };
        }

        return Success(actionDescription, contentGuid);
    }
}