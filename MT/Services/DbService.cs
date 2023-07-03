using OfficeOpenXml;
using MT.Models;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;

namespace MT.Services
{
    public class DbService
    {
        private Table table;
        private const string _connectionString = "Server=.;initial catalog={0};Integrated Security=SSPI";
        private string tableName;


        public DbService(Table table)
        {
            this.table = table;
            this.tableName = table.tableName.Replace(' ', '_');
        }


        public void CreateTable()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{tableName}' and xtype='U')");
            sb.AppendLine("BEGIN");
            sb.AppendLine($"CREATE TABLE {tableName} (");

            for (int colIdx = 0; colIdx < table.columnCount; colIdx++)
            {
                string type = CheckDataType(1, colIdx);
                string prefix = " ";
                if (colIdx != 0) prefix = ",";

                sb.AppendLine($"{prefix}[{table.columns[colIdx]}] {type}");

            }
            sb.AppendLine(")");
            sb.AppendLine("END");
            ExecCommand(sb.ToString(), GetConnectionString("Test"));            
        }  
        

        public string CheckDataType(int i, int j)
        {
            Type type = table.values[i, j].GetType();

            if (type == typeof(int)) return "INT";
            else if (type == typeof(double)) return "FLOAT(53)";
            else if (type == typeof(float)) return "FLOAT(53)";
            else if (type == typeof(decimal)) return "DECIMAL(2, 2)";
			else if (type == typeof(DateTime)) return "DATETIME";
            else return "NVARCHAR(255)";
		}

        public void TableInsert()
        {
            DataTable tbl = new DataTable();

            for (int colIdx = 0; colIdx < table.columnCount; colIdx++)
            {
                Type type = table.values[1, colIdx].GetType();
                tbl.Columns.Add(new DataColumn(table.columns[colIdx], type));

            }

            for (int i = 0; i < table.rowCount; i++)
            {
                DataRow dr = tbl.NewRow();

                for (int j = 0; j < table.columnCount; j++)
                {
                    if (table.values[i, j] == null) dr[table.columns[j]] = DBNull.Value;
                    else dr[table.columns[j]] = table.values[i, j];

                }
                tbl.Rows.Add(dr);
            }

            string connection = GetConnectionString("Test");
            using (SqlConnection con = new SqlConnection(connection))
            {
                con.Open();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    SqlBulkCopy objbulk = new SqlBulkCopy(con, SqlBulkCopyOptions.KeepIdentity, transaction);
                    objbulk.DestinationTableName = tableName;                    
                    
                    try
                    {
                        objbulk.WriteToServer(tbl);
                        transaction.Commit();
                    }

                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // 0 = DB name
        private const string createDbCmd = @"
        IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '{0}')
        BEGIN
        CREATE DATABASE[{0}]
        END";

        public void CreateDb(string DbName)
        {            
            var cmd = string.Format(createDbCmd, DbName);
            ExecCommand(cmd, GetConnectionString("master"));
        }

        // Helper function to execute Sql commands
        private static void ExecCommand(string queryString, string connectionString) 
        { 
            using (SqlConnection connection = new SqlConnection(connectionString))
            { 
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Connection.Open();
                command.ExecuteNonQuery();
            } 
        }

        public string GetConnectionString(string DbName)
        {
            return string.Format(_connectionString, DbName);            
        }
    }
}

