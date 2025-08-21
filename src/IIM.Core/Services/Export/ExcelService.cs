using ClosedXML.Excel;
using IIM.Core.Models;
using IIM.Shared.DTOs;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IIM.Core.Services;

public class ExcelService : IExcelService
{
    private readonly ILogger<ExcelService> _logger;
    
    public ExcelService(ILogger<ExcelService> logger)
    {
        _logger = logger;
    }
    
    public async Task<byte[]> GenerateSpreadsheetAsync(object data, ExportOptions options)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Export");
        
        // Handle different data types
        if (data is InvestigationResponse response && response.DisplayType == ResponseDisplayType.Table)
        {
            await AddTableDataToWorksheet(worksheet, response, options);
        }
        else if (data is IEnumerable<object> enumerable)
        {
            AddEnumerableToWorksheet(worksheet, enumerable);
        }
        else
        {
            // Add as key-value pairs
            AddObjectPropertiesToWorksheet(worksheet, data);
        }
        
        // Apply formatting
        if (options is ExcelExportOptions excelOptions)
        {
            if (excelOptions.AutoFitColumns)
            {
                worksheet.Columns().AdjustToContents();
            }
            
            if (excelOptions.IncludeFilters)
            {
                worksheet.RangeUsed().SetAutoFilter();
            }
            
            if (excelOptions.FreezePanes)
            {
                worksheet.SheetView.FreezeRows(1);
            }
        }
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
    
    public async Task<byte[]> GenerateFromDataTableAsync(DataTable dataTable, ExcelExportOptions options)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(options.SheetName ?? "Data");
        
        // Insert DataTable
        worksheet.Cell(1, 1).InsertTable(dataTable);
        
        // Apply formatting
        if (options.AutoFitColumns)
        {
            worksheet.Columns().AdjustToContents();
        }
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
    
    public async Task<byte[]> GenerateMultiSheetAsync(Dictionary<string, object> sheets, ExportOptions options)
    {
        using var workbook = new XLWorkbook();
        
        foreach (var (sheetName, sheetData) in sheets)
        {
            var worksheet = workbook.Worksheets.Add(sheetName);
            
            if (sheetData is DataTable dt)
            {
                worksheet.Cell(1, 1).InsertTable(dt);
            }
            else if (sheetData is IEnumerable<object> enumerable)
            {
                AddEnumerableToWorksheet(worksheet, enumerable);
            }
            else
            {
                AddObjectPropertiesToWorksheet(worksheet, sheetData);
            }
            
            worksheet.Columns().AdjustToContents();
        }
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
    
    public async Task<byte[]> AddChartAsync(byte[] xlsx, ChartOptions chartOptions)
    {
        // ClosedXML doesn't support charts directly
        // You would need to use EPPlus or similar for chart support
        throw new NotImplementedException("Chart generation requires EPPlus or similar library");
    }
    
    private async Task AddTableDataToWorksheet(IXLWorksheet worksheet, InvestigationResponse response, ExportOptions options)
    {
        // Add metadata if requested
        if (options.IncludeMetadata)
        {
            worksheet.Cell(1, 1).Value = "Response ID:";
            worksheet.Cell(1, 2).Value = response.Id;
            worksheet.Cell(2, 1).Value = "Created:";
            worksheet.Cell(2, 2).Value = response.CreatedAt;
            worksheet.Cell(3, 1).Value = "Confidence:";
            worksheet.Cell(3, 2).Value = response.Confidence;
            
            // Start data at row 5
            var startRow = 5;
            
            if (response.Visualization?.Data != null)
            {
                // Add table data from visualization
                // This would need to handle the specific structure of your table data
                var tableData = response.Visualization.Data;
                // Implementation depends on your data structure
            }
        }
    }
    
    private void AddEnumerableToWorksheet(IXLWorksheet worksheet, IEnumerable<object> data)
    {
        var list = data.ToList();
        if (!list.Any()) return;
        
        // Get properties from first item
        var properties = list.First().GetType().GetProperties();
        
        // Add headers
        for (int i = 0; i < properties.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = properties[i].Name;
        }
        
        // Add data
        int row = 2;
        foreach (var item in list)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                var value = properties[i].GetValue(item);
                worksheet.Cell(row, i + 1).Value = value?.ToString() ?? "";
            }
            row++;
        }
    }
    
    private void AddObjectPropertiesToWorksheet(IXLWorksheet worksheet, object data)
    {
        var properties = data.GetType().GetProperties();
        int row = 1;
        
        foreach (var prop in properties)
        {
            worksheet.Cell(row, 1).Value = prop.Name;
            worksheet.Cell(row, 2).Value = prop.GetValue(data)?.ToString() ?? "";
            row++;
        }
    }

  
}
