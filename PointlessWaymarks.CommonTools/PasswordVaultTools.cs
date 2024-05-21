using Windows.Security.Credentials;
using Polly;
using Serilog;

namespace PointlessWaymarks.CommonTools;

public static class PasswordVaultTools
{
    public static (string username, string password) GetCredentials(string resourceIdentifier)
    {
        var vault = new PasswordVault();
        
        var pipeline = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();
        
        IReadOnlyList<PasswordCredential>? possibleCredentials;
        
        try
        {
            possibleCredentials = pipeline.Execute(() => vault.FindAllByResource(resourceIdentifier));
        }
        catch (Exception e)
        {
            //Log if apparently not just a not found error - but either way just return null since
            //the credential can't currently be retrieved.
            if (!e.Message.Contains("element not found", StringComparison.OrdinalIgnoreCase))
                Log.ForContext(nameof(resourceIdentifier), resourceIdentifier)
                    .Error(e, "Error in PasswordVaultTools - GetCredentials");
            possibleCredentials = null;
        }
        
        if (possibleCredentials == null || !possibleCredentials.Any()) return (string.Empty, string.Empty);
        
        //Unexpected Condition - I think the best we can do is clean up and continue
        if (possibleCredentials.Count > 1) possibleCredentials.Skip(1).ToList().ForEach(x => vault.Remove(x));
        
        possibleCredentials[0].RetrievePassword();
        
        return (possibleCredentials[0].UserName, possibleCredentials[0].Password);
    }

    /// <summary>
    ///     Removes all Credentials associated with the resourceIdentifier
    /// </summary>
    public static void RemoveCredentials(string resourceIdentifier)
    {
        var vault = new PasswordVault();
        
        var pipeline = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();
        
        IReadOnlyList<PasswordCredential> possibleCredentials;
        
        try
        {
            possibleCredentials = pipeline.Execute(() => vault.FindAllByResource(resourceIdentifier));
        }
        catch (Exception e)
        {
            //Nothing to remove
            if (e.Message.Contains("element not found", StringComparison.OrdinalIgnoreCase)) return;
            
            //Error
            Log.ForContext(nameof(resourceIdentifier), resourceIdentifier)
                .Error(e, "Error in PasswordVaultTools - RemoveCredentials");
            throw;
        }
        
        if (possibleCredentials == null || !possibleCredentials.Any()) return;
        
        possibleCredentials.ToList().ForEach(x => vault.Remove(x));
    }

    /// <summary>
    ///     Removes any existing Credentials Associated with the resourceIdentifier and then saves the new credentials
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
        
        var pipeline = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();
        
        //An error will throw on timeout
        var credential = new PasswordCredential(resourceIdentifier, userName, password);
        pipeline.Execute(() => vault.Add(credential));
    }
}