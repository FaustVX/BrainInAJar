using System.Diagnostics;
using BrainInAJar;
using Spectre.Console;

record class Arguments
{
    private Arguments() {}
    public static Arguments Instance { get; private set; } = default!;
    public FileInfo SaveFile { get; init; } = new("save/BrainInAJar.json");
    public bool AllowManualEdit { get; init; } = false;
    public FigletFont FigletFont { get; init; } = FigletFont.Default;
    public Run? Run { get; init; } = default!;

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
        ["activity", .. var tail] => ParseRun(tail, out outer) with { Selection = Selection.Activity },
        ["status", .. var tail] => ParseRun(tail, out outer) with { Selection = Selection.Status },
        ["history", .. var tail] => ParseRun(tail, out outer) with { Selection = Selection.History },
        ["edit", .. var tail] => ParseRun(tail, out outer) with { Selection = Selection.ManualEntry },
        ["quit", .. var tail] => ParseRun(tail, out outer) with { Selection = Selection.Quit },
        var tail => CreateWithRefArgs(tail, out outer),
    };

    static Run CreateWithRefArgs(string[] args, out string[] outer)
    {
        outer = args;
        return new();
    }

    static async Task<Stream> GetStream(string path)
    => new FileInfo(path) is { Exists: true, Extension: ".flf" } file ? file.Open(FileMode.Open, FileAccess.Read, FileShare.Read)
        : await new HttpClient().GetStreamAsync(path);

    static Arguments Help()
    {
        AnsiConsole.MarkupLine($"[grey]dotnet {Environment.GetCommandLineArgs()[0]}[/] [[<[blue]save file path[/]>]] [[--allow-manual-edit]] [[--figlet-font <[blue]uri/path[/]>]] [[-h|--help]]");
        Environment.Exit(0);
        throw new UnreachableException();
    }
}

record class Run
{
    public Selection Selection { get; init; }
}
