public struct ServerData
{
    public int Size;
    public int Max;
    public string Id;
    public bool AllowJoining;
}

public struct PresenceData
{
    public string State;
    public string Details;
    public string LargeImageKey;
    public string LargeImageText;
    public string SmallImageKey;
    public string SmallImageText;

    public ServerData? Multiplayer;
}