using Dapper;
using System.Data;
using System.Text;

namespace pskrmqtt2db;

public static class ExtensionMethods
{
    public static async Task BulkInsert<T>(this IDbConnection connection, string table, ICollection<string> columns, IList<T> tuples)
    {
        var sb = new StringBuilder($"INSERT INTO {table} (");
        sb.Append(string.Join(", ", columns));
        sb.AppendLine(") VALUES ");

        var type = tuples[0]!.GetType();
        var properties = type.GetProperties();
        var propDict = properties.ToDictionary(k => k.Name, k => k, StringComparer.OrdinalIgnoreCase);

        var dict = new Dictionary<string, object>();
        for (int i = 1; i <= tuples.Count; i++)
        {
            sb.Append("(");
            sb.Append(string.Join(", ", columns.Select(c => "@" + c + i)));

            if (i == tuples.Count)
            {
                sb.AppendLine(")");
                sb.AppendLine("ON DUPLICATE KEY UPDATE seq=values(seq);");
            }
            else
            {
                sb.AppendLine("),");
            }

            var tuple = tuples[i - 1];

            foreach (var col in columns)
            {
                var prop = propDict[col];
                var value = prop!.GetValue(tuple);
                var parameterName = $"@{col}{i}";

                dict.Add(parameterName, value!);
            }
        }

        var parameters = new DynamicParameters(dict);
        var sql = sb.ToString();
        await connection.ExecuteAsync(sql, parameters);
    }
}
