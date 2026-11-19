using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MiniWms.Api.Tests.Persistence;

/// <summary>Reads SQLite metadata through the context's own connection (kept open until the fixture is disposed).</summary>
internal static class SchemaQuery
{
    public static List<string[]> Run(DbContext context, string sql, params (string Name, object Value)[] parameters)
    {
        context.Database.OpenConnection();
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        using var reader = command.ExecuteReader();
        var rows = new List<string[]>();
        while (reader.Read())
        {
            rows.Add(Enumerable.Range(0, reader.FieldCount)
                .Select(i => Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture) ?? string.Empty)
                .ToArray());
        }

        return rows;
    }
}

/// <summary>TASK-005 AC1: the migrations create the tables of docs/specs/data-model.md (columns, keys, FKs, CHECKs).</summary>
public class SchemaTests
{
    private const string Audit = "CreatedAt|INTEGER|1,CreatedBy|TEXT|1,UpdatedAt|INTEGER|0,UpdatedBy|TEXT|0,Version|INTEGER|1";

    private static readonly Dictionary<string, string> ExpectedColumns = new()
    {
        ["Warehouses"] = $"Id|INTEGER|1,Code|TEXT|1,Name|TEXT|1,IsActive|INTEGER|1,{Audit}",
        ["Zones"] = $"Id|INTEGER|1,WarehouseId|INTEGER|1,Code|TEXT|1,Name|TEXT|1,Type|TEXT|1,IsActive|INTEGER|1,{Audit}",
        ["Bins"] = $"Id|INTEGER|1,ZoneId|INTEGER|1,Code|TEXT|1,Name|TEXT|1,IsActive|INTEGER|1,{Audit}",
        ["Products"] = $"Id|INTEGER|1,Sku|TEXT|1,Name|TEXT|1,UnitOfMeasure|TEXT|1,ReorderLevel|INTEGER|1,IsActive|INTEGER|1,{Audit}",
    };

    public static TheoryData<string> Tables => ["Warehouses", "Zones", "Bins", "Products"];

    [Theory]
    [MemberData(nameof(Tables))]
    [Trait("AC", "TASK-005.1")]
    public void Table_exists_with_exactly_the_columns_types_and_required_flags_of_the_data_model(string table)
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext();

        var rows = SchemaQuery.Run(context, "SELECT name, type, \"notnull\" FROM pragma_table_info($t)", ("$t", table));

