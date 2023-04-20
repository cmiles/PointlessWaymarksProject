using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PointlessWaymarks.CloudBackupTests
{
    public static class Helpers
    {
        public static FileInfo RandomFile(string fullName)
        {
            //Credit to [performance - Creating a Random File in C# - Stack Overflow](https://stackoverflow.com/questions/4432178/creating-a-random-file-in-c-sharp)
            var sizeInMb = new Random().Next(1, 10);
            var data = new byte[sizeInMb * 1024 * 1024];
            var rng = new Random();
            rng.NextBytes(data);
            File.WriteAllBytes(fullName, data);
            return new FileInfo(fullName);
        }
    }
}
