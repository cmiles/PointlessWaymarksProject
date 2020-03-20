using System.Windows;
using PointlessWaymarksCmsData;

namespace PointlessWaymarksCmsContentEditor
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            UserSettingsUtilities.VerifyAndCreate();
            var log = Db.Log().Result;
            log.Database.EnsureCreated();
            var db = Db.Context().Result;
            db.Database.EnsureCreated();
        }
    }
}