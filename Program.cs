using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;
using Spectre.Console.Rendering;

while (true)
{
    var (brain, history) = GetOrCreateBrain();
    AnsiConsole.Write(ShowStatus(brain));
    while (brain.Mood is not Mood.Dead)
        switch (AnsiConsole.Prompt(new SelectionPrompt<Action>()
            .Title("Do what ?")
            .WrapAround()
            .AddChoices([
                ..brain.MoodCheckUnavailable ? Array.Empty<Action>() : [Action.MoodCheck],
                ..brain.ActivityUnavailable ? Array.Empty<Action>() : [Action.Activity],
                Action.Status, Action.History, Action.ManualEntry, Action.Quit])
                .UseConverter(a => a switch
                {
                    Action.MoodCheck => "Morning mood check",
                    Action.Activity => "Do activity",
                    Action.Status => "Show status",
                    Action.History => $"Show history ({history.Length} deaths, so far ...)",
                    Action.ManualEntry => "Manual entry",
                    Action.Quit => "Quit",
                    _ => throw new UnreachableException(),
                })
                .DefaultValue(Action.Status)))
        {
            case Action.MoodCheck:
                brain.MorningMoodCheck();
                if (brain.Mood is Mood.Dead)
                {
                    history = [brain, ..history];
                    brain = new() { Name = AnsiConsole.Ask<string>("Your brain is [red bold]dead[/]. What is your new brain's name ?") };
                }
                AnsiConsole.Write(ShowStatus(brain));
                Save(new(brain, history));
                break;
            case Action.Activity:
                brain.DoActivity();
                AnsiConsole.Write(ShowStatus(brain));
                Save(new(brain, history));
                break;
            case Action.Status:
                (brain, history) = GetOrCreateBrain();
                AnsiConsole.Write(ShowStatus(brain));
                break;
            case Action.History:
                var list = new Columns(history.Select(ShowStatus));
                AnsiConsole.Write(list);
                break;
            case Action.ManualEntry:
                (brain, history) = GetOrCreateBrain();
                while (Edit(brain))
                    Save(new(brain, history));

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
            case Action.Quit:
                Save(new(brain, history));
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
            grid.AddRow(new Text("Last Activity"), new Markup($"[{(lastActivity.TotalHours >= 1 ? "green" : "red")}]{lastActivity}[/]"));
            grid.AddRow(new Text("Last Mood Check"), new Markup($"[{LastMoodColor(brain)}]{DateTime.Now - brain.LastMoodCheck}[/]"));
            grid.AddRow(new Rule(), new Rule());
            grid.AddRow(new Text("Mood"), new Markup($"[{ToMoodColor(brain)}]{brain.Mood}[/]"));
            if (brain.DayComplete)
                grid.AddRow("Day complete", "[green]true[/]");
            else
            {
                grid.AddRow(new Text("Food"), new Markup($"[{FoodLevelColor(brain)}]{brain.FoodLevel}/3[/]\n[{MealStyle(brain, Meal.Breakfast)}]Breakfast[/]-[{MealStyle(brain, Meal.Lunch)}]Lunch[/]-[{MealStyle(brain, Meal.Dinner)}]Dinner[/]"));
                grid.AddRow(new Text("Plays"), new Markup($"[{PlaysColor(brain)}]{brain.Plays}/2[/]"));
                grid.AddRow(new Text("Fog"), new Markup($"[{FogColor(brain)}]{brain.Fog}/{brain.Days}[/]"));
                grid.AddRow(new Text("Love"), new Markup($"[{LoveColor(brain)}]{brain.Love}/1[/]"));
            }
        }
        grid.AddRow(new Rule(), new Rule());
        grid.AddRow("Dice Ratio", $"{brain.DiceStats.Wins}/{brain.DiceStats.Total} ({brain.DiceStats.Rate:P}%)");
        grid.AddRow("Days Ratio", $"{brain.DaysStats.Wins}/{brain.DaysStats.Total} ({brain.DaysStats.Rate:P}%)");
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

        static string LastMoodColor(Brain brain)
        => TimeOnly.FromDateTime(DateTime.Now) >= new TimeOnly(12, 0) || DateOnly.FromDateTime(DateTime.Now) <= DateOnly.FromDateTime(brain.LastMoodCheck) ? "on" : "green";

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
            Brain.CurrentMeal > meal ? "strikethrough": "on";

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

static Data GetOrCreateBrain()
{
    try
    {
        Directory.CreateDirectory("save");
        using var stream = File.Open("save/BrainInAJar.json", FileMode.Open, FileAccess.Read, FileShare.Read);
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
        var brain = new Brain() { Name = AnsiConsole.Ask<string>("Brain's name ?") };
        var data = new Data(brain, []);
        Save(data);
        return data;
    }
}

static void Save(Data data)
{
    Directory.CreateDirectory("save");
    using var stream = File.Open("save/BrainInAJar.json", FileMode.Create, FileAccess.Write, FileShare.Read);
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

public class Brain
{
    public required string Name { get; set; }
    public int Days { get; set; }
    public DateOnly CreatedAt { get; init; } = DateOnly.FromDateTime(DateTime.Now);
    public DateTime LastActivity { get; set; } = DateTime.Now.AddHours(-1);
    public bool ActivityUnavailable => DateTime.Now.AddHours(-1) < LastActivity || this[CurrentMeal] && Fog <= 0 && (Mood is <= Mood.Awakened || Plays >= 2);
    public DateTime LastMoodCheck { get; set; } = DateTime.Now;
    public bool MoodCheckUnavailable => TimeOnly.FromDateTime(DateTime.Now) >= new TimeOnly(12, 0) || DateOnly.FromDateTime(DateTime.Now) <= DateOnly.FromDateTime(LastMoodCheck);
    public Mood Mood
    {
        get; set
        {
            if (value is Mood.Dead)
                field = value;
            else if (value < Mood.Awakened || field is Mood.Awakened)
                field = Mood.Awakened;
            else if (value > Mood.Content)
                field = Mood.Content;
            else
                field = value;
        }
    } = Mood.Content;
    public bool Breakfast { get; set; }
    public bool Lunch { get; set; }
    public bool Dinner { get; set; }
    public int FoodLevel => (Breakfast ? 1 : 0) + (Lunch ? 1 : 0) + (Dinner ? 1 : 0);
    public static Meal CurrentMeal => TimeOnly.FromDateTime(DateTime.Now) switch
    {
        { Hour: < 12 } => Meal.Breakfast,
        { Hour: >= 18 } => Meal.Dinner,
        _ => Meal.Lunch,
    };
    public bool this[Meal meal]
    {
        get => meal switch {
            Meal.Breakfast => Breakfast,
            Meal.Lunch => Lunch,
            Meal.Dinner => Dinner,
            _ => throw new UnreachableException(),
        };
        set
        {
            switch (meal)
            {
                case Meal.Breakfast:
                    Breakfast = value;
                    break;
                case Meal.Lunch:
                    Lunch = value;
                    break;
                case Meal.Dinner:
                    Dinner = value;
                    break;
                default:
                    throw new UnreachableException();
            }
        }
    }
    public int Plays { get; set; }
    public int Fog { get; set; }
    public int Love => DayComplete ? 1 : 0;
    public bool DayComplete => FoodLevel == 3 && Plays == 2 && Fog == 0;
    public int Care => FoodLevel + Plays + Love;
    public Stats DiceStats { get; init; } = new();
    public Stats DaysStats { get; init; } = new();

    public void MorningMoodCheck()
    {
        var errors = (morning: false, sameDay: false);
        if (TimeOnly.FromDateTime(DateTime.Now) >= new TimeOnly(12, 0))
            errors.morning = true;
        if (DateOnly.FromDateTime(DateTime.Now) <= DateOnly.FromDateTime(LastMoodCheck))
            errors.sameDay = true;
        if (errors is not (false, false))
        {
            if (errors.morning)
                AnsiConsole.MarkupLine("[red]You should check only in the [italic blue]morning[/], come back later[/]");
            if (errors.sameDay)
                AnsiConsole.MarkupLine("[red]You already checked the morning mood today, come back tomorrow[/]");
            return;
        }

        var roll1 = Random.Shared.Next(1, 7);
        var roll2 = Random.Shared.Next(1, 7);
        switch (roll1, roll2)
        {
            case (6, 6) when Mood is Mood.Awakened:
                AnsiConsole.MarkupLineInterpolated($"Hooray you rolled 2 [green]6[/]'s, but unfortunately you are [red]{Mood}[/], nothing changes");
                break;
            case (6, 6):
                Mood += 2;
                AnsiConsole.MarkupLineInterpolated($"Hooray you rolled 2 [green]6[/]'s, your mood goes up by [green]2[/] to [green]{Mood}[/]");
                break;
            case (1, 1) when Mood is Mood.Awakened:
                AnsiConsole.MarkupLineInterpolated($"Unfortunately you rolled 2 [red]1[/]'s, and also you are already [red]{Mood}[/], you [red bold]die[/]");
                Mood = Mood.Dead;
                return;
            case (1, 1):
                Mood -= 2;
                AnsiConsole.MarkupLineInterpolated($"Unfortunately you rolled 2 [red]1[/]'s, your mood goes down by [red]2[/] to [red]{Mood}[/]");
                break;
            default:
                var total = roll1 + roll2 + Care - Fog;
                AnsiConsole.MarkupLineInterpolated($"Your morning mood check rolls are [green]{roll1}[/] + [green]{roll2}[/] + (Care)[blue]{Care}[/] - (Fog)[red]{Fog}[/] = [blue]{total}[/]");
                switch (total)
                {
                    case >= 9 when Mood is Mood.Awakened:
                        AnsiConsole.MarkupLineInterpolated($"You are [red]{Mood}[/], nothing changes");
                        break;
                    case >= 9:
                        Mood += 1;
                        AnsiConsole.MarkupLineInterpolated($"You mood goes up by [green]1[/] to [green]{Mood}[/]");
                        break;
                    case <= 5 when Mood is Mood.Awakened:
                        AnsiConsole.MarkupLineInterpolated($"You mood goes down by [red]1[/] but you are already [red]{Mood}[/], you [red bold]die[/]");
                        Mood = Mood.Dead;
                        return;
                    case <= 5:
                        Mood -= 1;
                        AnsiConsole.MarkupLineInterpolated($"You mood goes down by [red]1[/] to [red]{Mood}[/]");
                        break;
                    default:
                        AnsiConsole.MarkupLineInterpolated($"Your mood doesn't change at all");
                        break;
                }
                break;
        }
        if (DayComplete)
            DaysStats.Wins++;
        else
            DaysStats.Loses++;
        LastMoodCheck = DateTime.Now;
        Breakfast = Lunch = Dinner = false;
        Plays = 0;
        Fog = Days += 1;
    }

    public void DoActivity()
    {
        var errors = (wait1hour: false, nothingToDo: false);
        if (DateTime.Now.AddHours(-1) < LastActivity)
            errors.wait1hour = true;
        if (this[CurrentMeal] && Fog <= 0 && (Mood is <= Mood.Awakened || Plays >= 2))
            errors.nothingToDo = true;
        if (errors is not (false, false))
        {
            if (errors.wait1hour)
                AnsiConsole.MarkupLineInterpolated($"[red]You should wait at least [italic blue]1 hour[/] after the last activity tried[/] ([blue]{LastActivity - DateTime.Now.AddHours(-1)}[/] remaining)");
            if (errors.nothingToDo)
                AnsiConsole.MarkupLine("[red]You have nothing to do today, come back later[/]");
            return;
        }
        ReselectActivity:
        var activity = AnsiConsole.Prompt(new SelectionPrompt<Activity>()
            .Title("Select the activity")
            .WrapAround()
            .AddCancelResult((Activity)(-1))
            .AddChoices([
                ..this[CurrentMeal] ? Array.Empty<Activity>() : [Activity.Eat],
                ..Fog <= 0 ? Array.Empty<Activity>() : [Activity.Clean],
                ..Mood is <= Mood.Awakened || Plays >= 2 ? Array.Empty<Activity>() : [Activity.Play]])
                .UseConverter(a => a is Activity.Eat ? $"Eat {CurrentMeal}" : a.ToString()));
        if (activity is (Activity)(-1))
            return;
        var numDice = Mood switch
        {
            Mood.Content => 6,
            Mood.Stable => 5,
            Mood.Troubled or Mood.Awakened => 4,
            _ => throw new UnreachableException(),
        };
        AfterClean:
        AnsiConsole.MarkupLine($"You will play the dice game with [underline italic blue]{Name}[/] to [green]{(activity is Activity.Eat ? $"Eat {CurrentMeal}" : activity)}[/] with [green]{numDice}[/] dice because you are [underline italic blue]{Mood}[/]");
        var dice = GetRandom(activity).GetItems([1, 2, 3, 4, 5, 6], numDice);
        AnsiConsole.MarkupLine("your dice: [green]" + Ext.Join("[/], [green]", "[/] and [green]", dice) + "[/]");
        ReselectDice:
        var die1 = AnsiConsole.Prompt(new SelectionPrompt<(int i, int d)>()
            .Title($"Select [blue]1st[/] die")
            .AddChoices(dice.Index())
            .AddCancelResult((0, 0))
            .WrapAround()
            .UseConverter(t => t.Item2.ToString()));
        if (die1 is (0, 0))
            goto ReselectActivity;
        var die2 = AnsiConsole.Prompt(new SelectionPrompt<(int i, int d)>()
            .Title($"Select [blue]2nd[/] die ([green]{die1.Item2}[/])")
            .AddChoices(dice.Index().Except([die1]))
            .AddCancelResult((0, 0))
            .WrapAround()
            .UseConverter(t => t.Item2.ToString()));
        if (die2 is (0, 0))
            goto ReselectDice;
        var reach = die1.Item2 + die2.Item2;
        AnsiConsole.MarkupLine($"sum to reach: [green]{reach}[/]");
        var die3 = AnsiConsole.Prompt(new SelectionPrompt<(int i, int d)>()
            .Title($"Select [blue]3rd[/] die")
            .AddChoices(dice.Index().Except([die1, die2]))
            .AddCancelResult((0, 0))
            .WrapAround()
            .UseConverter(t => t.Item2.ToString()));
        if (die3 is (0, 0))
            goto ReselectDice;
        var die4 = AnsiConsole.Prompt(new SelectionPrompt<(int i, int d)>()
            .Title($"Select [blue]4th[/] die ([green]{die3.Item2}[/])")
            .AddChoices(dice.Index().Except([die1, die2, die3]))
            .AddCancelResult((0, 0))
            .WrapAround()
            .UseConverter(t => t.Item2.ToString()));
        if (die4 is (0, 0))
            goto ReselectDice;
        var success = die3.Item2 + die4.Item2 == reach;
        if (!success)
            switch (AnsiConsole.Prompt(new SelectionPrompt<bool>()
                .Title("What to do ?")
                .AddChoices([false, true])
                .WrapAround()
                .UseConverter(i => i switch
                {
                    false => "Reselect dice",
                    true => "Auto-select pairs",
                })))
            {
                case false:
                    goto ReselectDice;
                case true:
                    SelectPair();
                    break;
                void SelectPair()
                {
                    for (var a = 0; a < dice.Length; a++)
                        for (var b = 0; b < dice.Length; b++)
                            for (var c = 0; c < dice.Length; c++)
                                for (var d = 0; d < dice.Length; d++)
                                    if (b == a || (c == a || c == b) || (d == a || d == b || d == c))
                                        continue;
                                    else if (dice[a] + dice[b] == dice[c] + dice[d])
                                    {
                                        (die1, die2, die3, die4, success) = ((a, dice[a]), (b, dice[b]), (c, dice[c]), (d, dice[d]), true);
                                        return;
                                    }
                    success = false;
                }
            }
        if (success)
            DiceStats.Wins++;
        else
            DiceStats.Loses++;
        if (success)
        {
            AnsiConsole.MarkupLine($"You [green bold]succeded[/] with 2 equals pairs: [green]{die1.Item2}[/] + [green]{die2.Item2}[/] = [blue]{reach}[/] = [green]{die3.Item2}[/] + [green]{die4.Item2}[/]");
            switch (activity)
            {
                case Activity.Eat:
                    this[CurrentMeal] = true;
                    AnsiConsole.MarkupLine($"Food: [blue]{FoodLevel}[/]");
                    break;
                case Activity.Clean:
                    Fog--;
                    AnsiConsole.MarkupLine($"Fog: [blue]{Fog}[/]");
                    if (Fog > 0)
                        goto AfterClean;
                    break;
                case Activity.Play:
                    Plays++;
                    AnsiConsole.MarkupLine($"Plays: [blue]{Plays}[/]");
                    break;
            }
        }
        else
            AnsiConsole.MarkupLine($"You [red bold]failed[/] with 2 differents pairs: [green]{die1.Item2}[/] + [green]{die2.Item2}[/] != [green]{die3.Item2}[/] + [green]{die4.Item2}[/]");
        LastActivity = DateTime.Now;
    }

    private Random GetRandom(Activity context)
    => new((CreatedAt.DayNumber * 73856093) ^ (Days * 19349663) ^ (DiceStats.Total * 83492809) ^ ((int)context * 12345701) + (int)LastActivity.Ticks);
}

public record class Data(Brain Brain, ImmutableArray<Brain> Deaths);

public sealed class Stats
{
    public int Wins { get; set; }
    public int Loses { get; set; }
    public int Total => Wins + Loses;
    public double Rate => (double)Wins / Total;
}

public enum Mood
{
    Dead,
    Awakened,
    Troubled,
    Stable,
    Content,
}

public enum Activity
{
    Eat,
    Play,
    Clean,
}

public enum Meal
{
    Breakfast,
    Lunch,
    Dinner,
}

public enum Action
{
    MoodCheck,
    Activity,
    Status,
    History,
    ManualEntry,
    Quit,
}

public enum ManualEntry
{
    Name,
    Days,
    Mood,
    LastActivity,
    LastMoodCheck,
    Breakfast,
    Lunch,
    Dinner,
    Plays,
    Fog,
    WinRoll,
    LosesRoll,
    WinDays,
    LosesDays,
}

file class Ext
{
    public static string Join<T>(string separator, string lastSeparator, params ReadOnlySpan<T> values)
    {
        switch (values)
        {
            case []:
                return "";
            case [var single]:
                return $"{single}";
            case [..var head, var last]:
                return $"{string.Join(separator, [..head])}{lastSeparator}{last}";
        }
    }
}
