using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;
using Spectre.Console.Rendering;
using BrainInAJar;
using BrainInAJar.Components;

var arguments = Arguments.Parse(args);

Console.CancelKeyPress += (_, e) =>
{
    Console.ResetColor();
    Console.CursorVisible = true;
};

while (true)
{
    var (brain, history) = GetOrCreateBrain(arguments.SaveFile);
    AnsiConsole.Write(ShowStatus(brain));
    while (brain.Mood is not Mood.Dead)
        switch (AnsiConsole.Prompt(new SelectionPrompt<Selection>()
            .Title("Do what ?")
            .WrapAround()
            .AddChoices([
                ..brain.MoodCheckUnavailable ? Array.Empty<Selection>() : [Selection.MoodCheck],
                ..brain.ActivityUnavailable ? Array.Empty<Selection>() : [Selection.Activity],
                Selection.Status, Selection.History,
                ..(!arguments.AllowManualEdit) ? Array.Empty<Selection>() : [Selection.ManualEntry],
                Selection.Quit])
                .UseConverter(a => a switch
                {
                    Selection.MoodCheck => "Morning mood check",
                    Selection.Activity => "Do activity",
                    Selection.Status => "Show status",
                    Selection.History => $"Show history ({history.Length} deaths, so far ...)",
                    Selection.ManualEntry => "Manual entry",
                    Selection.Quit => "Quit",
                    _ => throw new UnreachableException(),
                })))
        {
            case Selection.MoodCheck:
                brain.MorningMoodCheck();
                if (brain.Mood is Mood.Dead)
                {
                    history = [brain, ..history];
                    brain = new() { Name = AnsiConsole.Ask<string>("Your brain is [red bold]dead[/]. What is your new brain's name ?") };
                }
                AnsiConsole.Write(ShowStatus(brain));
                Save(new(brain, history), arguments.SaveFile);
                break;
            case Selection.Activity:
                foreach (var _ in brain.DoActivity())
                    Save(new(brain, history), arguments.SaveFile);
                Save(new(brain, history), arguments.SaveFile);
                AnsiConsole.Write(ShowStatus(brain));
                break;
            case Selection.Status:
                (brain, history) = GetOrCreateBrain(arguments.SaveFile);
                AnsiConsole.Write(ShowStatus(brain));
                break;
            case Selection.History:
                var list = new Columns(history.Select(ShowStatus));
                AnsiConsole.Write(list);
                break;
            case Selection.ManualEntry:
                (brain, history) = GetOrCreateBrain(arguments.SaveFile);
                while (Edit(brain))
                    Save(new(brain, history), arguments.SaveFile);

                static bool Edit(Brain brain)
                {
                    switch (AnsiConsole.Prompt(new SelectionPrompt<ManualEntry>()
                        .Title("Select which field to edit")
                        .AddChoices(Enum.GetValues<ManualEntry>())
                        .WrapAround()
                        .AddCancelResult((ManualEntry)(-1))))
                    {
                        case ManualEntry.Name:
                            brain.Name = AnsiConsole.Ask("Name", brain.Name);
                            return true;
                        case ManualEntry.Days:
                            brain.Days = AnsiConsole.Ask("Days", brain.Days);
                            return true;
                        case ManualEntry.Mood:
                            GetMood(brain) = AnsiConsole.Prompt(new SelectionPrompt<Mood>()
                                .Title("Mood")
                                .AddChoices(Enum.GetValues<Mood>())
                                .WrapAround()
                                .DefaultValue(brain.Mood));
                            return true;
                            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = $"<{nameof(brain.Mood)}>k__BackingField")]
                            static extern ref Mood GetMood(Brain brain);
                        case ManualEntry.LastActivity:
                            brain.LastActivity = AnsiConsole.Ask("Last Activity", brain.LastActivity);
                            return true;
                        case ManualEntry.LastMoodCheck:
                            brain.LastMoodCheck = AnsiConsole.Ask("LastMoodCheck", brain.LastMoodCheck);
                            return true;
                        case ManualEntry.Breakfast:
                        {
                            (brain.Breakfast, var original) = (AnsiConsole.Confirm("Breakfast", brain.Breakfast), brain.Breakfast);
                            if (brain.Breakfast == original)
                                return true;
                            break;
                        }
                        case ManualEntry.Lunch:
                        {
                            (brain.Lunch, var original) = (AnsiConsole.Confirm("Lunch", brain.Lunch), brain.Lunch);
                            if (brain.Lunch == original)
                                return true;
                            break;
                        }
                        case ManualEntry.Dinner:
                        {
                            (brain.Dinner, var original) = (AnsiConsole.Confirm("Dinner", brain.Dinner), brain.Dinner);
                            if (brain.Dinner == original)
                                return true;
                            break;
                        }
                        case ManualEntry.Plays:
                        {
                            (brain.Plays, var original) = (AnsiConsole.Ask("Plays", brain.Plays), brain.Plays);
                            if (brain.Plays == original)
                                return true;
                            break;
                        }
                        case ManualEntry.Fog:
                        {
                            (brain.Fog, var original) = (AnsiConsole.Ask("Fog", brain.Fog), brain.Fog);
                            if (brain.Fog == original)
                                return true;
                            break;
                        }
                        case ManualEntry.WinRoll:
                            brain.DiceStats.Wins = AnsiConsole.Ask("Win rolls", brain.DiceStats.Wins);
                            return true;
                        case ManualEntry.LosesRoll:
                            brain.DiceStats.Loses = AnsiConsole.Ask("Lose rolls", brain.DiceStats.Loses);
                            return true;
                        case ManualEntry.WinDays:
                            brain.DaysStats.Wins = AnsiConsole.Ask("Win days", brain.DaysStats.Wins);
                            return true;
                        case ManualEntry.LosesDays:
                            brain.DaysStats.Loses = AnsiConsole.Ask("Lose days", brain.DaysStats.Loses);
                            return true;
                        case (ManualEntry)(-1):
                            return false;
                    }
                    brain.LastActivity = DateTime.Now;
                    return true;
                }
                break;
            case Selection.Quit:
                Save(new(brain, history), arguments.SaveFile);
                return;
        }

    static IRenderable ShowStatus(Brain brain)
    {
        var grid = new Grid();
        grid.AddColumns(2);
        grid.AddRow("Created At", brain.CreatedAt.ToString());
        grid.AddRow("Days", $"[{DaysColor(brain.Days)}]{brain.Days}[/]");
        if (brain.Mood is not Mood.Dead)
        {
            grid.AddRow("Current Date Time", DateTime.Now.ToString());
            var lastActivity = DateTime.Now - brain.LastActivity;
            var lastMoodCheck = DateTime.Now - brain.LastMoodCheck;
            grid.AddRow(new Text("Last Activity"), new ProgressBar((float)lastActivity.TotalSeconds, (float)TimeSpan.FromHours(1).TotalSeconds)
            {
                CarretMarkup = $"|{60-(int)Math.Clamp(lastActivity.TotalMinutes, 0, 60)}m|",
                RemainingChar = '>',
            });
            var remainingMoodCheck = TimeSpan.FromDays(1) - lastMoodCheck;
            grid.AddRow(new Text("Last Mood Check"), new ProgressBar((float)lastMoodCheck.TotalMinutes, (float)TimeSpan.FromDays(1).TotalMinutes)
            {
                CarretMarkup = $"|{(int)Math.Clamp(remainingMoodCheck.TotalHours, 0, 24)}h{remainingMoodCheck.Minutes}m|",
                RemainingChar = '>',
            });
            grid.AddRow(new Rule(), new Rule());
            grid.AddRow(new Text("Mood"), new Markup($"[{ToMoodColor(brain)}]{brain.Mood}[/]"));
            if (brain.DayComplete)
                grid.AddRow("Day complete", "[green]True[/]");
            else
            {
                grid.AddRow(new Text("Food"), new Markup($"[{FoodLevelColor(brain)}]{brain.FoodLevel}/3[/]\n[{MealStyle(brain, Meal.Breakfast)}]Breakfast[/]-[{MealStyle(brain, Meal.Lunch)}]Lunch[/]-[{MealStyle(brain, Meal.Dinner)}]Dinner[/]"));
                grid.AddRow(new Text("Plays"), new Markup($"[{PlaysColor(brain)}]{brain.Plays}/2[/]"));
                grid.AddRow(new Text("Fog"), new Markup($"[{FogColor(brain)}]{brain.Fog}/{brain.Days}[/]"));
                grid.AddRow(new Text("Love"), new Markup($"[{LoveColor(brain)}]{brain.Love}/1[/]"));
            }
        }
        grid.AddRow(new Rule(), new Rule());
        if (brain.DiceStats.Total != 0)
            grid.AddRow(new Markup("Dice Ratio"), new ProgressBar(brain.DiceStats.Wins, brain.DiceStats.Total){CarretMarkup=$"{brain.DiceStats.Wins}/{brain.DiceStats.Total} ({brain.DiceStats.Rate:P2})"});
        else
            grid.AddRow("Dice Ratio", "-");
        if (brain.DaysStats.Total != 0)
            grid.AddRow(new Markup("Days Ratio"), new ProgressBar(brain.DaysStats.Wins, brain.DaysStats.Total){CarretMarkup=$"{brain.DaysStats.Wins}/{brain.DaysStats.Total} ({brain.DaysStats.Rate:P2})"});
        else
            grid.AddRow("Days Ratio", "-");
        var outer = new Grid();
        outer.AddColumns(1);
        outer.AddRow(new FigletText(brain.Name));
        outer.AddRow(grid);
        return new Panel(outer).Border(BoxBorder.Beveled);

        static string DaysColor(int days)
        => days switch
        {
            < 6 => "red",
            <= 10 => "darkOrange",
            <= 20 => "blue",
            <= 30 => "green",
            _ => "gold1",
        };

        static string ToMoodColor(Brain brain)
        => brain.Mood switch
        {
            Mood.Dead => "bold red",
            Mood.Awakened => "red",
            Mood.Troubled => "darkOrange",
            Mood.Stable => "blue",
            Mood.Content => "green",
            _ => throw new UnreachableException(),
        };

        static string FoodLevelColor(Brain brain)
        => brain.FoodLevel switch
        {
            0 => "red",
            1 => "darkOrange",
            2 => "blue",
            3 => "green",
            _ => throw new UnreachableException(),
        };

        static string MealStyle(Brain brain, Meal meal)
        => brain[meal] ? "green" :
            Brain.CurrentMeal > meal ? "strikethrough" :
            Brain.CurrentMeal == meal ? "underline" : "default";

        static string PlaysColor(Brain brain)
        => brain.Plays switch
        {
            0 => "red",
            2 => "green",
            _ => "blue",
        };

        static string FogColor(Brain brain)
        => brain.Fog switch
        {
            0 => "green",
            _ => "blue",
        };

        static string LoveColor(Brain brain)
        => brain.Love switch
        {
            0 => "red",
            _ => "green",
        };
    }
}

static Data GetOrCreateBrain(FileInfo saveFile)
{
    try
    {
        Directory.CreateDirectory(saveFile.Directory!.FullName);
        using var stream = saveFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        return JsonSerializer.Deserialize<Data>(stream, new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            AllowTrailingCommas = true,
            IgnoreReadOnlyProperties = true,
            Converters =
            {
                new JsonStringEnumConverter(),
            },
            WriteIndented = true,
        })!;
    }
    catch
    {
        var brain = new Brain() { Name = AnsiConsole.Ask<string>("Brain's name ?", Path.GetFileNameWithoutExtension(saveFile.Name)) };
        var data = new Data(brain, []);
        Save(data, saveFile);
        return data;
    }
}

static void Save(Data data, FileInfo saveFile)
{
    Directory.CreateDirectory(saveFile.Directory!.FullName);
    using var stream = saveFile.Open(FileMode.Create, FileAccess.Write, FileShare.Read);
    JsonSerializer.Serialize(stream, data, new JsonSerializerOptions(JsonSerializerDefaults.General)
    {
        AllowTrailingCommas = true,
        IgnoreReadOnlyProperties = true,
        Converters =
        {
            new JsonStringEnumConverter(),
        },
        WriteIndented = true,
    });
}
