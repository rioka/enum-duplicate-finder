using ClosedXML.Excel;
using EnumDuplicateFinder.Core;
using Mono.Cecil;

namespace EnumDuplicateFinder.Plugins.Excel;

public class ExcelExport : IFileExport<IDictionary<string, (TypeDefinition, TypeDefinition[])>>
{
  private static readonly int StartRow = 1;
  private static readonly int StartCol = 1;
  
  public Task SaveTo(IDictionary<string, (TypeDefinition, TypeDefinition[])> content, string file)
  {
    using var wb = new XLWorkbook();
    
    var ws = wb.AddWorksheet("Duplicated enums");
    var lastRow = CreateHeaders(ws);

    foreach (var (key, (enumType, types)) in content)
    {
      SetContent(ws, lastRow, StartCol, key);
      foreach (var type in types)
      {
        SetContent(ws, lastRow, StartCol + 1, type.FullName);
        lastRow++;
      }
    }
    
    wb.SaveAs(file);
    
    return Task.CompletedTask;
  }

  #region Internals

  /// <summary>
  /// Set headers, using reports' names.
  /// </summary>
  /// <param name="ws">A worksheet</param>
  /// <returns>The index of the first row below headers.</returns>
  private int CreateHeaders(IXLWorksheet ws)
  {
    var col = StartCol;
    
    SetContent(ws, StartRow, col, "Type", true);
    SetContent(ws, StartRow, ++col, "Possible duplicate definitions", true);

    return StartRow + 1;
  }

  /// <summary>
  ///   Set the content of cell at row <paramref name="row"/> and col <param name="col"></param> to <paramref name="text"/>.
  ///   Optionally force font to bold.
  /// </summary>
  /// <param name="ws">A worksheet</param>
  /// <param name="row">The row index of the cell</param>
  /// <param name="col">The column index of the cell</param>
  /// <param name="text">The content</param>
  /// <param name="bold">Flag to force bold</param>
  /// <returns>The cell</returns>
  private IXLCell SetContent(IXLWorksheet ws, int row, int col, string text, bool bold = false)
  {
    var cell = ws.Cell(row, col); 
    cell.Value = text;
    if (bold)
    {
      cell.Style.Font.Bold = true;
    }
    return cell;
  }
  
  #endregion
}
