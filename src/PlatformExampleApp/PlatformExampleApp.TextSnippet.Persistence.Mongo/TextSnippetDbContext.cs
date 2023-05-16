using Easy.Platform.Application.Context.UserContext;
using Easy.Platform.MongoDB;
using Easy.Platform.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using PlatformExampleApp.TextSnippet.Application.Persistence;
using PlatformExampleApp.TextSnippet.Domain.Entities;

namespace PlatformExampleApp.TextSnippet.Persistence.Mongo;

public class TextSnippetDbContext : PlatformMongoDbContext<TextSnippetDbContext>, ITextSnippetDbContext
{
    public TextSnippetDbContext(
        IOptions<PlatformMongoOptions<TextSnippetDbContext>> options,
        IPlatformMongoClient<TextSnippetDbContext> client,
        ILoggerFactory loggerFactory,
        IPlatformApplicationUserContextAccessor userContextAccessor,
        PlatformPersistenceConfiguration<TextSnippetDbContext> persistenceConfiguration) : base(
        options,
        client,
        loggerFactory,
        userContextAccessor,
        persistenceConfiguration)
    {
    }

    public IMongoCollection<TextSnippetEntity> TextSnippetCollection => GetCollection<TextSnippetEntity>();

    public override async Task InternalEnsureIndexesAsync(bool recreate = false)
    {
        if (recreate)
            await Task.WhenAll(
                TextSnippetCollection.Indexes.DropAllAsync());

        await Task.WhenAll(
            TextSnippetCollection.Indexes.CreateManyAsync(
                new List<CreateIndexModel<TextSnippetEntity>>
                {
                    new(
                        Builders<TextSnippetEntity>.IndexKeys.Ascending(p => p.CreatedBy)),
                    new(
                        Builders<TextSnippetEntity>.IndexKeys.Ascending(p => p.CreatedDate)),
                    new(
                        Builders<TextSnippetEntity>.IndexKeys.Ascending(p => p.LastUpdatedBy)),
                    new(
                        Builders<TextSnippetEntity>.IndexKeys.Ascending(p => p.LastUpdatedDate)),
                    new(
                        Builders<TextSnippetEntity>.IndexKeys.Ascending(p => p.SnippetText)),
                    new(
                        Builders<TextSnippetEntity>.IndexKeys.Ascending(p => p.Address)),
                    new(
                        Builders<TextSnippetEntity>.IndexKeys.Ascending(p => p.Addresses)),
                    new(
                        Builders<TextSnippetEntity>.IndexKeys.Ascending(p => p.AddressStrings)),
                    new(
                        Builders<TextSnippetEntity>.IndexKeys
                            .Text(p => p.SnippetText)
                            .Text(p => p.FullText))
                }));
    }

    public override List<KeyValuePair<Type, string>> EntityTypeToCollectionNameMaps()
    {
        return new List<KeyValuePair<Type, string>>
        {
            new(typeof(TextSnippetEntity), "TextSnippetEntity")
        };
    }
}
