using System.Windows;
namespace PromptForge.UI;

/// <summary>Swaps a merged resource dictionary (theme or language) at runtime, so no restart is needed.</summary>
public static class ResourceSwitcher
{
    public static void Swap(string folderPrefix, string newSource)
    {
        var merged = Application.Current.Resources.MergedDictionaries;
        var old = merged.FirstOrDefault(d => d.Source is not null && d.Source.OriginalString.StartsWith(folderPrefix));
        if (old is not null) merged.Remove(old);
        merged.Add(new ResourceDictionary { Source = new Uri(newSource, UriKind.Relative) });
    }
}
