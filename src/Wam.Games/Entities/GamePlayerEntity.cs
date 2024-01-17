using Azure;
using Azure.Data.Tables;

namespace Wam.Games.Entities;

public class GamePlayerEntity: ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public string DisplayName { get; set; }
    public string EmailAddress { get; set; }
    public bool IsBanned { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}