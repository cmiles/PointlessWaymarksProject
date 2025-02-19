using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.Status;
using Serilog;

namespace PointlessWaymarks.CmsWpfControls.PointContentEditor
{
    public static class ElevationGuiHelper
    {
        /// <summary>
        /// This method will try to get Elevation for the input latitude and longitude (valid values are
        /// expected for latitude and longitude) and communicate back to the GUI about success/failure. The
        /// GUI messages mean that after calling this you should be able to set the Elevation from the
        /// return without additional GUI notifications. A null return means there was some kind of error...
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="statusContext"></param>
        /// <returns></returns>
        public static async Task<double?> GetElevation(double latitude, double longitude,
            StatusControlContext statusContext)
        {
            try
            {
                var elevationResult = await ElevationService.OpenTopoNedElevation(latitude,
                    longitude, statusContext.ProgressTracker());

                if (elevationResult != null)
                {
                    await statusContext.ToastSuccess(
                        $"Found elevation of {elevationResult} from Open Topo Data - www.opentopodata.org - NED data set");

                    return elevationResult.Value;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "GetElevation Error - Open Topo Data NED Request for {0}, {1}", latitude, longitude);
            }

            try
            {
                var elevationResult = await ElevationService.OpenTopoMapZenElevation(latitude,
                    longitude, statusContext.ProgressTracker());

                if (elevationResult == null)
                {
                    Log.Error("Unexpected Null return from an Open Topo Data Mapzen Request to {0}, {1}", latitude,
                        longitude);
                    await statusContext.ToastError("Elevation Exception - unexpected Null return...");
                    return null;
                }

                await statusContext.ToastSuccess(
                    $"Found elevation of {elevationResult} from Open Topo Data - www.opentopodata.org - Mapzen data set");

                return elevationResult.Value;
            }
            catch (Exception e)
            {
                Log.Error(e, "Open Topo Data Mapzen Request for {0}, {1}", latitude, longitude);
                await statusContext.ToastError($"Elevation Exception - {e.Message}");
            }

            await statusContext.ToastError("Elevation - could not get a value...");
            return null;
        }
    }
}