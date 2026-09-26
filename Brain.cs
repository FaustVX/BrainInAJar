using System.Collections;
using System.Diagnostics;
using Spectre.Console;

namespace BrainInAJar;

class Brain
{
    public required string Name { get; set; }
    public int Days { get; set; }
    public DateOnly CreatedAt { get; init; } = DateOnly.FromDateTime(DateTime.Now);
    public DateTime LastActivity { get; set; } = DateTime.Now.AddHours(-1);
    public bool ActivityUnavailable => DateTime.Now.AddHours(-1) < LastActivity || this[CurrentMeal] && Fog <= 0 && (Mood is <= Mood.Awakened || Plays >= 2);
    public DateOnly LastMoodCheck { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public bool MoodCheckUnavailable => TimeOnly.FromDateTime(DateTime.Now) >= new TimeOnly(12, 0) || DateOnly.FromDateTime(DateTime.Now) <= LastMoodCheck;
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
        if (DateOnly.FromDateTime(DateTime.Now) <= LastMoodCheck)
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
                        DaysStats.Update(DayComplete);
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
        DaysStats.Update(DayComplete);
        LastMoodCheck = DateOnly.FromDateTime(DateTime.Now);
        Breakfast = Lunch = Dinner = false;
        Plays = 0;
        Fog = Days += 1;
    }

    public IEnumerable DoActivity()
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
            yield break;
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
            yield break;
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
                                    if (b == a) // Continue if die 2 equals to a previous die
                                        continue;
                                    else if (c == a || c == b) // Continue if die 3 equals to a previous die
                                        continue;
                                    else if (d == a || d == b || d == c) // Continue if die 4 equals to a previous die
                                        continue;
                                    else if (dice[a] + dice[b] == dice[c] + dice[d])
                                    {
                                        (die1, die2, die3, die4, reach, success) = ((a, dice[a]), (b, dice[b]), (c, dice[c]), (d, dice[d]), dice[a] + dice[b], true);
                                        return;
                                    }
                    success = false;
                }
            }
        DiceStats.Update(success);
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
                    {
                        LastActivity = DateTime.Now;
                        yield return null;
                        goto AfterClean;
                    }
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
