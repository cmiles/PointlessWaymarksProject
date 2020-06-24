using System;
using System.Collections.Generic;
using System.Linq;

namespace PointlessWaymarksCmsData.FolderStructureAndGeneratedContent
{
    public class GenerationReturn
    {
        public GenerationReturn()
        {
            CreatedOn = DateTime.Now;
        }

        public Guid? ContentId { get; set; }

        public DateTime? CreatedOn { get; }
        public string ErrorNote { get; set; }

        public Exception Exception { get; set; }

        public bool HasError { get; set; }

        public static List<GenerationReturn> FilterForErrors(List<GenerationReturn> toFilter)
        {
            if (toFilter == null || !toFilter.Any()) return new List<GenerationReturn>();

            return toFilter.Where(x => x.HasError).OrderByDescending(x => x.CreatedOn).ToList();
        }

        public static List<GenerationReturn> FilterForErrors(List<List<GenerationReturn>> toFilter)
        {
            if (toFilter == null || !toFilter.Any()) return new List<GenerationReturn>();

            return toFilter.SelectMany(x => x).Where(x => x.HasError).OrderByDescending(x => x.CreatedOn).ToList();
        }

        public static GenerationReturn NoError()
        {
            return new GenerationReturn {HasError = false};
        }

        public static GenerationReturn TryCatchToGenerationReturn(Action toRun, string actionDescription)
        {
            if (toRun == null)
                return new GenerationReturn {HasError = true, ErrorNote = "Attempted to run a Null Action?"};

            try
            {
                toRun();
            }
            catch (Exception e)
            {
                return new GenerationReturn
                {
                    HasError = true,
                    ErrorNote =
                        $"Exception running {(string.IsNullOrWhiteSpace(actionDescription) ? "[Blank Action Description]" : actionDescription)}",
                    Exception = e
                };
            }

            return NoError();
        }
    }
}