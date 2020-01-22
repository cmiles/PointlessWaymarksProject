using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PointlessWaymarksCmsData
{
    public static class Db
    {
        public static async Task<PointlessWaymarksContext> Context()
        {
            var optionsBuilder = new DbContextOptionsBuilder<PointlessWaymarksContext>();
            var dbPath = UserSettingsSingleton.CurrentSettings().DatabaseName;
            return new PointlessWaymarksContext(optionsBuilder
                .UseSqlServer(
                    $"Server = (localdb)\\mssqllocaldb; Database={dbPath}; Trusted_Connection=True; MultipleActiveResultSets=true",
                    x => x.UseNetTopologySuite()).Options);
        }
    }
}