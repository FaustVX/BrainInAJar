using BrainInAJar;
using Spectre.Console;

record class Arguments
{
    private Arguments() {}
    public static Arguments Instance { get; private set; } = default!;
    public FileInfo SaveFile { get; init; } = new("save/BrainInAJar.json");
    public bool AllowManualEdit { get; init; } = false;
    public FigletFont FigletFont { get; init; } = FigletFont.Default;
    public Run? Run { get; init; }

    public static async Task<Arguments> ParseAsync(string[] args)
    => Instance = args switch
    {
        [] => new(),
        ["--help" or "-h", ..] => Help(),
        ["--allow-manual-edit", .. var tail] => await ParseAsync(tail) with { AllowManualEdit = true },
        ["--figlet-font", var path, .. var tail] => await ParseAsync(tail) with { FigletFont = FigletFont.Load(await GetStream(path)) },
        ["run", .. var tail] when ParseRun(tail, out args) is var run => await ParseAsync(args) with { Run = run },
        [var path, .. var tail] when Path.GetExtension(path) == ".json" => await ParseAsync(tail) with { SaveFile = new(path) },
        _ => Help(),
    };

    static Run ParseRun(string[] args, out string[] outer)
    => args switch
    {
        ["mood", .. var tail] => ParseRun(tail, out outer) with { Selection = Selection.MoodCheck },
        ["activity", .. var tail] when ParseActivity(tail, out outer) is {} activity => ParseRun(outer, out outer) with { Selection = Selection.Activity, DoActivity = activity },
        ["status", .. var tail] => ParseRun(tail, out outer) with { Selection = Selection.Status },
        ["history", .. var tail] => ParseRun(tail, out outer) with { Selection = Selection.History },
        ["edit", var command, var value, .. var tail] when ParseEdit(command, value) is var edit => ParseRun(tail, out outer) with { Selection = Selection.ManualEntry, Edit = edit },
        ["edit", .. var tail] when HelpEdit() is var entry => ParseRun(tail, out outer) with { Selection = Selection.ManualEntry, Edit = entry },
        ["quit", .. var tail] => ParseRun(tail, out outer) with { Selection = Selection.Quit },
        var tail => CreateWithRefArgs(tail, out outer),
    };

    static Run CreateWithRefArgs(string[] args, out string[] outer)
    {
        outer = args;
        return new();
    }

    static Edit ParseEdit(string edit, string value)
    => edit switch
    {
        "name" => new(ManualEntry.Name, value),
        "days" when int.TryParse(value, out var days) && days is >= 0 => new(ManualEntry.Days, days),
        "mood" when Enum.TryParse<Mood>(value, out var mood) => new(ManualEntry.Mood, mood),
        "activity" when DateTime.TryParse(value, out var activity) => new(ManualEntry.LastActivity, activity),
        "check" when DateOnly.TryParse(value, out var check) => new(ManualEntry.LastMoodCheck, check),
        "breakfast" when bool.TryParse(value, out var breakfast) => new(ManualEntry.Breakfast, breakfast),
        "lunch" when bool.TryParse(value, out var lunch) => new(ManualEntry.Lunch, lunch),
        "dinner" when bool.TryParse(value, out var dinner) => new(ManualEntry.Dinner, dinner),
        "plays" when int.TryParse(value, out var plays) && plays >= 0 => new(ManualEntry.Plays, plays),
        "fog" when int.TryParse(value, out var fog) && fog >= 0 => new(ManualEntry.Fog, fog),
        "win-roll" when int.TryParse(value, out var winRolls) && winRolls >= 0 => new(ManualEntry.WinRoll, winRolls),
        "loose-roll" when int.TryParse(value, out var looseRolls) && looseRolls >= 0 => new(ManualEntry.LosesRoll, looseRolls),
        "win-days" when int.TryParse(value, out var winDays) && winDays >= 0 => new(ManualEntry.WinDays, winDays),
        "loose-days" when int.TryParse(value, out var looseDays) && looseDays >= 0 => new(ManualEntry.LosesDays, looseDays),
        "--help" or "-h" or _ => HelpEdit(),
    };

    static DoActivity ParseActivity(string[] args, out string[] outer)
    {
        return args switch
        {
            ["eat", .. var tail] => ParseArgs(tail, out outer) with { Activity = Activity.Eat },
            ["play", .. var tail] => ParseArgs(tail, out outer) with { Activity = Activity.Play },
            ["clean", .. var tail] => ParseArgs(tail, out outer) with { Activity = Activity.Clean },
            ["--help" or "-h"] or _ => HelpActivity(out outer),
        };

        static DoActivity ParseArgs(string[] args, out string[] outer)
        {
            if (args is [var a, var b, var c, var d, .. var tail] && ParseDice(a, b, c, d) is {} dice)
            {
                outer = tail;
                return new() { Dice = dice };
            }
            outer = args;
            return new();
        }

        static int[]? ParseDice(string a, string b, string c, string d)
        {
            if (int.TryParse(a, out var d1) && int.TryParse(b, out  var d2) && int.TryParse(c, out var d3) && int.TryParse(d, out var d4)
                && d1 != d2 && d1 != d3 && d1 != d4 && d2 != d3 && d2 != d4 && d3 != d4) // All indices are mutualy exclusive
                return [d1, d2, d3, d4];
            return null;
        }
    }

    static async Task<Stream> GetStream(string path)
    => new FileInfo(path) is { Exists: true, Extension: ".flf" } file ? file.Open(FileMode.Open, FileAccess.Read, FileShare.Read)
        : await new HttpClient().GetStreamAsync(path);

    static Arguments Help()
    {
        AnsiConsole.MarkupLine($"[grey]dotnet {Environment.GetCommandLineArgs()[0]}[/] [[<[blue]save file path[/]>]] [[--allow-manual-edit]] [[--figlet-font <[blue]uri/path[/]>]] [[-h|--help]]");
        Environment.Exit(0);
        throw new System.Diagnostics.UnreachableException();
    }

    static Edit HelpEdit()
    {
        var commands = string.Join(", ", Enumerable.Select([("name", "string"), ("days", "int"), ("mood", "Mood"), ("activity", "datetime"), ("check", "date"), ("breakfast", "bool"), ("lunch", "bool"), ("dinner", "bool"), ("plays", "int"), ("fog", "int"), ("win-roll", "int"), ("loose-roll", "int"), ("win-days", "int"), ("loose-days", "int")], Edit));
        AnsiConsole.MarkupLine($"Available edit: {commands}");
        Environment.Exit(0);
        throw new System.Diagnostics.UnreachableException();

        static string Edit((string command, string type) tuple)
        => $"[blue]{tuple.command}[/]: [blue]{tuple.type}[/]";
    }

    static DoActivity HelpActivity(out string[] outer)
    {
        outer = default!;
        AnsiConsole.MarkupLine($"Available activity: [blue]eat[/]|[blue]play[/]|[blue]clean[/] [[<[blue]d1[/]> <[blue]d2[/]> <[blue]d3[/]> <[blue]d4[/]>]]");
        AnsiConsole.MarkupLine($"Activity dice are zero-based indicies");
        Environment.Exit(0);
        throw new System.Diagnostics.UnreachableException();
    }
}

record class Run
{
    public Selection Selection { get; init; }
    public Edit? Edit { get; init; }
    public DoActivity? DoActivity { get; init; }
}

readonly struct Edit(ManualEntry entry, object value)
{
    public ManualEntry ManualEntry { get; init; } = entry;
    public object Value { get; init; } = value;
}

readonly struct DoActivity
{
    public Activity Activity { get; init; }
    public int[]? Dice { get; init; }
}
