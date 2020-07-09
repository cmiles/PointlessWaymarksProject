using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Database;

namespace PointlessWaymarksCmsData.Content
{
    public class GenerationReturn
    {
        private GenerationReturn()
        {
            CreatedOn = DateTime.Now;
        }

        public Guid? ContentId { get; set; }
        public DateTime? CreatedOn { get; }
        public Exception Exception { get; set; }
        public string GenerationNote { get; set; }
        public bool HasError { get; set; }

        public static async Task<GenerationReturn> Error(string generationNote, Guid? contentGuid = null,
            Exception e = null)
        {
            await EventLogContext.TryWriteGenerationEventToLog(
                $"Success Generation Return; Content Guid: {(contentGuid == null ? "Null" : contentGuid.ToString())}; Note: {generationNote};{(e == null ? string.Empty : $" Exception: {e}")}",
                "GenerationReturn");
            return new GenerationReturn {HasError = true, GenerationNote = generationNote, ContentId = contentGuid};
        }

        private static List<GenerationReturn> FilterForErrors(List<GenerationReturn> toFilter)
        {
            if (toFilter == null || !toFilter.Any()) return new List<GenerationReturn>();

            return toFilter.Where(x => x.HasError).OrderByDescending(x => x.CreatedOn).ToList();
        }

        private static List<GenerationReturn> FilterForErrors(List<List<GenerationReturn>> toFilter)
        {
            if (toFilter == null || !toFilter.Any()) return new List<GenerationReturn>();

            return toFilter.SelectMany(x => x).Where(x => x.HasError).OrderByDescending(x => x.CreatedOn).ToList();
        }

        public static (bool hasErrors, List<GenerationReturn> errorList) HasErrors(List<GenerationReturn> toFilter)
        {
            if (toFilter == null || !toFilter.Any()) return (false, new List<GenerationReturn>());

            return (toFilter.Any(x => x.HasError),
                toFilter.Where(x => x.HasError).OrderByDescending(x => x.CreatedOn).ToList());
        }

        public static (bool hasErrors, List<GenerationReturn> errorList) HasErrors(
            List<List<GenerationReturn>> toFilter)
        {
            if (toFilter == null || !toFilter.Any()) return (false, new List<GenerationReturn>());

            return (toFilter.SelectMany(x => x).Any(x => x.HasError),
                toFilter.SelectMany(x => x).Where(x => x.HasError).OrderByDescending(x => x.CreatedOn).ToList());
        }

        public static async Task<GenerationReturn> Success(string generationNote, Guid? contentGuid = null)
        {
            await EventLogContext.TryWriteGenerationEventToLog(
                $"Success Generation Return; Content Guid: {(contentGuid == null ? "Null" : contentGuid.ToString())}; Note: {generationNote}",
                "GenerationReturn");
            return new GenerationReturn {HasError = false, GenerationNote = generationNote, ContentId = contentGuid};
        }

        public static async Task<GenerationReturn> TryCatchToGenerationReturn(Action toRun, string actionDescription,
            Guid? contentGuid = null)
        {
            if (toRun == null)
                return new GenerationReturn {HasError = true, GenerationNote = "Attempted to run a Null Action?"};

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

            return await Success(actionDescription, contentGuid);
        }
    }
}