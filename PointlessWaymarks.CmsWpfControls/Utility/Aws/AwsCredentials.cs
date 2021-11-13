using Windows.Security.Credentials;
using PointlessWaymarks.CmsData;

namespace PointlessWaymarks.CmsWpfControls.Utility.Aws
{
    public static class AwsCredentials
    {
        /// <summary>
        ///     Returns the Credential Manager Resource Key for the current settings file for AWS Site credentials
        /// </summary>
        /// <returns></returns>
        public static string AwsSiteCredentialResourceString()
        {
            return
                $"Pointless Waymarks CMS - AWS, Site - {UserSettingsSingleton.CurrentSettings().SettingsId.ToString()}";
        }

        /// <summary>
        ///     Retrieves the AWS Credentials associated with this settings file
        /// </summary>
        /// <returns></returns>
        public static (string accessKey, string secret) GetAwsSiteCredentials()
        {
            var vault = new PasswordVault();

            IReadOnlyList<PasswordCredential> possibleCredentials;

            try
            {
                possibleCredentials = vault.FindAllByResource(AwsSiteCredentialResourceString());
            }
            catch (Exception)
            {
                return (string.Empty, string.Empty);
            }

            if (possibleCredentials == null || !possibleCredentials.Any()) return (string.Empty, string.Empty);

            //Unexpected Condition - I think the best we can do is clean up and continue
            if (possibleCredentials.Count > 1) possibleCredentials.Skip(1).ToList().ForEach(x => vault.Remove(x));

            possibleCredentials[0].RetrievePassword();

            return (possibleCredentials[0].UserName, possibleCredentials[0].Password);
        }

        /// <summary>
        ///     Removes all AWS Credentials associated with this settings file
        /// </summary>
        public static void RemoveAwsSiteCredentials()
        {
            var vault = new PasswordVault();

            IReadOnlyList<PasswordCredential> possibleCredentials;

            try
            {
                possibleCredentials = vault.FindAllByResource(AwsSiteCredentialResourceString());
            }
            catch (Exception)
            {
                //Nothing to remove
                return;
            }

            if (possibleCredentials == null || !possibleCredentials.Any()) return;

            possibleCredentials.ToList().ForEach(x => vault.Remove(x));
        }

        /// <summary>
        ///     Removes any existing AWS Credentials Associated with this settings file and Saves new Credentials
        /// </summary>
        /// <param name="accessKey"></param>
        /// <param name="secret"></param>
        public static void SaveAwsSiteCredential(string accessKey, string secret)
        {
            //The Credential Manager will update a password if the resource and username are the same but will otherwise
            //create a new entry - removing any previous records seem like the easiest way atm to keep only one entry per
            //resource since the strategy here is to make the resource the lookup key (the app doesn't know the username)
            RemoveAwsSiteCredentials();

            var vault = new PasswordVault();
            var credential = new PasswordCredential(AwsSiteCredentialResourceString(), accessKey, secret);
            vault.Add(credential);
        }
    }
}