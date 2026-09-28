using System.Diagnostics;
using Spectre.Console;

record class Arguments
{
    private Arguments() {}
    public static Arguments Instance { get; private set; } = default!;
    public FileInfo SaveFile { get; init; } = new("save/BrainInAJar.json");
    public bool AllowManualEdit { get; init; } = false;
    public FigletFont FigletFont { get; init; } = FigletFont.Default;
    public static async Task<Arguments> ParseAsync(string[] args)
    => Instance = args switch
    {
        [] => new(),
        ["--help" or "-h", ..] => Help(),
        ["--allow-manual-edit", .. var tail] => await ParseAsync(tail) with { AllowManualEdit = true },
        ["--figlet-font", var path, .. var tail] => await ParseAsync(tail) with { FigletFont = FigletFont.Load(await GetStream(path)) },
        [var path, .. var tail] when Path.GetExtension(path) == ".json" => await ParseAsync(tail) with { SaveFile = new(path) },
        _ => Help(),
    };

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
