using System.Diagnostics;
using Spectre.Console;

readonly struct Arguments()
{
    public FileInfo SaveFile { get; init; } = new("save/BrainInAJar.json");
    public bool AllowManualEdit { get; init; } = false;
    public static Arguments Parse(ReadOnlySpan<string> args)
    => args switch
    {
        [] => new(),
        ["--help" or "-h", ..] => Help(),
        ["--allow-manual-edit", .. var tail] => Parse(tail) with { AllowManualEdit = true },
        [var path, .. var tail] when Path.GetExtension(path) == ".json" => Parse(tail) with { SaveFile = new(path) },
        _ => Help(),
    };

    static Arguments Help()
    {
        AnsiConsole.MarkupLine($"[grey]dotnet {Environment.GetCommandLineArgs()[0]}[/] [[<[blue]save file path[/]>]] [[--allow-manual-edit]] [[-h|--help]]");
        Environment.Exit(0);
        throw new UnreachableException();
    }
}