        var actual = rows.Select(r => string.Join('|', r)).Order(StringComparer.Ordinal);
        var expected = ExpectedColumns[table].Split(',').Order(StringComparer.Ordinal);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(Tables))]
    [Trait("AC", "TASK-005.1")]
    public void Id_is_the_only_primary_key_column_and_auto_increments(string table)
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext();

        var keys = SchemaQuery.Run(context, "SELECT name FROM pragma_table_info($t) WHERE pk > 0", ("$t", table));
        var sql = SchemaQuery.Run(context, "SELECT sql FROM sqlite_master WHERE name = $t", ("$t", table)).Single()[0];

        Assert.Equal(["Id"], keys.Select(k => k[0]));
        Assert.Contains("\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_" + table + "\" PRIMARY KEY AUTOINCREMENT", sql);
    }

    [Theory]
    [InlineData("Zones", "WarehouseId", "Warehouses")]
    [InlineData("Bins", "ZoneId", "Zones")]
    [Trait("AC", "TASK-005.1")]
    public void Child_table_has_a_foreign_key_to_its_parent_id(string table, string column, string parent)
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext();

        var fks = SchemaQuery.Run(context, "SELECT \"from\", \"table\", \"to\" FROM pragma_foreign_key_list($t)", ("$t", table));

        Assert.Equal([column + "|" + parent + "|Id"], fks.Select(r => string.Join('|', r)));
    }

    [Theory]
    [InlineData("Warehouses")]
    [InlineData("Products")]
    [Trait("AC", "TASK-005.1")]
    public void Root_tables_have_no_foreign_keys(string table)
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext();

        var fks = SchemaQuery.Run(context, "SELECT \"from\" FROM pragma_foreign_key_list($t)", ("$t", table));

        Assert.Empty(fks);
    }

    [Theory]
    [InlineData("Warehouses", "IX_Warehouses_Code", "Code")]
    [InlineData("Zones", "IX_Zones_Code", "Code")]
    [InlineData("Bins", "IX_Bins_Code", "Code")]
    [InlineData("Products", "IX_Products_Sku", "Sku")]
    [Trait("AC", "TASK-005.1")]
    public void Code_columns_have_a_unique_NOCASE_index(string table, string index, string column)
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext();

        var isUnique = SchemaQuery.Run(context, "SELECT \"unique\" FROM pragma_index_list($t) WHERE name = $i", ("$t", table), ("$i", index));
        var keyColumns = SchemaQuery.Run(context, "SELECT name, coll FROM pragma_index_xinfo($i) WHERE key = 1", ("$i", index));

        Assert.Equal(["1"], isUnique.Select(r => r[0]));
        Assert.Equal([column + "|NOCASE"], keyColumns.Select(r => string.Join('|', r)));
    }

    [Theory]
    [InlineData("Warehouses", "CK_Warehouses_Code_Length", "length(\"Code\") <= 20")]
    [InlineData("Warehouses", "CK_Warehouses_Name_Length", "length(\"Name\") <= 100")]
    [InlineData("Zones", "CK_Zones_Code_Length", "length(\"Code\") <= 20")]
    [InlineData("Zones", "CK_Zones_Name_Length", "length(\"Name\") <= 100")]
    [InlineData("Zones", "CK_Zones_Type", "\"Type\" IN ('Receiving', 'Storage', 'Shipping')")]
    [InlineData("Bins", "CK_Bins_Code_Length", "length(\"Code\") <= 30")]
    [InlineData("Bins", "CK_Bins_Name_Length", "length(\"Name\") <= 100")]
    [InlineData("Products", "CK_Products_Sku_Length", "length(\"Sku\") <= 40")]
    [InlineData("Products", "CK_Products_Name_Length", "length(\"Name\") <= 150")]
    [InlineData("Products", "CK_Products_UnitOfMeasure_Length", "length(\"UnitOfMeasure\") <= 20")]
    [InlineData("Products", "CK_Products_ReorderLevel", "\"ReorderLevel\" >= 0")]
    [Trait("AC", "TASK-005.1")]
    public void Table_definition_contains_the_CHECK_constraint(string table, string name, string expression)
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext();

        var sql = SchemaQuery.Run(context, "SELECT sql FROM sqlite_master WHERE name = $t", ("$t", table)).Single()[0];

        Assert.Contains($"CONSTRAINT \"{name}\" CHECK ({expression})", sql);
    }

    [Fact]
    [Trait("AC", "TASK-005.1")]
    public void Over_length_warehouse_code_is_rejected_by_the_CHECK_constraint()
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext();

        var error = Assert.Throws<SqliteException>(() => InsertWarehouse(context, new string('A', 21)));

        Assert.Equal(SqliteConstraint.Check, error.SqliteExtendedErrorCode);
    }

    [Fact]
    [Trait("AC", "TASK-005.1")]
    public void Warehouse_code_at_the_maximum_length_is_accepted()
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext();

        InsertWarehouse(context, new string('A', 20));

        Assert.Equal(1, context.Warehouses.Count());
    }

    [Fact]
    [Trait("AC", "TASK-005.1")]
    public void Over_length_product_sku_is_rejected_by_the_CHECK_constraint()
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext();

        var error = Assert.Throws<SqliteException>(() => InsertProduct(context, new string('S', 41), 0));

        Assert.Equal(SqliteConstraint.Check, error.SqliteExtendedErrorCode);
    }

    [Fact]
    [Trait("AC", "TASK-005.1")]
    public void Negative_reorder_level_is_rejected_by_the_CHECK_constraint()
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext();

        var error = Assert.Throws<SqliteException>(() => InsertProduct(context, "SKU-1", -1));

        Assert.Equal(SqliteConstraint.Check, error.SqliteExtendedErrorCode);
    }

    [Fact]
    [Trait("AC", "TASK-005.1")]
    public void Reorder_level_zero_is_accepted()
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext();

        InsertProduct(context, "SKU-1", 0);

        Assert.Equal(1, context.Products.Count());
    }

    internal static void InsertWarehouse(DbContext context, string code) =>
        context.Database.ExecuteSqlRaw(
            "INSERT INTO Warehouses (Code, Name, IsActive, CreatedAt, CreatedBy, Version) VALUES ({0}, 'W', 1, 0, 'sql', 1)", code);

    internal static void InsertProduct(DbContext context, string sku, int reorderLevel) =>
        context.Database.ExecuteSqlRaw(
            "INSERT INTO Products (Sku, Name, UnitOfMeasure, ReorderLevel, IsActive, CreatedAt, CreatedBy, Version) VALUES ({0}, 'P', 'EA', {1}, 1, 0, 'sql', 1)",
            sku, reorderLevel);
}

/// <summary>SQLite extended result codes for constraint failures.</summary>
internal static class SqliteConstraint
{
    public const int Check = 275;
    public const int ForeignKey = 787;
    public const int Unique = 2067;
}
