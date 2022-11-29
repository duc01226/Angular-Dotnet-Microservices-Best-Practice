using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Easy.Platform.Infrastructures.FileStorage;

namespace Easy.Platform.AzureFileStorage;

public class PlatformAzureFileStorageFileItem : IPlatformFileStorageFileItem
{
    public const string BlobDescriptionKey = "Description";

    public string FullFilePath { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    public string ContentType { get; init; }
    public string Etag { get; init; }
    public long Size { get; init; }
    public string Description { get; init; }
    public string AbsoluteUri { get; init; }

    public static PlatformAzureFileStorageFileItem Create(BlobItem blobItem, BlobContainerClient blobContainer)
    {
        var result = new PlatformAzureFileStorageFileItem
        {
            FullFilePath = blobItem.Name,
            LastModified = blobItem.Properties.LastModified,
            ContentType = blobItem.Properties.ContentType,
            Etag = blobItem.Properties.ETag?.ToString(),
            Size = blobItem.Properties.ContentLength ?? 0,
            Description = GetMetadata(blobItem.Metadata, BlobDescriptionKey),
            AbsoluteUri = blobContainer.Uri.ConcatRelativePath(blobItem.Name).AbsoluteUri
        };

        return result;
    }

    public static string GetFullFilePath(BlobClient blobClient)
    {
        return Util.Path.ConcatRelativePath(blobClient.BlobContainerName, blobClient.Name);
    }

    public static IDictionary<string, string> SetMetadata(
        IDictionary<string, string> currentMetadata,
        string key,
        string value)
    {
        currentMetadata.Remove(key);

        // Convert value to base 64 string because if value has space, request save metadata will
        // throw exception BlobNotFound
        currentMetadata.Add(key, value?.Contains(' ') == true ? value.ToBase64String() : value ?? "");

        return currentMetadata;
    }

    public static string GetMetadata(
        IDictionary<string, string> currentMetadata,
        string key)
    {
        // From base 64 string because if value has space, request save metadata will
        // throw exception BlobNotFound. So when save metadata we has encode data to base64
        return currentMetadata.TryGetValueOrDefault(key)
            .PipeIfNotNull(_ => _.TryFromBase64ToString());
    }
}
