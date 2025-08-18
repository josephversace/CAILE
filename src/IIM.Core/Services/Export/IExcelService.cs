using IIM.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IIM.Core.Services;

public interface IExcelService
{
    Task<byte[]> GenerateSpreadsheetAsync(object data, ExportOptions options);
    Task<byte[]> GenerateFromDataTableAsync(System.Data.DataTable dataTable, ExcelExportOptions options);
    Task<byte[]> GenerateMultiSheetAsync(Dictionary<string, object> sheets, ExportOptions options);
    Task<byte[]> AddChartAsync(byte[] xlsx, ChartOptions chartOptions);
}

public record ExcelExportOptions : ExportOptions
{
    public bool AutoFitColumns { get; set; } = true;
    public bool IncludeFilters { get; set; } = true;
    public bool FreezePanes { get; set; } = true;
    public string? SheetName { get; set; }
}

public class ChartOptions
{
    public string ChartType { get; set; } = "Column";
    public string DataRange { get; set; } = "";
    public string Title { get; set; } = "";
    public string? XAxisTitle { get; set; }
    public string? YAxisTitle { get; set; }
}
