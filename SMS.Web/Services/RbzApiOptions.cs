namespace SMS.Web.Services;

public class RbzApiOptions
{
    public string BaseUrl { get; set; } = "https://allratestoday.com/api/v1/central-bank/rbz";
    public string ApiKey { get; set; } = "art_live_SQYXOgfA9bUWRJzPxZsQdbFct1efpPpQ";
}

public class RbzFeatureOptions
{
    public bool Enabled { get; set; } = true;
    public bool AllowBackdatedFetches { get; set; } = true;
}