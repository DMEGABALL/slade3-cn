// -----------------------------------------------------------------------------
// Localize.cs - SLADE Chinese localisation helper
//
// Wraps hard-coded UI string literals in slade::i18n::tr(...) so that they can
// be looked up in the external translation table at runtime.
//
// Usage:
//   Localize.exe <source-root> [--apply] [--report <file>]
//
// Without --apply nothing is written; a report of what *would* be wrapped is
// produced instead. The tool is idempotent: literals that are already wrapped
// are skipped, so it can safely be re-run after merging upstream changes.
//
// Build (Windows, .NET Framework 4.x):
//   csc /out:Localize.exe Localize.cs
//
// This program is free software; you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the Free
// Software Foundation; either version 2 of the License, or (at your option)
// any later version.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

internal struct Target
{
    public string Name;
    public int Arg;
    public bool LiteralOnly;

    public Target(string name, int arg, bool literalOnly)
    {
        Name = name;
        Arg = arg;
        LiteralOnly = literalOnly;
    }
}

internal struct Edit
{
    public int Start; // index of first char of the argument
    public int End;   // index just past the last char of the argument
    public string Key;
    public string File;
    public int Line;
}

internal static class Localize
{
    private static readonly Target[] Targets =
    {
        new Target("wxStaticText", 2, true),
        new Target("wxButton", 2, true),
        new Target("wxCheckBox", 2, true),
        new Target("wxRadioButton", 2, true),
        new Target("wxMessageBox", 0, true),
        new Target("wxMessageBox", 1, true),
        new Target("SetLabel", 0, true),
        new Target("SetTitle", 0, true),
        new Target("SetToolTip", 0, true),
        new Target("AddPage", 1, true),
        new Target("AppendSubMenu", 1, true),
        new Target("Append", 1, true),
        new Target("createLabelHBox", 1, true),
        new Target("createLabelVBox", 1, true),
        new Target("createSectionSeparator", 1, true),
    };

