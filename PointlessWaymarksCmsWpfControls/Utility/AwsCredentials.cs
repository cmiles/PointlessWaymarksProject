using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Security.Credentials;
using PointlessWaymarksCmsData;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public static class AwsCredentials
    {
        public static string AwsCredentialResourceString()
        {
            return $"Pointless Waymarks CMS - AWS, Site - {UserSettingsSingleton.CurrentSettings().SettingsId.ToString()}";
        }

        public static (string accessKey, string secret) GetAwsCredentials()
        {
            var vault = new PasswordVault();

            IReadOnlyList<PasswordCredential> possibleCredentials;

            try
            {
                possibleCredentials = vault.FindAllByResource(AwsCredentialResourceString());
            }
            catch (Exception)
            {
                return (string.Empty, string.Empty);
            }

            if (possibleCredentials == null || !possibleCredentials.Any()) return (string.Empty, string.Empty);

            //Unexpected Condition - I think the best we can do is clean up and continue
            if (possibleCredentials.Count > 1) possibleCredentials.Skip(1).ToList().ForEach(x => vault.Remove(x));

            return (possibleCredentials.First().UserName, possibleCredentials.First().Password);
        }

        public static void RemoveAwsCredentials()
        {
            var vault = new PasswordVault();

            IReadOnlyList<PasswordCredential> possibleCredentials;

            try
            {
                possibleCredentials = vault.FindAllByResource(AwsCredentialResourceString());
            }
            catch (Exception)
            {
                //Nothing to remove
                return;
            }

            if (possibleCredentials == null || !possibleCredentials.Any()) return;

            possibleCredentials.ToList().ForEach(x => vault.Remove(x));
        }

        public static void SaveAwsCredential(string accessKey, string secret)
        {
            var vault = new PasswordVault();
            var credential = new PasswordCredential(AwsCredentialResourceString(), accessKey, secret);
            vault.Add(credential);
        }
    }
}