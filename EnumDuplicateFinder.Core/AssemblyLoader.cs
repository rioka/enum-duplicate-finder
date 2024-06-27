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
  
  /// <summary>
  /// Scan <paramref name="path"/> for assemblies, and load those who satisfy <paramref name="filter"/>.
  /// </summary>
  /// <param name="path">A path</param>
  /// <param name="filter">
  /// <para>
  /// A predicate.
  /// </para>
  /// <para>Files whose name does not satisfy this predicate are not loaded.</para>
  /// </param>
  /// <returns>A list of <see cref="AssemblyDefinition"/></returns>
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