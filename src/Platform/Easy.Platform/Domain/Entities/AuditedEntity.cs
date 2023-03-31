using Easy.Platform.Common.Extensions;

namespace Easy.Platform.Domain.Entities;

public interface IDateAuditedEntity
{
    public DateTime? CreatedDate { get; set; }

    public DateTime? LastUpdatedDate { get; set; }
}

public interface IUserAuditedEntity
{
    public IUserAuditedEntity SetCreatedBy(object value);
    public IUserAuditedEntity SetLastUpdatedBy(object value);
}

public interface IUserAuditedEntity<TUserId> : IUserAuditedEntity
{
    public TUserId CreatedBy { get; set; }

    public TUserId LastUpdatedBy { get; set; }
}

public interface IFullAuditedEntity<TUserId> : IDateAuditedEntity, IUserAuditedEntity<TUserId>
{
}

public abstract class RootAuditedEntity<TEntity, TPrimaryKey, TUserId> : RootEntity<TEntity, TPrimaryKey>, IFullAuditedEntity<TUserId>
    where TEntity : Entity<TEntity, TPrimaryKey>, new()
{
    private TUserId lastUpdatedBy;
    private DateTime? lastUpdatedDate;

    public RootAuditedEntity()
    {
        CreatedDate ??= DateTime.UtcNow;
        LastUpdatedDate ??= CreatedDate;
    }

    public RootAuditedEntity(TUserId createdBy) : this()
    {
        CreatedBy = createdBy;
        LastUpdatedBy ??= CreatedBy;
    }

    public TUserId CreatedBy { get; set; }

    public TUserId LastUpdatedBy
    {
        get => lastUpdatedBy ?? CreatedBy;
        set => lastUpdatedBy = value;
    }

    public DateTime? CreatedDate { get; set; }

    public DateTime? LastUpdatedDate
    {
        get => lastUpdatedDate ?? CreatedDate;
        set => lastUpdatedDate = value;
    }

    public IUserAuditedEntity SetCreatedBy(object value)
    {
        if (value != typeof(TUserId).GetDefaultValue())
        {
            CreatedBy = (TUserId)value;
            LastUpdatedBy = CreatedBy;
        }

        return this;
    }

    public IUserAuditedEntity SetLastUpdatedBy(object value)
    {
        if (value != typeof(TUserId).GetDefaultValue())
            LastUpdatedBy = (TUserId)value;

        return this;
    }
}
