namespace EnumDuplicateFinder.Core;

public interface IFileExport<in T>
{
  Task SaveTo(T content, string file);
}