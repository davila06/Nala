namespace PawTrack.Domain.Medical;

public enum ActivityType
{
    Walk = 0,
    Run = 1,
    Play = 2,
    Swim = 3,
    Training = 4,
    Other = 5,
}

public enum ActivitySource
{
    Manual = 0, // user entered
    Tractive = 1, // computed from CollarLocation track points
}
