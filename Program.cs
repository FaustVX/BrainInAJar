using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

while (true)
{
    var brain = GetOrCreateBrain();
    while (brain.Mood is not Mood.Dead)
        switch (AnsiConsole.Prompt(new SelectionPrompt<Action>()
            .Title("Do what ?")
            .WrapAround()
            .AddChoices([
                ..(TimeOnly.FromDateTime(DateTime.Now) >= new TimeOnly(12, 0) || DateOnly.FromDateTime(DateTime.Now) <= DateOnly.FromDateTime(brain.LastMoodCheck)) ? Array.Empty<Action>() : [Action.MoodCheck],
                ..DateTime.Now.AddHours(-1) < brain.LastActivity ? Array.Empty<Action>() : [Action.Activity],
                Action.Status])
                .UseConverter(a => a switch
                {
                    Action.MoodCheck => "Morning mood check",
                    Action.Activity => "Do activity",
                    Action.Status => "Show status",
                    _ => throw new UnreachableException(),
                })))
        {
            case Action.MoodCheck:
                brain.MorningMoodCheck();
                Save(brain);
                break;
            case Action.Activity:
                brain.DoActivity();
                Save(brain);
                break;
            case Action.Status:
                brain = GetOrCreateBrain();
                var grid = new Grid();
                grid.AddColumns(2);
                grid.AddRow(new Text("Name"), new FigletText(brain.Name));
                grid.AddRow("Created At", brain.CreatedAt.ToString());
                grid.AddRow("Days", brain.Days.ToString());
                grid.AddRow("Current Date Time", DateTime.Now.ToString());
                var lastActivity = DateTime.Now - brain.LastActivity;
                grid.AddRow(new Text("Last Activity"), new Markup($"[{(lastActivity.TotalHours >= 1 ? "green" : "red")}]{lastActivity}[/]"));
                grid.AddRow(new Text("Last Morning Mood Check"), new Markup($"[{LastMoodColor(brain)}]{DateTime.Now - brain.LastMoodCheck}[/]"));
                grid.AddRow(new Text("Mood"), new Markup($"[{ToMoodColor(brain)}]{brain.Mood}[/]"));
                grid.AddRow(new Text("Food"), new Markup($"[{FoodLevelColor(brain)}]{brain.FoodLevel}/3[/]\n[{MealColor(brain.Breakfast)}]Breakfast[/]-[{MealColor(brain.Lunch)}]Lunch[/]-[{MealColor(brain.Dinner)}]Dinner[/]"));
                grid.AddRow(new Text("Plays"), new Markup($"[{PlaysColor(brain)}]{brain.Plays}/2[/]"));
                grid.AddRow(new Text("Fog"), new Markup($"[{FogColor(brain)}]{brain.Fog}/{brain.Days}[/]"));
                grid.AddRow(new Text("Love"), new Markup($"[{LoveColor(brain)}]{brain.Love}/1[/]"));
                AnsiConsole.Write(new Panel(grid).Border(BoxBorder.Beveled));
                break;

                static string ToMoodColor(Brain brain)
                => brain.Mood switch
                {
                    Mood.Dead => "bold red",
                    Mood.Awakened => "red",
                    Mood.Troubled => "orange",
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

                static string MealColor(bool meal)
                => meal ? "green" : "on";

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

static Brain GetOrCreateBrain()
{
    try
    {
        using var stream = File.Open("Brain.json", FileMode.Open, FileAccess.Read, FileShare.Read);
        return JsonSerializer.Deserialize<Brain>(stream, new JsonSerializerOptions(JsonSerializerDefaults.General)
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
        Save(brain);
        return brain;
    }
}

static void Save(Brain brain)
{
    using var stream = File.Open("Brain.json", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
    JsonSerializer.Serialize(stream, brain, new JsonSerializerOptions(JsonSerializerDefaults.General)
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
    public required string Name { get; init; }
    public int Days { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime LastActivity { get; set; } = DateTime.Now.AddHours(-1);
    public DateTime LastMoodCheck { get; set; } = DateTime.Now;
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
    public int Love => FoodLevel == 3 && Plays == 2 && Fog == 0 ? 1 : 0;
    public int Care => FoodLevel + Plays + Love;

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
        LastMoodCheck = DateTime.Now;
        Fog = Days++;
    }

    public void DoActivity()
    {
        var errors = (wait1hour: false, nothingToDo: false);
        if (DateTime.Now.AddHours(-1) < LastActivity)
            errors.wait1hour = true;
        if (FoodLevel >= 3 && Fog <= 0 && (Mood is <= Mood.Awakened || Plays >= 2))
            errors.nothingToDo = true;
        if (errors is not (false, false))
        {
            if (errors.wait1hour)
                AnsiConsole.MarkupLineInterpolated($"[red]You should wait at least [italic blue]1 hour[/] after the last activity tried[/] ([blue]{LastActivity - DateTime.Now.AddHours(-1)}[/] remaining)");
            if (errors.nothingToDo)
                AnsiConsole.MarkupLine("[red]You have nothing to do today, come back tomorrow[/]");
            return;
        }
        var meal = TimeOnly.FromDateTime(DateTime.Now) switch
        {
            { Hour: < 12 } => Meal.Breakfast,
            { Hour: >= 18 } => Meal.Dinner,
            _ => Meal.Lunch,
        };
        var activity = AnsiConsole.Prompt(new SelectionPrompt<Activity>()
            .Title("Select the activity")
            .WrapAround()
            .AddChoices([
                ..this[meal] ? Array.Empty<Activity>() : [Activity.Eat],
                ..Fog <= 0 ? Array.Empty<Activity>() : [Activity.Clean],
                ..Mood is <= Mood.Awakened || Plays >= 2 ? Array.Empty<Activity>() : [Activity.Play]])
                .UseConverter(a => a is Activity.Eat ? $"Eat {meal}" : a.ToString()));
        var numDice = Mood switch
        {
            Mood.Content => 6,
            Mood.Stable => 5,
            Mood.Troubled or Mood.Awakened => 4,
            _ => throw new UnreachableException(),
        };
        AfterClean:
        AnsiConsole.MarkupLine($"You will play the dice game with [underline italic blue]{Name}[/] to [green]{(activity is Activity.Eat ? $"Eat {meal}" : activity)}[/] with [green]{numDice}[/] dice because you are [underline italic blue]{Mood}[/]");
        var dice = Random.Shared.GetItems([1, 2, 3, 4, 5, 6], numDice);
        AnsiConsole.MarkupLine("your dice: [green]" + Ext.Join("[/], [green]", "[/] and [green]", dice) + "[/]");
        ReselectDice:
        var die1 = AnsiConsole.Prompt(new SelectionPrompt<(int i, int d)>()
            .Title($"Select [blue]1st[/] die")
            .AddChoices(dice.Index())
            .AddCancelResult((0, 0))
            .WrapAround()
            .UseConverter(t => t.Item2.ToString()));
        if (die1 is (0, 0))
            goto ReselectDice;
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
        if (success)
        {
            AnsiConsole.MarkupLine($"You [green bold]succeded[/] with 2 equals pairs: [green]{die1.Item2}[/] + [green]{die2.Item2}[/] = [blue]{reach}[/] = [green]{die3.Item2}[/] + [green]{die4.Item2}[/]");
            switch (activity)
            {
                case Activity.Eat:
                    this[meal] = true;
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
}

public enum Mood
{
    Dead = -1,
    None,
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
