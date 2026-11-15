using System.Globalization;

namespace FiveMServerLauncher.ViewModels;

public enum ServerBrowserSort
{
    PlayersDescending,
    PlayersAscending,
    NameAscending,
    NameDescending
}

// Total orders for the browser rows (see .scratch/browser-sorting/spec.md): the main key in the
// chosen direction, then ALWAYS-ascending tie-breakers (players → name → cfx id; name → cfx id),
// so equal rows never shuffle between sorts or refreshes.
//
// Speed at ~33k rows: the culture-aware name work happens once per load (RankNames, off the UI
// thread); every sort then packs one 64-bit key per row and sorts primitive keys, so a sort
// change never compares strings or chases row pointers on the UI thread.
public static class ServerBrowserSorting
{
    private const CompareOptions NameCompareOptions = CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols;

    // Ranks the rows by name: culture-aware (current culture), case-insensitive, and symbols/
    // punctuation/whitespace/emoji ignored so decoration cannot buy a top slot. A name with no
    // letters/digits keys as empty and sorts LAST in both name directions (blank-looking rows
    // would otherwise fill the top of NAME A–Z). Equal names share a NameRank; NameOrdinal is
    // the unique position in (name, cfx id) order. Both are only meaningful within the set
    // ranked together — one load.
    public static void RankNames(IReadOnlyList<ServerBrowserItem> rows)
    {
        var compareInfo = CultureInfo.CurrentCulture.CompareInfo;
        var emptyKey = compareInfo.GetSortKey(string.Empty, NameCompareOptions).KeyData;
        var keys = new byte[rows.Count][];

        for (var i = 0; i < rows.Count; i++)
        {
            keys[i] = compareInfo.GetSortKey(rows[i].Name, NameCompareOptions).KeyData;
            rows[i].HasSortableName = !keys[i].AsSpan().SequenceEqual(emptyKey);
        }

        var order = Enumerable.Range(0, rows.Count).ToArray();
        Array.Sort(order, (x, y) =>
        {
            var sortable = rows[y].HasSortableName.CompareTo(rows[x].HasSortableName);

            if (sortable != 0)
            {
                return sortable;
            }

            var byName = keys[x].AsSpan().SequenceCompareTo(keys[y]);
            return byName != 0 ? byName : string.CompareOrdinal(rows[x].CfxId, rows[y].CfxId);
        });

        var rank = 0;

        for (var i = 0; i < order.Length; i++)
        {
            if (i > 0 && !keys[order[i]].AsSpan().SequenceEqual(keys[order[i - 1]]))
            {
                rank++;
            }

            rows[order[i]].NameRank = rank;
            rows[order[i]].NameOrdinal = i;
        }
    }

    // Rows must have been ranked together by RankNames.
    public static List<ServerBrowserItem> Sort(IEnumerable<ServerBrowserItem> rows, ServerBrowserSort sort)
    {
        var items = rows.ToArray();
        var keys = new long[items.Length];

        for (var i = 0; i < items.Length; i++)
        {
            keys[i] = KeyOf(items[i], sort);
        }

        Array.Sort(keys, items);
        return [.. items];
    }

    // High 32 bits: the main key in the chosen direction. Low 32 bits: the unique name-then-id
    // ordinal, which is exactly the ascending tie-break chain for every sort.
    private static long KeyOf(ServerBrowserItem row, ServerBrowserSort sort)
    {
        long primary = sort switch
        {
            ServerBrowserSort.PlayersDescending => -(long)row.Players,
            ServerBrowserSort.PlayersAscending => row.Players,
            ServerBrowserSort.NameAscending => 0,
            ServerBrowserSort.NameDescending => row.HasSortableName ? -(long)row.NameRank : 1,
            _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, null)
        };

        return (primary << 32) | (uint)row.NameOrdinal;
    }
}
