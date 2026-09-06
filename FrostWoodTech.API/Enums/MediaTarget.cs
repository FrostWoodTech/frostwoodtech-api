namespace FrostWoodTech.API.Enums;

/// <summary>
/// Which part of the folder convention an upload belongs to. The client picks a target rather
/// than sending a folder path, so a stolen admin token cannot scribble anywhere in the account.
/// </summary>
public enum MediaTarget
{
    Projects,
    Services,
    Tags,
    Articles,
    Certificates
}
