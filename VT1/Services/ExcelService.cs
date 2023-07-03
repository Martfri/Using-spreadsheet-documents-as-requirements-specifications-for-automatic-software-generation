using OfficeOpenXml;
using MT.Models;

namespace MT.Services
{

    public interface IExcelService
    {
        List<Table> FFinalTables { get; set; }
    }

    class ExcelService : IExcelService
    {
        private ExcelPackage Excelpackage;
        private ExcelWorksheets wss;
        private List<TableName> tables = new List<TableName>();
        private List<Column> columns = new List<Column>();
        private List<Entry> dataEntries = new List<Entry>();
        private int tableCounter = 0;
        public List<Table> FFinalTables { get; set; }
        public List<dynamic> models = new List<dynamic>();

        private static string filePath = ".\\wwwroot\\Uploads\\Test.xlsx";


        public ExcelService()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            ExcelPackage sss = new ExcelPackage(filePath);
            wss = sss.Workbook.Worksheets;
            var table = TableDetection();
            MapToTable(table);
        }


        public List<TableName> TableDetection()
        {
            foreach (var ws in wss)
            {
                int colCount;
                int rowCount;

                try
                {
                    //get column count of document
                    colCount = ws.Dimension.End.Column;
                    //get row count of document
                    rowCount = ws.Dimension.End.Row;
                }

                catch
                {
                    colCount = 0;
                    rowCount = 0;
                }


                for (int row = rowCount; row >= 1; row--)
                {
                    for (int col = colCount; col >= 1; col--)
                    {
                        TableRecognition(row, col, ws);
                    }
                }
            }

            return tables;
        }

        public List<TableName> TableRecognition(int i, int j, ExcelWorksheet ews)
        {

            if (ews.Cells[i, j].Value == null) return null;

            else if (IsDataCell(i, j, ews) || ews.Cells[i, j].Value is "n/a" or "N/A")
            {
                // Add new entry (N/A)
                if (ews.Cells[i, j].Value is "n/a" or "N/A")
                {
                    var na = new Entry();
                    na.i = i;
                    na.j = j;
                    na.value = null;
                    dataEntries.Add(na);
                    na.tableCount = tableCounter;

                }

                // Add new entry
                else
                {
                    var data = new Entry();
                    data.i = i;
                    data.j = j;
                    data.value = ews.Cells[i, j].Value;
                    dataEntries.Add(data);
                    data.tableCount = tableCounter;
                }

            }

            // Add new header
            else if (IsHeaderCell(i, j, ews))
            {
                var column = new Column();
                column.tableCount = tableCounter;
                column.count = j;
                column.Name = ews.Cells[i, j].Value.ToString();
                column.entries = dataEntries.Where(p => p.j == j && p.tableCount == tableCounter).ToList();
                columns.Add(column);
            }

            // Add new title
            else if (IsTitleCell(i, j, ews))
            {
                var title = new TableName();
                title.tableCount = tableCounter;
                title.name = ews.Cells[i, j].Value.ToString();
                title.columns = columns.Where(C => C.tableCount == tableCounter).ToList();
                tableCounter++;
                tables.Add(title);
            }

            else
            {
                return null;
            }

            return tables;

        }

        public bool IsTitleCell(int i, int j, ExcelWorksheet ews)
        {
            // Cell is type of string and there is a seperator
            if (ews.Cells[i, j].Value.GetType() == typeof(string) && columns.Where(c => c.tableCount == tableCounter).Count() != 0 && ews.Cells[i + 1, j].Value == null)
            {
                return true;
            }

            // A a title cell has already been found and in the last row scanned, a title cell has been detected
            else if (tables.Where(t => t.tableCount == tableCounter).Count() != 0 && (IsTitleCell(i + 1, j, ews) || IsTitleCell(i + 1, j - 1, ews) || IsTitleCell(i + 1, j + 1, ews)))
            {
                return true;
            }

            // A header cell has been found, C[i,j-1] is empty, C[i,j+1] empty and j is the table’s first column
            else if (columns.Where(c => c.tableCount == tableCounter).Count() != 0 && ews.Cells[i, j + 1].Value == null && ews.Cells[i, j - 1].Value == null && columns.Where(c => c.tableCount == tableCounter).First().count == columns.Where(c => c.tableCount == tableCounter).MinBy(c => c.count).count)
            {
                return true;
            }

            else
            {
                return false;
            }
        }

