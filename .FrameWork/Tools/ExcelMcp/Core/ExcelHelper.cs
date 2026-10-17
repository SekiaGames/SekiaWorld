using OfficeOpenXml;

namespace ET;

public static class ExcelHelper
{
    public static ExcelWorksheet GetWorksheet(ExcelPackage package, int index)
    {
        if (index < 0 || index >= package.Workbook.Worksheets.Count)
            throw new ArgumentException($"Invalid sheet index: {index}. Workbook has {package.Workbook.Worksheets.Count} sheets.");

        return package.Workbook.Worksheets[index];
    }

    public static void SetCellValue(ExcelRange cell, string value)
    {
        var parsed = ArgumentHelper.ParseValue(value);

        if (parsed is double numValue)
            cell.Value = numValue;
        else if (parsed is bool boolValue)
            cell.Value = boolValue;
        else if (parsed is DateTime dateValue)
            cell.Value = dateValue;
        else
            cell.Value = value;
    }
}
