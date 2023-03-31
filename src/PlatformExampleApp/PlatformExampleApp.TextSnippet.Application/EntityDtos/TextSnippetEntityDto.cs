using Easy.Platform.Application.Dtos;
using Easy.Platform.Common.ValueObjects;
using PlatformExampleApp.TextSnippet.Application.ValueObjectDtos;
using PlatformExampleApp.TextSnippet.Domain.Entities;

namespace PlatformExampleApp.TextSnippet.Application.EntityDtos;

public class TextSnippetEntityDto : PlatformEntityDto<TextSnippetEntity, Guid>
{
    public TextSnippetEntityDto() { }

    public TextSnippetEntityDto(TextSnippetEntity entity)
    {
        Id = entity.Id;
        SnippetText = entity.SnippetText;
        FullText = entity.FullText;
        Address = entity.Address != null ? ExampleAddressValueObjectDto.Create(entity.Address) : null;
        CreatedDate = entity.CreatedDate;
        TimeOnly = entity.TimeOnly;
    }

    public Guid? Id { get; set; }

    public string SnippetText { get; set; }

    public string FullText { get; set; }

    public TimeOnly? TimeOnly { get; set; }

    public ExampleAddressValueObjectDto Address { get; set; }

    public DateTime? CreatedDate { get; set; }

    /// <summary>
    /// Demo some common useful value object like Address
    /// </summary>
    public Address Address1 { get; set; }

    /// <summary>
    /// Demo some common useful value object like FullName
    /// </summary>
    public FullName FullName { get; set; }

    protected override object? GetSubmittedId()
    {
        return Id;
    }

    public override TextSnippetEntity MapToEntity(TextSnippetEntity entity, PlatformEntityDtoMapToEntityModes mode)
    {
        entity.Id = IsSubmitToCreate() ? Guid.NewGuid() : Id!.Value;
        entity.SnippetText = SnippetText;
        entity.FullText = FullText;

        // Demo do not update address on submit. Only when create new entity or mapping data to return to client
        if (mode == PlatformEntityDtoMapToEntityModes.MapNewEntity)
            entity.Address = Address?.MapToObject();
        entity.TimeOnly = TimeOnly ?? default;

        return entity;
    }
}
