
using Microsoft.EntityFrameworkCore;

namespace new_user_app.DbContexts;

/// <summary>
/// Helper class for different naming convention strategies
/// </summary>
public static class NamingConventions
{
    /// <summary>
    /// Configure all entities to use snake_case naming convention
    /// </summary>
    public static void ApplySnakeCaseNaming(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Table names
            var tableName = ToSnakeCase(entityType.GetTableName() ?? entityType.Name);
            entityType.SetTableName(tableName);

            // Column names
            foreach (var property in entityType.GetProperties())
            {
                var columnName = ToSnakeCase(property.GetColumnName() ?? property.Name);
                property.SetColumnName(columnName);
            }

            // Primary key names
            foreach (var key in entityType.GetKeys())
            {
                var keyName = ToSnakeCase(key.GetName() ?? $"PK_{entityType.GetTableName()}");
                key.SetName(keyName);
            }

            // Foreign key names
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                var fkName = ToSnakeCase(foreignKey.GetConstraintName() ?? 
                    $"FK_{entityType.GetTableName()}_{foreignKey.PrincipalEntityType.GetTableName()}");
                foreignKey.SetConstraintName(fkName);
            }

            // Index names
            foreach (var index in entityType.GetIndexes())
            {
                var indexName = ToSnakeCase(index.GetDatabaseName() ?? 
                    $"IX_{entityType.GetTableName()}_{string.Join("_", index.Properties.Select(p => p.Name))}");
                index.SetDatabaseName(indexName);
            }
        }
    }

    /// <summary>
    /// Configure all entities to use lowercase naming convention
    /// </summary>
    public static void ApplyLowercaseNaming(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            entityType.SetTableName(entityType.GetTableName()?.ToLowerInvariant() ?? entityType.Name.ToLowerInvariant());
            
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(property.GetColumnName()?.ToLowerInvariant() ?? property.Name.ToLowerInvariant());
            }
        }
    }

    /// <summary>
    /// Configure all entities to use PascalCase (default EF Core behavior)
    /// </summary>
    public static void ApplyPascalCaseNaming(ModelBuilder modelBuilder)
    {
        // This is the default behavior, so no changes needed
        // But you can use this method to explicitly set PascalCase if needed
    }

    /// <summary>
    /// Convert PascalCase to snake_case
    /// </summary>
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }
}
