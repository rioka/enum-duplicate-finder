using Microsoft.Extensions.Logging;
using Mono.Cecil;

namespace EnumDuplicateFinder.Core;

public class Sanitizer
{
  private readonly ILogger<Sanitizer> _logger;

  public const int MaxDistance = 5; 

  public Sanitizer(ILogger<Sanitizer> logger)
  {
    _logger = logger;
  }

  /// <summary>
  /// Given a map of types and their potential duplicated definitions, try to remove spurious entries
  /// excluding definitions whose base name is not "close enough" to the original one. 
  /// </summary>
  /// <param name="candidates">A map of types and their potential duplicates.</param>
  /// <returns>Another map spurious duplicated types have been removed from.</returns>
  /// <remarks>All logic is hard-coded for now.</remarks>
  public IDictionary<string, (TypeDefinition, TypeDefinition[])> ConsolidateResults(IDictionary<string, (TypeDefinition, IList<TypeDefinition>)> candidates)
  {
    var consolidated = new Dictionary<string, (TypeDefinition, TypeDefinition[])>();
    foreach (var (typeName, (type, matches)) in candidates)
    {
      var simpleName = typeName.Split('.').Last();
      var evaluator = new Fastenshtein.Levenshtein(simpleName);
      var filtered = matches
        .Select(m => (Type: m, Distance: evaluator.DistanceFrom(m.Name)))
        .Where(x => x.Distance <= MaxDistance)
        .ToList();

      if (filtered.Count > 0)
      {
        consolidated[typeName] = (
          type,
          filtered
            .Select(f => f.Type)
            .ToArray());
        _logger.LogInformation("{Type} has {Count} possible duplicate(s): {Duplicates}", 
          typeName, filtered.Count, string.Join(',', filtered.Select(f => f.Type.FullName)));
      }
      else
      {
        _logger.LogInformation("No likely duplicate definition found for {Type} out of {Candidates}", 
          typeName, string.Join(',', matches.Select(m => m.FullName)));
      }
    }

    return consolidated;
  }
}