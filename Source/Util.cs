using System.Numerics;
using System.Text.Json;
using Raylib_cs;

namespace VoidHunter;

static class Rng
{
    public static readonly Random I = new();
    public static float Float(float a, float b) => a + (b - a) * I.NextSingle();
    public static int Int(int a, int bExclusive) => I.Next(a, bExclusive);
    public static bool Chance(float p) => I.NextSingle() < p;
    public static T Pick<T>(params T[] xs) => xs[I.Next(xs.Length)];
}

static class V
{
    public static Vector2 FromAngle(float rad) => new(MathF.Cos(rad), MathF.Sin(rad));
    public static float Ang(Vector2 v) => MathF.Atan2(v.Y, v.X);
    public static Vector2 Norm(Vector2 v)
    {
        float l = v.Length();
        return l < 1e-5f ? Vector2.Zero : v / l;
    }

    public static Vector2 ClampTo(Vector2 p, Rectangle r, float rad)
    {
        p.X = Math.Clamp(p.X, r.X + rad, r.X + r.Width - rad);
        p.Y = Math.Clamp(p.Y, r.Y + rad, r.Y + r.Height - rad);
        return p;
    }

    public static Vector2 Perp(Vector2 v) => new(-v.Y, v.X);
}

static class Col
{
    public static Color Rgba(int r, int g, int b, int a = 255) => new(r, g, b, a);
    public static Color Fade(Color c, float a) => Raylib.Fade(c, Math.Clamp(a, 0f, 1f));
}

static class SaveData
{
    static string PathFile
    {
        get
        {
            string dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VoidHunter");
            Directory.CreateDirectory(dir);
            return System.IO.Path.Combine(dir, "save.json");
        }
    }

    public static int HighScore { get; private set; }

    public static void Load()
    {
        try
        {
            if (!File.Exists(PathFile)) return;
            using var doc = JsonDocument.Parse(File.ReadAllText(PathFile));
            if (doc.RootElement.TryGetProperty("highScore", out var hs))
                HighScore = Math.Max(0, hs.GetInt32());
        }
        catch { /* keep default */ }
    }

    public static bool TryRecord(int score)
    {
        if (score <= HighScore) return false;
        HighScore = score;
        try
        {
            File.WriteAllText(PathFile, $"{{\"highScore\":{HighScore}}}");
        }
        catch { /* still keep in-memory */ }
        return true;
    }
}

static class Paths
{
    public static string Assets { get; private set; } = "Assets";

    public static void Discover()
    {
        string baseDir = AppContext.BaseDirectory;
        string? exeDir = System.IO.Path.GetDirectoryName(Environment.ProcessPath);
        string[] candidates =
        [
            System.IO.Path.Combine(baseDir, "Assets"),
            exeDir is null ? "" : System.IO.Path.Combine(exeDir, "Assets"),
            System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "Assets")),
        ];
        foreach (string c in candidates)
        {
            if (c.Length > 0 && File.Exists(System.IO.Path.Combine(c, "Sprites", "player.png")))
            {
                Assets = c;
                return;
            }
        }
        Assets = candidates[0];
    }

    public static string Sprite(string name) => System.IO.Path.Combine(Assets, "Sprites", name);
    public static string Audio(string name) => System.IO.Path.Combine(Assets, "Audio", name);
}
