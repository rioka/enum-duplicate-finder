using EnumDuplicateFinder.Core;
using EnumDuplicateFinder.Plugins.Excel;
using Microsoft.Extensions.Logging;
using Mono.Cecil;

namespace EnumDuplicateFinder.App;

internal class Program
{
  /// <summary>
  /// List of prefixes we want to scan
  /// </summary>
  private static readonly string[] TargetAssemblyPrefixes = new[] {
    "FDM.",
    "FinancialBridge.",
    "MDM.",
    "MediaModule.",
    "OneLogin.",
    "OneStrata.",
    "RDM.",
    "UserStore.",
    "VendorInbox."
  };
  
  /// Selection criteria 
  private static readonly Func<string, bool> AssemblySelector = s => 
    TargetAssemblyPrefixes.FirstOrDefault(a => s.StartsWith(a, StringComparison.OrdinalIgnoreCase)) != null;
  
  public static async Task Main(string[] args)
  {
    // args is expected to be a list of folder to be scanned for enum types
    
    var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    var loader = new AssemblyLoader(loggerFactory.CreateLogger<AssemblyLoader>());
    var assemblies = new List<AssemblyDefinition>();

    foreach (var folder in args)
    {
      assemblies.AddRange(loader.LoadAssembliesInFolder(folder, AssemblySelector));
    }
    
    var detector = new DuplicateDetector(loggerFactory.CreateLogger<DuplicateDetector>());
    var duplicates = detector.FindDuplicatedTypes(assemblies);

    var sanitizer = new Sanitizer(loggerFactory.CreateLogger<Sanitizer>());
    var consolidated = sanitizer.ConsolidateResults(duplicates);

    var exporter = new ExcelExport();
    var saveTo = $"Export_{DateTime.UtcNow:O}.xlsx"
      .Replace("-", "")
      .Replace(":", "");
    await exporter.SaveTo(consolidated, saveTo);
  }
}