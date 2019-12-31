using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TheLemmonWorkshopData;

namespace TheLemmonWorkshopWpfControls
{
    public static class Db
    {
        public static async Task<LemmonWorkshopContext> Context()
        {
            var optionsBuilder = new DbContextOptionsBuilder<LemmonWorkshopContext>();
            var dbPath = (await UserSettingsUtilities.ReadSettings()).DatabaseName;
            return new LemmonWorkshopContext(optionsBuilder
                .UseSqlServer(
                    $"Server = (localdb)\\mssqllocaldb; Database={dbPath}; Trusted_Connection=True; MultipleActiveResultSets=true",
                    x => x.UseNetTopologySuite()).Options);
        }
    }
}