using Azure;
using Azure.Data.Tables;

namespace Wam.Games.Entities;

public class GameEntity : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public string Code { get; set; }
    public string State { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? StartedOn { get; set; }
    public DateTimeOffset? FinishedOn { get; set; }
    public string Players { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}