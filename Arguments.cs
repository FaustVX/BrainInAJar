using Spectre.Console;

class Arguments
{
    public FileInfo SaveFile { get; init; } = new("save/BrainInAJar.json");
    public bool AllowManualEdit { get; init; } = false;
    public static Arguments Parse(string[] args)
    => args switch
    {
        [] => new(),
        ["--help" or "-h"] => Help(),
        [var path] when Path.GetExtension(path) == ".json" => new() { SaveFile = new(path) },
        ["--allow-manual-edit"] => new() { AllowManualEdit = true},
        [var path, "--allow-manual-edit"] when Path.GetExtension(path) == ".json" => new() { SaveFile = new(path), AllowManualEdit = true},
        ["--allow-manual-edit", var path] when Path.GetExtension(path) == ".json" => new() { SaveFile = new(path), AllowManualEdit = true},
        _ => Help(),
    };

    static Arguments Help()
    {
        AnsiConsole.MarkupLine($"[grey]dotnet {Environment.GetCommandLineArgs()[0]}[/] [[<[blue]save file path[/]>]] [[--allow-manual-edit]] [[-h|--help]]");
        Environment.Exit(0);
        return default!;
    }
}
