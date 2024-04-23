using Windows.Security.Credentials;
using Polly;
using Polly.Retry;

namespace PointlessWaymarks.CommonTools;

public static class PasswordVaultTools
{
    public static (string username, string password) GetCredentials(string resourceIdentifier)
    {
        var vault = new PasswordVault();
        
        ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();
        
        IReadOnlyList<PasswordCredential>? possibleCredentials;

        try
        {
            possibleCredentials = pipeline.Execute(() => vault.FindAllByResource(resourceIdentifier));
        }
        catch (Exception e)
        {
            possibleCredentials = null;
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
    public static void RemoveCredentials(string resourceIdentifier)
    {
        var vault = new PasswordVault();
        
        IReadOnlyList<PasswordCredential> possibleCredentials;
        
        try
        {
            possibleCredentials = vault.FindAllByResource(resourceIdentifier);
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
    /// <param name="resourceIdentifier"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    public static void SaveCredentials(string resourceIdentifier, string userName, string password)
    {
        //The Credential Manager will update a password if the resource and username are the same but will otherwise
        //create a new entry - removing any previous records seem like the easiest way atm to keep only one entry per
        //resource since the strategy here is to make the resource the lookup key (the app doesn't know the username)
        RemoveCredentials(resourceIdentifier);
        
        var vault = new PasswordVault();
        var credential = new PasswordCredential(resourceIdentifier, userName, password);
        vault.Add(credential);
    }
}