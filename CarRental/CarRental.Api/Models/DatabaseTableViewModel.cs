namespace CarRental.Api.Models;

public sealed record DatabaseTableViewModel(
    string TableName,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows);