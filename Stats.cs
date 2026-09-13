using System.Collections.Immutable;

namespace BrainInAJar;

sealed class Stats
{
    public int Wins { get; set; }
    public int Loses { get; set; }
    public int Total => Wins + Loses;
    public double Rate => (double)Wins / Total;
}

record class Data(Brain Brain, ImmutableArray<Brain> Deaths);
