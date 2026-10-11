namespace SeasonsTogether.Core.Application;
/// <summary>Allowed music links only. Playback remains the existing external browser flow.</summary>
public static class MusicLink
{
    public static bool TryParse(string text, out Uri? uri)
    {
        return Uri.TryCreate(text.Trim(),UriKind.Absolute,out uri) && uri.Scheme == "https" && uri.UserInfo == "" && uri.IsDefaultPort &&
            uri.Host is "youtube.com" or "www.youtube.com" or "music.youtube.com" or "m.youtube.com" or "youtu.be";
    }
}
