using Raylib_cs;

namespace VoidHunter;

sealed class ContentPack : IDisposable
{
    public Texture2D Player, Scout, Strafer, Bruiser, Wasp, Spitter, Boss;
    public Texture2D Health, Weapon, Shield, Overdrive;
    public Texture2D Nebula, Menu, Glow;
    public Font Font;
    public bool OwnsFont;

    public void Load()
    {
        Player = LoadSprite("player.png");
        Scout = LoadSprite("scout.png");
        Strafer = LoadSprite("strafer.png");
        Bruiser = LoadSprite("bruiser.png");
        Wasp = LoadSprite("wasp.png");
        Spitter = LoadSprite("spitter.png");
        Boss = LoadSprite("boss.png");
        Health = LoadSprite("health.png");
        Weapon = LoadSprite("weapon.png");
        Shield = LoadSprite("shield.png");
        Overdrive = LoadSprite("overdrive.png");
        Nebula = LoadSprite("nebula.png");
        Menu = LoadSprite("menu.png");

        Image glow = Raylib.GenImageGradientRadial(64, 64, 0.12f, Color.White, Col.Rgba(255, 255, 255, 0));
        Glow = Raylib.LoadTextureFromImage(glow);
        Raylib.UnloadImage(glow);
        Raylib.SetTextureFilter(Glow, TextureFilter.Bilinear);

        Font = LoadNiceFont(72);
    }

    static Texture2D LoadSprite(string name)
    {
        string path = Paths.Sprite(name);
        if (!File.Exists(path))
            return default;
        Texture2D tex = Raylib.LoadTexture(path);
        if (tex.Id != 0)
            Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
        return tex;
    }

    Font LoadNiceFont(int size)
    {
        string[] fonts =
        [
            @"C:\Windows\Fonts\bahnschrift.ttf",
            @"C:\Windows\Fonts\segoeui.ttf",
            @"C:\Windows\Fonts\arial.ttf",
        ];
        foreach (string f in fonts)
        {
            if (!File.Exists(f)) continue;
            Font font = Raylib.LoadFontEx(f, size, null, 0);
            if (font.Texture.Id != 0)
            {
                Raylib.SetTextureFilter(font.Texture, TextureFilter.Bilinear);
                OwnsFont = true;
                return font;
            }
        }
        OwnsFont = false;
        return Raylib.GetFontDefault();
    }

    public Texture2D TexFor(EnemyKind kind) => kind switch
    {
        EnemyKind.Scout => Scout,
        EnemyKind.Strafer => Strafer,
        EnemyKind.Bruiser => Bruiser,
        EnemyKind.Wasp => Wasp,
        EnemyKind.Spitter => Spitter,
        _ => Boss,
    };

    public Texture2D TexFor(PickupKind kind) => kind switch
    {
        PickupKind.Health => Health,
        PickupKind.Weapon => Weapon,
        PickupKind.Shield => Shield,
        _ => Overdrive,
    };

    public void Dispose()
    {
        Unload(Player); Unload(Scout); Unload(Strafer); Unload(Bruiser);
        Unload(Wasp); Unload(Spitter); Unload(Boss);
        Unload(Health); Unload(Weapon); Unload(Shield); Unload(Overdrive);
        Unload(Nebula); Unload(Menu); Unload(Glow);
        if (OwnsFont) Raylib.UnloadFont(Font);
    }

    static void Unload(Texture2D t)
    {
        if (t.Id != 0) Raylib.UnloadTexture(t);
    }
}
