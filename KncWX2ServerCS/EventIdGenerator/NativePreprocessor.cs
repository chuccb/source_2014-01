#nullable enable

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

internal sealed class NativeMacroSet
{
    private readonly Dictionary<string, long> _values = new(StringComparer.Ordinal);

    public bool IsDefined(string name) => _values.ContainsKey(name);

    public long GetValue(string name) => _values.TryGetValue(name, out var value) ? value : 0;

    public void Define(string name, long value = 1) => _values[name] = value;

    public void Undefine(string name) => _values.Remove(name);

    public static NativeMacroSet Load(string path)
    {
        var macros = new NativeMacroSet();

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var separator = line.IndexOf('=');
            if (separator < 0)
            {
                macros.Define(line);
                continue;
            }

            var name = line[..separator].Trim();
            var valueText = line[(separator + 1)..].Trim();
            if (!Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$"))
                throw new InvalidOperationException($"Invalid macro name '{name}' in '{path}'.");

            if (!long.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                throw new InvalidOperationException($"Macro '{name}' has non-numeric value '{valueText}'.");

            macros.Define(name, value);
        }

        return macros;
    }
}

internal sealed class NativePreprocessor
{
    private static readonly Regex DirectiveRegex = new(@"^\s*#\s*(?<directive>[A-Za-z_][A-Za-z0-9_]*)(?:\s+(?<argument>.*?))?\s*$", RegexOptions.Compiled);
    private static readonly Regex IncludeRegex = new(@"^[\"<](.*?)[\">]$", RegexOptions.Compiled);

    private readonly NativeMacroSet _macros;
    private readonly HashSet<string> _includedFiles = new(StringComparer.OrdinalIgnoreCase);

    public NativePreprocessor(NativeMacroSet macros) => _macros = macros;

    public string Process(string path)
    {
        _includedFiles.Clear();
        return ProcessFile(Path.GetFullPath(path));
    }

    private string ProcessFile(string path)
    {
        if (!_includedFiles.Add(path))
            return string.Empty;

        var output = new StringBuilder();
        var conditions = new Stack<ConditionalFrame>();

        foreach (var rawLine in File.ReadLines(path))
        {
            var match = DirectiveRegex.Match(rawLine);
            if (!match.Success)
            {
                if (IsActive(conditions))
                    output.AppendLine(rawLine);
                continue;
            }

            var directive = match.Groups["directive"].Value;
            var argument = match.Groups["argument"].Value.Trim();

            switch (directive)
            {
                case "if":
                    PushCondition(conditions, Evaluate(argument));
                    break;
                case "ifdef":
                    PushCondition(conditions, _macros.IsDefined(argument));
                    break;
                case "ifndef":
                    PushCondition(conditions, !_macros.IsDefined(argument));
                    break;
                case "elif":
                    SwitchElseIf(conditions, Evaluate(argument));
                    break;
                case "else":
                    SwitchElse(conditions);
                    break;
                case "endif":
                    if (conditions.Count == 0)
                        throw new InvalidOperationException($"Unmatched #endif in '{path}'.");
                    conditions.Pop();
                    break;
                case "define":
                    if (IsActive(conditions))
                        Define(argument);
                    break;
                case "undef":
                    if (IsActive(conditions))
                        Undefine(argument);
                    break;
                case "include":
                    if (IsActive(conditions))
                        output.Append(ProcessInclude(path, argument));
                    break;
                case "pragma":
                    break;
                default:
                    if (IsActive(conditions))
                        output.AppendLine(rawLine);
                    break;
            }
        }

        if (conditions.Count != 0)
            throw new InvalidOperationException($"Unclosed preprocessor conditional in '{path}'.");

        return output.ToString();
    }

    private string ProcessInclude(string includingFile, string argument)
    {
        var match = IncludeRegex.Match(argument);
        if (!match.Success)
            throw new InvalidOperationException($"Unsupported #include syntax '{argument}' in '{includingFile}'.");

        var includePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(includingFile)!, match.Groups[1].Value));
        if (!File.Exists(includePath))
            throw new FileNotFoundException($"Native include '{match.Groups[1].Value}' was not found while processing '{includingFile}'.", includePath);

