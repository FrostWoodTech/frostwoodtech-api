namespace FrostWoodTech.API.Media;

public sealed class NeonStorageOptions
{
    public string Endpoint { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Required for SigV4 signing.</summary>
    public string Region { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    public string BaseFolder { get; set; } = "frostwoodtech";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint)
        && !string.IsNullOrWhiteSpace(AccessKey)
        && !string.IsNullOrWhiteSpace(SecretKey)
        && !string.IsNullOrWhiteSpace(Region)
        && !string.IsNullOrWhiteSpace(BucketName);
}
