using Microsoft.Data.SqlClient;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.Destructurers;

namespace Easy.Platform.EfCore.Logging.Exceptions;

/// <summary>
/// A destructurer for <see cref="SqlException" />.
/// </summary>
/// <seealso cref="ExceptionDestructurer" />
public class PlatformMicrosoftSqlExceptionDestructurer : ExceptionDestructurer
{
    /// <inheritdoc />
    public override Type[] TargetTypes => [typeof(SqlException)];

    /// <inheritdoc />
    public override void Destructure(
        Exception exception,
        IExceptionPropertiesBag propertiesBag,
        Func<Exception, IReadOnlyDictionary<string, object>> destructureException)
    {
        base.Destructure(exception, propertiesBag, destructureException);

        var sqlException = (SqlException)exception;
        propertiesBag.AddProperty(nameof(SqlException.ClientConnectionId), sqlException.ClientConnectionId);
        propertiesBag.AddProperty(nameof(SqlException.Class), sqlException.Class);
        propertiesBag.AddProperty(nameof(SqlException.LineNumber), sqlException.LineNumber);
        propertiesBag.AddProperty(nameof(SqlException.Number), sqlException.Number);
        propertiesBag.AddProperty(nameof(SqlException.Server), sqlException.Server);
        propertiesBag.AddProperty(nameof(SqlException.State), sqlException.State);
        propertiesBag.AddProperty(nameof(SqlException.Errors), sqlException.Errors.Cast<SqlError>().ToArray());
    }
}
