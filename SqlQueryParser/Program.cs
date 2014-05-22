using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlQueryParser
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    internal static class QueryParser
    {
        /// <summary>
        /// Parses the query and returns the WHERE clause in the query 
        /// The match is done by selecting the table in the from clause or in the join clause of the incoming query
        /// </summary>
        /// <param name="inputQuery">Input Query</param>
        /// <param name="tableName">
        /// Schema table name
        /// <remarks>
        /// This table name should be of the format of [schema].[tablename]
        /// </remarks>
        /// </param>
        /// <returns>The where clause string if found in the matched query, else null</returns>
        internal static string ParseQuery(string inputQuery, string tableName)
        {
            if (string.IsNullOrEmpty(inputQuery) || string.IsNullOrEmpty(tableName))
            {
                return null;
            }

            inputQuery = inputQuery.ToLowerInvariant();
            inputQuery = inputQuery.StartsWith("(") ? inputQuery.TrimStart('(') : inputQuery;
            inputQuery = inputQuery.StartsWith("(") && inputQuery.EndsWith(")") ? inputQuery.TrimEnd(')') : inputQuery;

            int bracketcount = 0;
            StringBuilder buffer = new StringBuilder();

            if (!inputQuery.Contains("("))
            {
                return WhereClauseOfQuery(inputQuery, tableName);
            }

            for (int i = 0; i < inputQuery.Length; i++)
            {
                if (inputQuery[i] == '(')
                {
                    bracketcount++;
                    i++;
                }

                while (bracketcount > 0)
                {
                    if (inputQuery[i] == ')')
                    {
                        bracketcount--;
                    }
                    else if (inputQuery[i] == '(')
                    {
                        bracketcount++;
                    }
                    i++;
                    continue;
                }

                if (i == inputQuery.Length)
                {
                    bool tableNameExists = DoesTableNameExist(inputQuery, tableName);
                    return null;
                }

                buffer.Append(inputQuery[i]);

                if (buffer.ToString().Contains("where"))
                {
                    buffer.Append(inputQuery.Substring(i + 1));
                    return WhereClauseOfQuery(buffer.ToString(), tableName);
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the where clause of the query
        /// </summary>
        /// <param name="incomingQuery">input query</param>
        /// <param name="tableName">schema table name</param>
        /// <returns>Where clause of the query</returns>
        private static string WhereClauseOfQuery(string incomingQuery, string tableName)
        {
            bool tableNameExists = DoesTableNameExist(incomingQuery, tableName);

            return tableNameExists
                ? GetWhereClauseFromInputSQL(incomingQuery)
                   : null;
        }

        /// <summary>
        /// Checks whether the table name exists in the obtained query string
        /// </summary>
        /// <param name="incomingQuery">input query</param>
        /// <param name="tableName">schema table name</param>
        /// <returns>
        /// <c>true</c> if the table name exists
        /// <c>false</c> if the table name does not exist
        /// </returns>
        private static bool DoesTableNameExist(string incomingQuery, string tableName)
        {
            bool tableNameExists = IfTableNamesExistInInputSQL(incomingQuery, tableName);

            if (!tableNameExists && incomingQuery.Contains("where"))
            {
                throw new ArgumentException("invalid query ");
            }
            return tableNameExists;
        }

        /// <summary>
        /// Checks whether there is a where clause in the input sql query
        /// </summary>
        /// <param name="input">input sql query string</param>
        /// <returns>
        /// the substring of the where clause to the end of the input query string 
        /// else, null
        /// </returns>
        private static string GetWhereClauseFromInputSQL(string input)
        {
            return input.Contains("where")
                ? input.Substring(input.IndexOf("where", StringComparison.OrdinalIgnoreCase))
                : null;
        }

        /// <summary>
        /// Checks if the table name exists in the input query for the given table name
        /// </summary>
        /// <param name="input">
        /// input sql query string 
        /// </param>
        /// <param name="tableName">
        /// table name of the table
        /// </param>
        /// <returns>
        /// <c>true</c> if the table name is found
        /// <c>false</c> if the table name is not found
        /// </returns>
        private static bool IfTableNamesExistInInputSQL(string input, string tableName)
        {
            string[] queryParts = Regex.Split(input, @"[\s\r\t]+");

            // I have a join clause to check for the name
            bool toCheckForJoinClause = false;
            if (input.Contains("join"))
            {
                toCheckForJoinClause = true;
            }

            string fromTableName = null, jointableName = null;

            for (int i = 0; i < queryParts.Length; i++)
            {
                if (queryParts[i] == "from")
                {
                    fromTableName = queryParts[i + 1];
                }

                if (toCheckForJoinClause && queryParts[i].Equals("join", StringComparison.OrdinalIgnoreCase))
                {
                    jointableName = queryParts[i + 1];
                }

                if (toCheckForJoinClause)
                {
                    if (!string.IsNullOrEmpty(fromTableName) && !string.IsNullOrEmpty(jointableName))
                        break;
                }
                else
                {
                    if (!string.IsNullOrEmpty(fromTableName))
                        break;
                }
            }

            return toCheckForJoinClause && !string.IsNullOrEmpty(jointableName)
                    ? (tableName.Equals(fromTableName, StringComparison.OrdinalIgnoreCase) || tableName.Equals(jointableName, StringComparison.OrdinalIgnoreCase))
                    : tableName.Equals(fromTableName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
