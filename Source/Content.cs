using Raylib_cs;

namespace VoidHunter;

sealed class ContentPack : IDisposable
{
    public Texture2D Player, Scout, Strafer, Bruiser, Wasp, Spitter, Boss;
    public Texture2D Health, Weapon, Shield, Overdrive;
    public Texture2D Nebula, Menu, Glow;
    public Texture2D AbyssSky, AbyssGround, Prism, Hunter, Wraith, Spire, Hydra;
    public Model GroundModel;
    public bool OwnsGround;
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
        AbyssSky = LoadSprite("abyss_sky.png");
        AbyssGround = LoadSprite("abyss_ground.png");
        Prism = LoadSprite("prism.png");
        Hunter = LoadSprite("hunter.png");
        Wraith = LoadSprite("wraith.png");
        Spire = LoadSprite("spire.png");
        Hydra = LoadSprite("hydra.png");
        if (AbyssGround.Id != 0)
        {
            Mesh mesh = Raylib.GenMeshPlane(96, 74, 1, 1);
            GroundModel = Raylib.LoadModelFromMesh(mesh);
            unsafe
            {
                Raylib.SetMaterialTexture(&GroundModel.Materials[0], MaterialMapIndex.Albedo, AbyssGround);
            }
            OwnsGround = GroundModel.MeshCount > 0;
        }

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
        EnemyKind.Prism => Prism.Id != 0 ? Prism : Boss,
        EnemyKind.Hunter => Hunter.Id != 0 ? Hunter : Boss,
        EnemyKind.Wraith => Wraith.Id != 0 ? Wraith : Boss,
        EnemyKind.Spire => Spire.Id != 0 ? Spire : Boss,
        EnemyKind.Hydra => Hydra.Id != 0 ? Hydra : Boss,
        _ => Boss,
    };

    public Texture2D TexFor(PickupKind kind) => kind switch
    {
        PickupKind.Health => Health,
        PickupKind.Weapon => Weapon,
        PickupKind.Shield => Shield,
        PickupKind.Star => Overdrive,
        _ => Overdrive,
    };

    public void Dispose()
    {
        Unload(Player); Unload(Scout); Unload(Strafer); Unload(Bruiser);
        Unload(Wasp); Unload(Spitter); Unload(Boss);
        Unload(Health); Unload(Weapon); Unload(Shield); Unload(Overdrive);
        Unload(Nebula); Unload(Menu); Unload(Glow);
        Unload(AbyssSky); Unload(AbyssGround);
        Unload(Prism); Unload(Hunter); Unload(Wraith); Unload(Spire); Unload(Hydra);
        if (OwnsGround) Raylib.UnloadModel(GroundModel);
        if (OwnsFont) Raylib.UnloadFont(Font);
    }

    static void Unload(Texture2D t)
    {
        if (t.Id != 0) Raylib.UnloadTexture(t);
    }
}
