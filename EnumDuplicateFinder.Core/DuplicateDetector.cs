﻿using Microsoft.Extensions.Logging;
using Mono.Cecil;

namespace EnumDuplicateFinder.Core;

public class DuplicateDetector
{
  private readonly ILogger<DuplicateDetector> _logger;

  public DuplicateDetector(ILogger<DuplicateDetector> logger)
  {
    _logger = logger;
  }

  /// <summary>
  /// <para>
  /// Scan <paramref name="assemblies"/> for duplicated <c>enum</c> type definitions.
  /// </para>
  /// <para>
  /// For a type to be identified as a potential duplicate of another, at least two values must match
  /// </para>
  /// </summary>
  /// <param name="assemblies">
  /// <para>
  /// A list of <see cref="AssemblyDefinition"/>.
  /// </para>
  /// <para>This list is scanned for duplicated items, using <c>AssemblyDefinition.MainModule.FileName</c> to identify duplicates.</para>
  /// </param>
  /// <returns>A map of enum type and their potential duplicated definitions.</returns>
  /// <remarks>
  /// All logic is hard-coded now; we cna change this later.
  /// </remarks>
  public IDictionary<string, (TypeDefinition Type , IList<TypeDefinition> Duplicates)> FindDuplicatedTypes(IEnumerable<AssemblyDefinition> assemblies)
  {
    const string GeneratedCodeAttribute = "System.CodeDom.Compiler.GeneratedCodeAttribute";
    
    var assembliesAsList = assemblies.ToList();
    _logger.LogInformation("Scanning {Count} assemblies for potentially duplicated enum types", assembliesAsList.Count);
    
    var duplicates = new Dictionary<string, (TypeDefinition, IList<TypeDefinition>)>();

    var enumTypes = GetUniqueAssemblies(assembliesAsList)
      .SelectMany(a => a.Modules)
      .SelectMany(m => m.Types)
      .Where(t => t.IsPublic)
      .Where(t => t.IsEnum)
      // Ignore compiler-generated code
      .Where(t => t.CustomAttributes.FirstOrDefault(ca => ca.AttributeType.FullName == GeneratedCodeAttribute) == null)
      .ToDictionary(t => t.FullName, t => t);

    _logger.LogInformation("{Count} (public) enum types found", enumTypes.Count);
    
    var valuesByType = enumTypes
      .ToDictionary(kvp => kvp.Key, kvp => GetEnumValues(kvp.Value));

    foreach (var (name, values) in valuesByType)
    {
      var potentialMatches = valuesByType
        .Where(kvp => kvp.Key != name)
        // many types can have a None/unknown/Undefined entry, so we look for at least two matching items 
        .Where(kvp => kvp.Value.Intersect(values, StringComparer.OrdinalIgnoreCase).Count() >= 2)
        .Select(kvp => kvp.Key)
        .ToList();

      if (potentialMatches.Count > 0)
      {
        duplicates[name] = (
          enumTypes[name], 
          potentialMatches
            .Select(typeName => enumTypes[typeName])
            .ToList());
      }
    }

    return duplicates;
  }

  #region Internals

  /// <summary>
  /// Try to detect duplicated assemblies, looking at their filename
  /// </summary>
  /// <param name="assemblies">A list of assemblies</param>
  /// <returns><paramref name="assemblies"/> with duplicated entries removed.</returns>
  private IEnumerable<AssemblyDefinition> GetUniqueAssemblies(IEnumerable<AssemblyDefinition> assemblies)
  {
    return assemblies
      .GroupBy(a => Path.GetFileName(a.MainModule.FileName))
      .Select(a => a.First());
  }

  /// <summary>
  /// Get values defined in <c>enum</c> type <paramref name="type"/>.
  /// </summary>
  /// <param name="type"></param>
  /// <returns>A list of values defined in <paramref name="type"/></returns>
  /// <exception cref="NotSupportedException">Thrown when <paramref name="type"/> is not an <c>enum</c></exception>
  private string[] GetEnumValues(TypeDefinition type)
  {
    if (!type.IsEnum)
    {
      throw new NotSupportedException($"{type.FullName} is not an enum");
    }
    return type.Fields
      .Where(f => f.IsLiteral)
      .Select(f => f.Name)
      .ToArray();
  }

  #endregion
}