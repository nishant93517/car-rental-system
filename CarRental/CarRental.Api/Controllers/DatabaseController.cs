using CarRental.Api.Data;
using CarRental.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Api.Controllers;

[Authorize(Policy = "FleetManagers")]
public sealed class DatabaseController(RentalDbContext db) : Controller
{
    public async Task<IActionResult> Index(string table = "Cars")
    {
        var tableNames = await db.Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name")
            .ToListAsync();

        if (!tableNames.Contains(table, StringComparer.Ordinal))
        {
            return NotFound();
        }

        var escapedTableName = table.Replace("'", "''", StringComparison.Ordinal);
        var columnNames = await db.Database.SqlQueryRaw<string>(
            $"SELECT name AS Value FROM pragma_table_info('{escapedTableName}') ORDER BY cid")
            .ToListAsync();

        ViewBag.TableNames = tableNames;
        var quotedTableName = $"\"{table.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        var rawRows = await db.Database.SqlQueryRaw<string>(
            $"SELECT COALESCE(json_group_array(json(row_data)), '[]') AS Value FROM (SELECT json_object({string.Join(", ", columnNames.Select((column, index) => $"'{column.Replace("'", "''", StringComparison.Ordinal)}', \"{column.Replace("\"", "\"\"", StringComparison.Ordinal)}\""))}) AS row_data FROM {quotedTableName} LIMIT 200)")
            .FirstOrDefaultAsync() ?? "[]";

        using var document = System.Text.Json.JsonDocument.Parse(rawRows);
        var rows = document.RootElement.EnumerateArray()
            .Select(row => (IReadOnlyList<string>)columnNames.Select(column =>
            {
                if (!row.TryGetProperty(column, out var value) || value.ValueKind == System.Text.Json.JsonValueKind.Null)
                {
                    return "NULL";
                }

                return value.ValueKind == System.Text.Json.JsonValueKind.String
                    ? value.GetString() ?? string.Empty
                    : value.ToString();
            }).ToArray())
            .ToArray();

        return View(new DatabaseTableViewModel(table, columnNames, rows));
    }
}