    private static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("usage: Localize <source-root> [--apply] [--report <file>]");
            return 2;
        }

        string root = args[0];
        bool apply = false;
        string report = null;

        for (int a = 1; a < args.Length; a++)
        {
            if (args[a] == "--apply")
                apply = true;
            else if (args[a] == "--report" && a + 1 < args.Length)
                report = args[++a];
        }

        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine("not a directory: " + root);
            return 2;
        }

        string[] files = Directory.GetFiles(root, "*.cpp", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        var all = new List<Edit>();
        int changedFiles = 0;

        foreach (string file in files)
        {
            string text = File.ReadAllText(file, new UTF8Encoding(false));
            ScanWrapped(file, text);

            var edits = ProcessFile(file, text);
            if (edits.Count == 0)
                continue;

            all.AddRange(edits);
            if (!apply)
                continue;

            // Apply edits from the end of the file backwards so indices stay valid
            edits.Sort(delegate(Edit x, Edit y) { return y.Start.CompareTo(x.Start); });
            var sb = new StringBuilder(text);
            foreach (var e in edits)
                sb.Insert(e.End, ")").Insert(e.Start, "slade::i18n::tr(");

            File.WriteAllText(file, sb.ToString(), new UTF8Encoding(false));
            changedFiles++;
        }

        Console.WriteLine("files changed: " + changedFiles);
        Console.WriteLine("literals wrapped: " + all.Count);

        var keys = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var e in all)
        {
            if (!keys.ContainsKey(e.Key))
                keys[e.Key] = e.File + ":" + e.Line.ToString(CultureInfo.InvariantCulture);
        }
        foreach (var e in already)
        {
            if (!keys.ContainsKey(e.Key))
                keys[e.Key] = e.File + ":" + e.Line.ToString(CultureInfo.InvariantCulture);
        }

        Console.WriteLine("unique strings: " + keys.Count);

        if (report != null)
        {
            var lines = new List<string>();
            foreach (var kv in keys)
                lines.Add(kv.Key + "\t" + kv.Value);
            lines.Sort(StringComparer.Ordinal);
            File.WriteAllLines(report, lines.ToArray(), new UTF8Encoding(false));
            Console.WriteLine("report written to " + report);
        }

        return 0;
    }

    private static readonly List<Edit> already = new List<Edit>();

    // Collects keys of literals that are already wrapped in i18n::tr(...), so the
    // report covers the whole translated string set and not just new edits.
    private static void ScanWrapped(string file, string text)
    {
        int index = 0;
        while (true)
        {
            index = text.IndexOf("i18n::tr", index, StringComparison.Ordinal);
            if (index < 0)
                return;

            int open = text.IndexOf('(', index);
            if (open < 0)
                return;

            int close = FindMatching(text, open);
            if (close < 0)
                return;

            var args = SplitArgs(text, open, close);
            if (args.Count == 1)
            {
                int start = args[0].Key;
                int end = args[0].Value;
                string key;
                if (start >= 0 && end > start
                    && TryGetLiteral(text.Substring(start, end - start), out key)
                    && key.Length > 0 && HasLetter(key))
                {
                    var edit = new Edit();
                    edit.Key = key;
                    edit.File = file;
                    edit.Line = LineOf(text, start);
                    already.Add(edit);
                }
            }

            index = close;
        }
    }

    private static List<Edit> ProcessFile(string file, string text)
    {
        var edits = new List<Edit>();
        var used = new List<int>();

        foreach (var target in Targets)
        {
            int index = 0;
            while (true)
            {
                index = FindCall(text, target.Name, index);
                if (index < 0)
                    break;

                int open = text.IndexOf('(', index);
                int close = FindMatching(text, open);
                if (close < 0)
                {
                    index = open + 1;
                    continue;
                }

                var args = SplitArgs(text, open, close);
                if (args.Count <= target.Arg)
                {
                    index = open + 1;
                    continue;
                }

                int start = args[target.Arg].Key;
                int end = args[target.Arg].Value;
                index = open + 1;

                if (start < 0 || end <= start)
                    continue;

                if (AlreadyWrapped(text, start))
                    continue;

                // Don't wrap the same argument twice
                if (used.Contains(start))
                    continue;

                string key;
                if (!TryGetLiteral(text.Substring(start, end - start), out key))
                    continue;

                if (key.Length == 0)
                    continue;

                // Skip strings with no letters in them (initial values, numbers,
                // separators, etc.)
                if (!HasLetter(key))
                    continue;

                used.Add(start);
                var edit = new Edit();
                edit.Start = start;
                edit.End = end;
                edit.Key = key;
                edit.File = file;
                edit.Line = LineOf(text, start);
                edits.Add(edit);
            }
        }

        return edits;
    }

    // Finds [name] used as a call (optionally preceded by 'new ', '.', '->' or
    // '::'), starting the search at [from]. Returns the index of the name, or -1.
    private static int FindCall(string text, string name, int from)
    {
        int index = from;
        while (true)
        {
            index = text.IndexOf(name, index, StringComparison.Ordinal);
            if (index < 0)
                return -1;

            int end = index + name.Length;
            if (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_'))
            {
                index = end;
                continue;
            }

            while (end < text.Length && (text[end] == ' ' || text[end] == '\t'))
                end++;

            if (end < text.Length && text[end] == '(')
                return index;

            index = index + name.Length;
        }
    }

    private static int FindMatching(string text, int open)
    {
        if (open < 0)
            return -1;

        int depth = 0;
        for (int a = open; a < text.Length; a++)
        {
            char c = text[a];
            if (c == '"' || c == '\'')
            {
                a = SkipQuoted(text, a);
                if (a < 0)
                    return -1;
                continue;
            }
            if (c == '/' && a + 1 < text.Length && text[a + 1] == '/')
            {
                int e = text.IndexOf('\n', a);
                if (e < 0)
                    return -1;
                a = e;
                continue;
            }
            if (c == '/' && a + 1 < text.Length && text[a + 1] == '*')
            {
                int e = text.IndexOf("*/", a + 2, StringComparison.Ordinal);
                if (e < 0)
                    return -1;
                a = e + 1;
                continue;
            }
            if (c == '(')
                depth++;
            else if (c == ')')
            {
                depth--;
                if (depth == 0)
                    return a;
            }
        }

        return -1;
    }

    // Returns the index of the closing quote of the literal starting at [start]
    private static int SkipQuoted(string text, int start)
    {
        char quote = text[start];
        for (int a = start + 1; a < text.Length; a++)
        {
            if (text[a] == '\\')
            {
                a++;
                continue;
            }
            if (text[a] == quote)
                return a;
            if (text[a] == '\n')
                return -1; // Unterminated
        }
        return -1;
    }

    // Splits the argument list of the call whose '(' is at [open] and whose ')'
    // is at [close] into trimmed (start, end) pairs.
    private static List<KeyValuePair<int, int>> SplitArgs(string text, int open, int close)
    {
        var args = new List<KeyValuePair<int, int>>();
        int depth = 0;
        int argStart = open + 1;

        for (int a = open + 1; a < close; a++)
        {
            char c = text[a];
            if (c == '"' || c == '\'')
            {
                a = SkipQuoted(text, a);
                if (a < 0)
                    break;
                continue;
            }
            if (c == '(' || c == '[' || c == '{')
                depth++;
            else if (c == ')' || c == ']' || c == '}')
                depth--;
            else if (c == ',' && depth == 0)
            {
                args.Add(Trim(text, argStart, a));
                argStart = a + 1;
            }
        }

        args.Add(Trim(text, argStart, close));
        return args;
    }

    private static KeyValuePair<int, int> Trim(string text, int start, int end)
    {
        while (start < end && char.IsWhiteSpace(text[start]))
            start++;
        while (end > start && char.IsWhiteSpace(text[end - 1]))
            end--;
        return new KeyValuePair<int, int>(start, end);
    }

    private static bool AlreadyWrapped(string text, int start)
    {
        int a = start - 1;
        while (a >= 0 && char.IsWhiteSpace(text[a]))
            a--;

        if (a < 2 || text[a] != '(')
            return false;

        a--;
        while (a >= 0 && char.IsWhiteSpace(text[a]))
            a--;

        // Check for a trailing "tr" (i18n::tr / slade::i18n::tr)
        if (a < 1 || text[a] != 'r' || text[a - 1] != 't')
            return false;

        int b = a - 2;
        if (b >= 0 && (char.IsLetterOrDigit(text[b]) || text[b] == '_'))
            return false;

        return true;
    }

    // Checks whether [arg] is made up of nothing but string literals (optionally
    // wrapped in wxS(...)). Returns the concatenated literal contents in [key].
    private static bool TryGetLiteral(string arg, out string key)
    {
        key = null;
        if (arg.Length == 0)
            return false;

        string inner = arg;
        if (inner.StartsWith("wxS(", StringComparison.Ordinal) && inner.EndsWith(")", StringComparison.Ordinal))
            inner = inner.Substring(4, inner.Length - 5);

        var sb = new StringBuilder();
        int a = 0;
        while (a < inner.Length)
        {
            char c = inner[a];
            if (char.IsWhiteSpace(c))
            {
                a++;
                continue;
            }

            if (c != '"')
                return false;

            a++;
            var literal = new StringBuilder();
            while (a < inner.Length && inner[a] != '"')
            {
                char lc = inner[a];

                // Only plain ASCII, no escape sequences, no format specifiers
                if (lc == '\\' || lc == '%' || lc < 0x20 || lc > 0x7E)
                    return false;

                literal.Append(lc);
                a++;
            }

            if (a >= inner.Length)
                return false;

            a++; // closing quote
            sb.Append(literal);
        }

        key = sb.ToString();
        return true;
    }

    private static bool HasLetter(string text)
    {
        for (int a = 0; a < text.Length; a++)
            if ((text[a] >= 'A' && text[a] <= 'Z') || (text[a] >= 'a' && text[a] <= 'z'))
                return true;
        return false;
    }

    private static int LineOf(string text, int index)
    {
        int line = 1;
        for (int a = 0; a < index && a < text.Length; a++)
            if (text[a] == '\n')
                line++;
        return line;
    }
}
