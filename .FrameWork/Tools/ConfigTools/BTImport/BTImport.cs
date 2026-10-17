using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using OfficeOpenXml;

namespace ET
{
    public static class BTImport
    {
        private const string JsonPath = "../Config/Excel_TestJsonExport/c/BTConfig.txt";

        private const string ExcelPath = "../Config/Excel/BTConfig.xlsx";

        private const string SheetName = "BTConfig_1";

        private const int FieldStartColumn = 3;

        private const int HeaderRow = 4;

        private const int DataStartRow = 6;

        private const string NodeSourceDir = "../../Scripts/Model/Share/Module/AI/BT";

        private const string NodeSourceSkip = "BTConfigHelper.cs";

        private static readonly string[] FieldNames = { "Id", "TreeId", "NodeId", "ParentId", "Order", "NodeType", "Params" };

        private static readonly string[] FieldDescs = { "行Id", "树Id", "节点Id", "父节点Id", "顺序", "节点类型全名", "参数键值对;分隔" };

        private static readonly string[] FieldTypes = { "int", "int", "int", "int", "int", "string", "string" };

        public static Assembly BuildNodeAssembly()
        {
            List<string> csFiles = new();
            foreach (string csFile in Directory.GetFiles(NodeSourceDir, "*.cs"))
            {
                if (Path.GetFileName(csFile) == NodeSourceSkip)
                {
                    continue;
                }

                csFiles.Add(csFile);
            }

            return DynamicAssembly.Build(csFiles);
        }

        public static void Export()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            if (!File.Exists(JsonPath))
            {
                throw new Exception($"json not found: {Path.GetFullPath(JsonPath)}");
            }

            if (!File.Exists(ExcelPath))
            {
                throw new Exception($"excel not found: {Path.GetFullPath(ExcelPath)}");
            }

            List<BTNodeData> rows = ReadJson(JsonPath);

            List<BTConfigError> errors = BTConfigValidator.Validate(rows);
            if (errors.Count > 0)
            {
                foreach (BTConfigError error in errors)
                {
                    Log.Console($"[BTImport] {error}");
                }

                throw new Exception($"bt config invalid, {errors.Count} error(s), excel not modified");
            }

            using ExcelPackage package = new(new FileInfo(ExcelPath));
            ExcelWorksheet sheet = package.Workbook.Worksheets[SheetName];
            if (sheet == null)
            {
                throw new Exception($"sheet not found: {SheetName}");
            }

            if (IsUpToDate(sheet, rows))
            {
                Log.Console($"BTImport ok: {rows.Count} rows, no change -> {ExcelPath}");
                return;
            }

            EnsureHeader(sheet);
            ClearData(sheet);
            WriteRows(sheet, rows);
            package.Save();

            Log.Console($"BTImport ok: {rows.Count} rows -> {ExcelPath}");
        }

        private static bool IsUpToDate(ExcelWorksheet sheet, List<BTNodeData> rows)
        {
            if (sheet.Dimension == null)
            {
                return false;
            }

            if (sheet.Cells[HeaderRow, FieldStartColumn].Text.Trim() != FieldNames[0])
            {
                return false;
            }

            if (sheet.Dimension.End.Row - DataStartRow + 1 != rows.Count)
            {
                return false;
            }

            for (int i = 0; i < rows.Count; ++i)
            {
                int rowIndex = DataStartRow + i;
                BTNodeData data = rows[i];
                if (CellText(sheet, rowIndex, FieldStartColumn) != (i + 1).ToString()
                        || CellText(sheet, rowIndex, FieldStartColumn + 1) != data.TreeId.ToString()
                        || CellText(sheet, rowIndex, FieldStartColumn + 2) != data.NodeId.ToString()
                        || CellText(sheet, rowIndex, FieldStartColumn + 3) != data.ParentId.ToString()
                        || CellText(sheet, rowIndex, FieldStartColumn + 4) != data.Order.ToString()
                        || CellText(sheet, rowIndex, FieldStartColumn + 5) != data.NodeType
                        || CellText(sheet, rowIndex, FieldStartColumn + 6) != data.Params)
                {
                    return false;
                }
            }

            return true;
        }

        private static string CellText(ExcelWorksheet sheet, int row, int column)
        {
            return sheet.Cells[row, column].Text.Trim();
        }

        private static List<BTNodeData> ReadJson(string path)
        {
            List<BTNodeData> rows = new();
            JsonDocumentOptions options = new() { AllowTrailingCommas = true };
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path), options);
            foreach (JsonElement item in document.RootElement.GetProperty("dict").EnumerateArray())
            {
                JsonElement fields = item[1];
                rows.Add(new BTNodeData
                {
                    TreeId = GetInt(fields, "TreeId"),
                    NodeId = GetInt(fields, "NodeId"),
                    ParentId = GetInt(fields, "ParentId"),
                    Order = GetInt(fields, "Order"),
                    NodeType = GetString(fields, "NodeType"),
                    Params = GetString(fields, "Params"),
                });
            }

            return rows;
        }

        private static int GetInt(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number? value.GetInt32() : 0;
        }

        private static string GetString(JsonElement element, string name)
        {
            if (!element.TryGetProperty(name, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
            {
                return "";
            }

            return value.GetString() ?? "";
        }

        private static void EnsureHeader(ExcelWorksheet sheet)
        {
            if (sheet.Cells[HeaderRow, FieldStartColumn].Text.Trim() == FieldNames[0])
            {
                return;
            }

            for (int i = 0; i < FieldNames.Length; ++i)
            {
                int col = FieldStartColumn + i;
                sheet.Cells[2, col].Value = "cs";
                sheet.Cells[3, col].Value = FieldDescs[i];
                sheet.Cells[HeaderRow, col].Value = FieldNames[i];
                sheet.Cells[5, col].Value = FieldTypes[i];
            }

            sheet.Cells[1, 1].Value = "cs";
        }

        private static void ClearData(ExcelWorksheet sheet)
        {
            if (sheet.Dimension != null && sheet.Dimension.End.Row >= DataStartRow)
            {
                sheet.DeleteRow(DataStartRow, sheet.Dimension.End.Row - DataStartRow + 1);
            }
        }

        private static void WriteRows(ExcelWorksheet sheet, List<BTNodeData> rows)
        {
            int rowIndex = DataStartRow;
            int id = 1;
            foreach (BTNodeData data in rows)
            {
                sheet.Cells[rowIndex, FieldStartColumn].Value = id++;
                sheet.Cells[rowIndex, FieldStartColumn + 1].Value = data.TreeId;
                sheet.Cells[rowIndex, FieldStartColumn + 2].Value = data.NodeId;
                sheet.Cells[rowIndex, FieldStartColumn + 3].Value = data.ParentId;
                sheet.Cells[rowIndex, FieldStartColumn + 4].Value = data.Order;
                sheet.Cells[rowIndex, FieldStartColumn + 5].Value = data.NodeType;
                sheet.Cells[rowIndex, FieldStartColumn + 6].Value = data.Params;
                ++rowIndex;
            }
        }
    }
}
