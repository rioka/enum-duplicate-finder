using Microsoft.Extensions.Logging;
using Mono.Cecil;

namespace EnumDuplicateFinder.Core;

internal class AssemblyLoader
{
  private readonly ILogger<AssemblyLoader> _logger;
  
  public AssemblyLoader(ILogger<AssemblyLoader> logger)
  {
    _logger = logger;
  }
  
  public IEnumerable<AssemblyDefinition> LoadAssembliesInFolder(string path, Func<string, bool> filter)
  {
    var parameters = BuildParameters(path);
    
    _logger.LogInformation("Loading assemblies in {Folder}", path);
    var assemblies = new List<AssemblyDefinition>();
    foreach (var file in Directory
               .EnumerateFiles(path, "*.dll")
               .Where(f => filter(Path.GetFileName(f))))
    {
      try
      {
        assemblies.Add(AssemblyDefinition.ReadAssembly(file, parameters));
      }
      catch (Exception e) 
        when (e is BadImageFormatException 
              || e is FileLoadException)
      {
        _logger.LogError(e, "Unable to load '{File}'", file);
      }
    }

    _logger.LogInformation("{Count} assemblies loaded from {Folder}", assemblies.Count, path);
    
    return assemblies;
  }

  #region Internals

  private static ReaderParameters BuildParameters(string path)
  {
    var resolver = new DefaultAssemblyResolver();
    resolver.AddSearchDirectory(path);
    
    return new ReaderParameters() {
      AssemblyResolver = resolver
    };
  }

  #endregion
}