        return ProcessFile(includePath);
    }

    private void Define(string argument)
    {
        var match = Regex.Match(argument, @"^(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\s+(?<value>.*))?$");
        if (!match.Success)
            throw new InvalidOperationException($"Unsupported #define '{argument}'.");

        var name = match.Groups["name"].Value;
        var value = match.Groups["value"].Value.Trim();
        if (value.Length == 0)
        {
            _macros.Define(name);
            return;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericValue))
        {
            _macros.Define(name, numericValue);
            return;
        }

        // Macro text is irrelevant for EventID conditional selection; treat a defined
        // non-numeric macro exactly like a normal C/C++ truthy macro.
        _macros.Define(name);
    }

    private void Undefine(string argument)
    {
        var name = argument.Trim();
        _macros.Undefine(name);
    }

    private static bool IsActive(Stack<ConditionalFrame> conditions) =>
        conditions.Count == 0 || conditions.Peek().IsActive;

    private static void PushCondition(Stack<ConditionalFrame> conditions, bool condition)
    {
        var parentActive = IsActive(conditions);
        conditions.Push(new ConditionalFrame(parentActive, parentActive && condition));
    }

    private static void SwitchElseIf(Stack<ConditionalFrame> conditions, bool condition)
    {
        if (conditions.Count == 0)
            throw new InvalidOperationException("#elif without matching #if.");

        var frame = conditions.Pop();
        if (!frame.ParentActive || frame.AnyBranchTaken)
        {
            conditions.Push(frame with { IsActive = false });
            return;
        }

        conditions.Push(frame with { IsActive = condition, AnyBranchTaken = condition });
    }

    private static void SwitchElse(Stack<ConditionalFrame> conditions)
    {
        if (conditions.Count == 0)
            throw new InvalidOperationException("#else without matching #if.");

        var frame = conditions.Pop();
        conditions.Push(frame with
        {
            IsActive = frame.ParentActive && !frame.AnyBranchTaken,
            AnyBranchTaken = true,
        });
    }

    private bool Evaluate(string expression) => new ExpressionParser(expression, _macros).Parse() != 0;

    private readonly record struct ConditionalFrame(bool ParentActive, bool IsActive, bool AnyBranchTaken = false);

    private sealed class ExpressionParser
    {
        private readonly string _text;
        private readonly NativeMacroSet _macros;
        private int _position;

        public ExpressionParser(string text, NativeMacroSet macros)
        {
            _text = text;
            _macros = macros;
        }

        public long Parse()
        {
            var value = ParseOr();
            SkipWhitespace();
            if (_position != _text.Length)
                throw new InvalidOperationException($"Unsupported native #if expression: '{_text}'.");
            return value;
        }

        private long ParseOr()
        {
            var value = ParseAnd();
            while (TryConsume("||"))
                value = (value != 0 || ParseAnd() != 0) ? 1 : 0;
            return value;
        }

        private long ParseAnd()
        {
            var value = ParseEquality();
            while (TryConsume("&&"))
                value = (value != 0 && ParseEquality() != 0) ? 1 : 0;
            return value;
        }

        private long ParseEquality()
        {
            var value = ParseUnary();
            while (true)
            {
                if (TryConsume("=="))
                {
                    value = value == ParseUnary() ? 1 : 0;
                    continue;
                }

                if (TryConsume("!="))
                {
                    value = value != ParseUnary() ? 1 : 0;
                    continue;
                }

                return value;
            }
        }

        private long ParseUnary()
        {
            SkipWhitespace();
            if (TryConsume("!"))
                return ParseUnary() == 0 ? 1 : 0;

            if (TryConsume("("))
            {
                var value = ParseOr();
                Expect(")");
                return value;
            }

            var token = ReadToken();
            if (token.Length == 0)
                throw new InvalidOperationException($"Expected token in native #if expression '{_text}'.");

            if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                return number;

            if (token.Equals("defined", StringComparison.Ordinal))
            {
                SkipWhitespace();
                string macroName;
                if (TryConsume("("))
                {
                    macroName = ReadToken();
                    Expect(")");
                }
                else
                {
                    macroName = ReadToken();
                }

                return _macros.IsDefined(macroName) ? 1 : 0;
            }

            return _macros.GetValue(token);
        }

        private string ReadToken()
        {
            SkipWhitespace();
            var start = _position;
            while (_position < _text.Length && (char.IsLetterOrDigit(_text[_position]) || _text[_position] == '_'))
                _position++;
            return _text[start.._position];
        }

        private bool TryConsume(string token)
        {
            SkipWhitespace();
            if (!_text.AsSpan(_position).StartsWith(token, StringComparison.Ordinal))
                return false;

            _position += token.Length;
            return true;
        }

        private void Expect(string token)
        {
            if (!TryConsume(token))
                throw new InvalidOperationException($"Expected '{token}' in native #if expression '{_text}'.");
        }

        private void SkipWhitespace()
        {
            while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
                _position++;
        }
    }
}
