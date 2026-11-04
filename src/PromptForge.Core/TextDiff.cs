namespace PromptForge.Core;

public enum DiffKind { Same, Added, Removed }
public sealed record DiffLine(DiffKind Kind, string Text);

/// <summary>Line-level diff based on the longest common subsequence.</summary>
public static class TextDiff
{
    public static IReadOnlyList<DiffLine> Compare(string oldText, string newText)
    {
        var a = oldText.Replace("\r\n", "\n").Split('\n');
        var b = newText.Replace("\r\n", "\n").Split('\n');
        var t = new int[a.Length + 1, b.Length + 1];
        for (int i = a.Length - 1; i >= 0; i--)
            for (int j = b.Length - 1; j >= 0; j--)
                t[i, j] = a[i] == b[j] ? t[i + 1, j + 1] + 1 : Math.Max(t[i + 1, j], t[i, j + 1]);
        var res = new List<DiffLine>(); int x = 0, y = 0;
        while (x < a.Length && y < b.Length)
        {
            if (a[x] == b[y]) { res.Add(new(DiffKind.Same, a[x])); x++; y++; }
            else if (t[x + 1, y] >= t[x, y + 1]) res.Add(new(DiffKind.Removed, a[x++]));
            else res.Add(new(DiffKind.Added, b[y++]));
        }
        while (x < a.Length) res.Add(new(DiffKind.Removed, a[x++]));
        while (y < b.Length) res.Add(new(DiffKind.Added, b[y++]));
        return res;
    }
}