        public bool IsHeaderCell(int i, int j, ExcelWorksheet ews)
        {
            if (ews.Cells[i, j].Value == null) return false;

            // Cell has type string and row below is not null
            else if (columns.Where(c => c.tableCount == tableCounter).Count() != 0 && ews.Cells[i, j].Value.GetType() == typeof(string) && ews.Cells[i + 1, j].Value != null)
            {
                return true;
            }

            // Cell or neighbour cells have borders and row below is not null
            else if (HasBorders(i, j, ews) && (HasBorders(i, j + 1, ews) || HasBorders(i, j - 1, ews)) && ews.Cells[i + 1, j].Value != null)
            {
                return true;
            }

            // Cell and neighbour cells have borders and row below is not null
            else if (HaveSimilarFormat(i, j, i, j - 1, ews) && HaveSimilarFormat(i, j, i, j + 1, ews) && ews.Cells[i + 1, j].Value != null)
            {
                return true;
            }

            // Rules for similar formatting between two consecutive cells and to distinguish headers from data entries
            else if (ews.Cells[i + 1, j].Value != null && ews.Cells[i + 1, j].Value.GetType() != typeof(string) && ews.Cells[i, j].Value.GetType() == typeof(string) && !HaveSimilarFormat(i, j, i + 1, j, ews) && ews.Cells[i, j - 1].Value != null && HaveSimilarFormat(i, j, i, j - 1, ews))
            {
                return true;
            }

            else
            {
                return false;
            }
        }

        public bool IsDataCell(int i, int j, ExcelWorksheet ews)
        {
            if (HaveSimilarFormat(i, j, i - 1, j, ews) && HaveSimilarFormat(i, j, i + 1, j, ews))
            {
                return true;
            }

            else if (HaveSimilarFormat(i, j, i - 1, j, ews) && ews.Cells[i + 1, j].Value == null)
            {
                return true;
            }

            else if (HaveSimilarFormat(i, j, i + 1, j, ews) && IsHeaderCell(i - 1, j, ews) == true)
            {
                return true;
            }

            else
            {
                return false;
            }
        }

        public bool HaveSimilarFormat(int i, int j, int ii, int jj, ExcelWorksheet ws)
        {
            if (ii == 0 || jj == 0 || ws.Cells[ii, jj].Value == null) return false;


            else if (ws.Cells[i, j].Style.Font.Size == ws.Cells[ii, jj].Style.Font.Size && ws.Cells[i, j].Style.Font.Color.Tint == ws.Cells[ii, jj].Style.Font.Color.Tint)
            {
                return true;
            }

            else
            {
                return false;
            }
        }

        public bool HasBorders(int i, int j, ExcelWorksheet ws)
        {
            if (i == 0 || j == 0 || ws.Cells[i, j].Value == null) return false;

            else if (ws.Cells[i, j].Style.Border.Top != null || ws.Cells[i, j].Style.Border.Bottom != null || ws.Cells[i, j].Style.Border.Left != null || ws.Cells[i, j].Style.Border.Right != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Mapping from column-based model to row-based
        public List<Table> MapToTable(List<TableName> tableNames)
        {
            List<Table> finalTables = new List<Table>();

            foreach (TableName t in tableNames)
            {

                int colCount = t.columns.Count;
                int rowCount = t.columns.Select(x => x.entries.Count).Max();

                Table table = new Table();
                table.tableName = t.name;
                table.columns = new string[colCount];
                table.values = new object[rowCount, colCount];
                table.columnCount = colCount;
                table.rowCount = rowCount;

                var columns = t.columns.OrderBy(x => x.count).ToList();
                for (int colIdx = 0; colIdx < columns.Count; colIdx++)
                {
                    var col = columns[colIdx];
                    table.columns[colIdx] = col.Name;

                    for (int rowIdx = 0; rowIdx < col.entries.Count; rowIdx++)
                    {
                        var entry = col.entries[rowIdx];
                        table.values[rowIdx, colIdx] = entry.value;
                    }
                }
                finalTables.Add(table);
            }
            FFinalTables = finalTables;
            return finalTables;
        }
    }
}



