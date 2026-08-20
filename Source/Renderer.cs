using System.Numerics;
using Raylib_cs;

namespace VoidHunter;

static class Renderer
{
    public static void DrawWorld(World w, ContentPack c)
    {
        if (w.IsAbyss)
        {
            DrawAbyss(w, c);
            return;
        }

        int sw = Raylib.GetScreenWidth();
        int sh = Raylib.GetScreenHeight();
        Vector2 shake = w.ShakeOff;

        DrawBackdrop(c, sw, sh, w.Time, shake);
        DrawPlayfieldFrame(w.Playfield, shake);

        foreach (Pickup p in w.Pickups)
        {
            if (!p.Alive) continue;
            float bob = MathF.Sin(p.Age * 3.4f) * 6f;
            float pulse = 0.9f + 0.1f * MathF.Sin(p.Age * 6f);
            float blink = p.Life < 3f && ((int)(p.Age * 8) % 2 == 0) ? 0.35f : 1f;
            DrawSprite(c.TexFor(p.Kind), p.Pos + shake + new Vector2(0, bob), 46 * pulse, p.Age * 20f, Col.Fade(Color.White, blink));
            DrawGlow(c, p.Pos + shake + new Vector2(0, bob), 38, PickupGlow(p.Kind), 0.35f * blink);
        }

        foreach (Bullet b in w.Bullets)
        {
            if (!b.Alive || b.Owner != BulletOwner.Enemy) continue;
            Vector2 p = b.Pos + shake;
            Raylib.DrawCircleV(p, b.Radius + 4, Col.Fade(b.Tint, 0.25f));
            Raylib.DrawCircleV(p, b.Radius, b.Tint);
            Raylib.DrawCircleV(p, b.Radius * 0.45f, Color.White);
        }

        foreach (Enemy e in w.Enemies)
        {
            if (!e.Alive) continue;
            float appear = e.SpawnIn > 0 ? 1f - Math.Clamp(e.SpawnIn / 0.45f, 0f, 1f) : 1f;
            if (e.Kind == EnemyKind.Boss && e.SpawnIn > 0)
                appear = 1f - Math.Clamp(e.SpawnIn / 1.1f, 0f, 1f);
            float size = e.Kind switch
            {
                EnemyKind.Wasp => 46,
                EnemyKind.Scout => 58,
                EnemyKind.Strafer => 72,
                EnemyKind.Bruiser => 88,
                EnemyKind.Spitter => 78,
                _ => 168,
            };
            Color tint = e.Flash > 0 ? Color.White : Col.Fade(Color.White, 0.35f + 0.65f * appear);
            float rot = e.Angle * Raylib.RAD2DEG + 90f;
            DrawGlow(c, e.Pos + shake, size * 0.7f, DeathTint(e.Kind), 0.22f * appear);
            DrawSprite(c.TexFor(e.Kind), e.Pos + shake, size, rot, tint);
            if (e.Hp < e.MaxHp && e.Kind != EnemyKind.Boss)
                DrawTinyBar(e.Pos + shake + new Vector2(0, e.Radius + 10), e.Hp / e.MaxHp, 28, Col.Rgba(255, 80, 70));
        }

        foreach (Bullet b in w.Bullets)
        {
            if (!b.Alive || b.Owner != BulletOwner.Player) continue;
            Vector2 p = b.Pos + shake;
            float ang = V.Ang(b.Vel) * Raylib.RAD2DEG;
            if (b.Style == WeaponKind.Rail)
            {
                Raylib.DrawCircleV(p, b.Radius + 8, Col.Fade(b.Tint, 0.28f));
                DrawRectCentered(p, 26, 7, ang, b.Tint);
                DrawRectCentered(p, 18, 3, ang, Color.White);
            }
            else if (b.Style == WeaponKind.Nova)
            {
                Raylib.DrawCircleV(p, b.Radius + 10, Col.Fade(b.Tint, 0.3f));
                Raylib.DrawCircleV(p, b.Radius, b.Tint);
                Raylib.DrawCircleV(p, b.Radius * 0.4f, Color.White);
            }
            else
            {
                Raylib.DrawCircleV(p, b.Radius + 5, Col.Fade(b.Tint, 0.3f));
                Raylib.DrawCircleV(p, b.Radius, b.Tint);
                Raylib.DrawCircleV(p, b.Radius * 0.4f, Color.White);
            }
        }

        if (w.Player.Alive)
        {
            var pl = w.Player;
            Color pt = pl.HurtFlash > 0 ? Col.Rgba(255, 120, 120) : Color.White;
            if (pl.IFrames > 0 && ((int)(w.Time * 20) % 2 == 0) && pl.HurtFlash <= 0)
                pt = Col.Fade(Color.White, 0.45f);
            float rot = pl.Angle * Raylib.RAD2DEG + 90f;
            if (pl.Shield > 0)
            {
                float a = 0.28f + 0.1f * MathF.Sin(w.Time * 6f);
                Raylib.DrawCircleLinesV(pl.Pos + shake, pl.Radius + 10, Col.Fade(Col.Rgba(90, 200, 255), a + 0.4f));
                Raylib.DrawCircleV(pl.Pos + shake, pl.Radius + 12, Col.Fade(Col.Rgba(80, 180, 255), a));
            }
            DrawGlow(c, pl.Pos + shake, 46, Col.Rgba(80, 230, 255), 0.35f);
            DrawSprite(c.Player, pl.Pos + shake, 68, rot, pt);
        }

        DrawParticles(w, c, shake);
        DrawRings(w, shake);
        DrawFloaters(w, c, shake);
        DrawVignette(sw, sh);
        if (w.Player.HurtFlash > 0)
            Raylib.DrawRectangle(0, 0, sw, sh, Col.Fade(Col.Rgba(180, 20, 30), w.Player.HurtFlash * 1.6f));
    }

    static void DrawBackdrop(ContentPack c, int sw, int sh, float time, Vector2 shake)
    {
        if (c.Nebula.Id != 0)
        {
            var src = new Rectangle(0, 0, c.Nebula.Width, c.Nebula.Height);
            var dest = new Rectangle(shake.X * 0.2f, shake.Y * 0.2f, sw, sh);
            Raylib.DrawTexturePro(c.Nebula, src, dest, Vector2.Zero, 0, Col.Rgba(170, 170, 190, 255));
        }
        else
        {
            Raylib.ClearBackground(Col.Rgba(6, 8, 16));
        }

        Raylib.DrawRectangle(0, 0, sw, sh, Col.Rgba(4, 6, 14, 90));
        var rng = new Random(17);
        for (int i = 0; i < 90; i++)
        {
            float x = (rng.NextSingle() * sw + time * (8 + rng.NextSingle() * 18)) % (sw + 8);
            float y = rng.NextSingle() * sh;
            float s = 1f + rng.NextSingle() * 2.2f;
            int tw = 160 + rng.Next(80);
            float twinkle = 0.45f + 0.55f * (0.5f + 0.5f * MathF.Sin(time * 3f + i));
            Raylib.DrawCircleV(new Vector2(x, y) + shake * 0.15f, s, Col.Fade(Color.White, twinkle * 0.7f));
        }
    }

    static void DrawPlayfieldFrame(Rectangle r, Vector2 shake)
    {
        var rr = new Rectangle(r.X + shake.X, r.Y + shake.Y, r.Width, r.Height);
        Raylib.DrawRectangleLinesEx(rr, 1.5f, Col.Rgba(70, 180, 220, 50));
    }

    static void DrawParticles(World w, ContentPack c, Vector2 shake)
    {
        Raylib.BeginBlendMode(BlendMode.Additive);
        foreach (Particle p in w.Particles)
        {
            if (!p.Alive || !p.Additive) continue;
            float t = Math.Clamp(p.Life / p.MaxLife, 0f, 1f);
            float sz = p.Size * (0.4f + 0.6f * t);
            Color col = Col.Fade(p.Color, t);
            if (c.Glow.Id != 0)
                DrawGlow(c, p.Pos + shake, sz * 1.6f, col, 0.8f);
            else
                Raylib.DrawCircleV(p.Pos + shake, sz, col);
        }
        Raylib.EndBlendMode();
        foreach (Particle p in w.Particles)
        {
            if (!p.Alive || p.Additive) continue;
            float t = Math.Clamp(p.Life / p.MaxLife, 0f, 1f);
            float sz = p.Size * (0.7f + 0.9f * (1f - t));
            Raylib.DrawCircleV(p.Pos + shake, sz, Col.Fade(p.Color, t * 0.75f));
            Raylib.DrawCircleV(p.Pos + shake, sz * 0.55f, Col.Fade(Col.Rgba(220, 220, 220), t * 0.4f));
        }
    }

    static void DrawRings(World w, Vector2 shake)
    {
        Raylib.BeginBlendMode(BlendMode.Additive);
        foreach (RingFx r in w.Rings)
        {
            if (!r.Alive) continue;
            float t = Math.Clamp(r.Life / r.MaxLife, 0f, 1f);
            Raylib.DrawCircleLinesV(r.Pos + shake, r.Radius, Col.Fade(r.Color, t * 0.85f));
            Raylib.DrawCircleLinesV(r.Pos + shake, r.Radius + 2f + 2f * t, Col.Fade(r.Color, t * 0.4f));
        }
        Raylib.EndBlendMode();
    }

    static void DrawFloaters(World w, ContentPack c, Vector2 shake)
    {
        foreach (Floater f in w.Floaters)
        {
            if (!f.Alive) continue;
            float t = Math.Clamp(f.Life / f.MaxLife, 0f, 1f);
            DrawTextCentered(c.Font, f.Text, f.Pos + shake, 18, Col.Fade(f.Color, t));
        }
    }

    static void DrawVignette(int sw, int sh)
    {
        int band = 90;
        Raylib.DrawRectangleGradientV(0, 0, sw, band, Col.Rgba(0, 0, 0, 150), Col.Rgba(0, 0, 0, 0));
        Raylib.DrawRectangleGradientV(0, sh - band, sw, band, Col.Rgba(0, 0, 0, 0), Col.Rgba(0, 0, 0, 170));
        Raylib.DrawRectangleGradientH(0, 0, band, sh, Col.Rgba(0, 0, 0, 130), Col.Rgba(0, 0, 0, 0));
        Raylib.DrawRectangleGradientH(sw - band, 0, band, sh, Col.Rgba(0, 0, 0, 0), Col.Rgba(0, 0, 0, 130));
    }

    public static void DrawHud(World w, ContentPack c)
    {
        int sw = Raylib.GetScreenWidth();
        float hp = w.Player.MaxHp <= 0 ? 0 : w.Player.Hp / w.Player.MaxHp;
        DrawBar(28, 22, 260, 16, hp, Col.Rgba(40, 16, 18, 180), Col.Rgba(230, 55, 70), w.IsAbyss ? "HEALTH" : "HULL");
        float shv = w.Player.MaxShield <= 0 ? 0 : w.Player.Shield / w.Player.MaxShield;
        DrawBar(28, 44, 260, 10, shv, Col.Rgba(12, 24, 36, 180), Col.Rgba(70, 190, 255), "SHIELD");

        DrawText(c.Font, $"SCORE  {w.Score:N0}", new Vector2(sw - 28, 20), 22, Color.White, true);
        DrawText(c.Font, $"BEST  {SaveData.HighScore:N0}", new Vector2(sw - 28, 46), 16, Col.Rgba(180, 200, 220), true);
        if (w.IsAbyss)
            DrawText(c.Font, $"STARS  {w.Stars}", new Vector2(sw - 28, 70), 20, Col.Rgba(255, 220, 90), true);
        if (w.Combo > 1)
            DrawText(c.Font, w.IsAbyss ? $"COMBO  x{w.Combo}  {ComboCheer(w.Combo)}" : $"COMBO  x{w.Combo}",
                new Vector2(sw - 28, w.IsAbyss ? 96 : 70), 18, Col.Rgba(255, 210, 90), true);

        DrawTextCentered(c.Font, w.WorldName, new Vector2(sw * 0.5f, 14), 14, w.IsAbyss ? Col.Rgba(255, 170, 230) : Col.Rgba(140, 210, 240));
        DrawTextCentered(c.Font, $"LEVEL {Math.Max(1, w.Wave)} / {World.FinalLevel}", new Vector2(sw * 0.5f, 34), 22, Col.Rgba(180, 230, 255));

        string wpn = w.IsAbyss
            ? $"{World.KidWeaponName(w.Player.Weapon)}  L{Math.Max(1, w.Player.Levels[(int)w.Player.Weapon])}"
            : $"{World.WeaponName(w.Player.Weapon)}  L{Math.Max(1, w.Player.Levels[(int)w.Player.Weapon])}";
        if (w.Player.Overdrive > 0) wpn += w.IsAbyss ? $"  STAR POWER {w.Player.Overdrive:0.0}" : $"  OD {w.Player.Overdrive:0.0}";
        DrawTextCentered(c.Font, wpn, new Vector2(sw * 0.5f, Raylib.GetScreenHeight() - 38), 18, Col.Rgba(140, 230, 255));
        if (w.IsAbyss)
            DrawTextCentered(c.Font, "MOUSE TURNS YOU   ARROWS FLY   Q/E UP-DOWN   CLICK TO BLAST",
                new Vector2(sw * 0.5f, Raylib.GetScreenHeight() - 16), 12, Col.Rgba(255, 190, 230, 220));

        // weapon slots
        float sx = sw * 0.5f - 86;
        float sy = Raylib.GetScreenHeight() - 64;
        for (int i = 0; i < 4; i++)
        {
            bool owned = w.Player.Levels[i] > 0;
            bool eq = owned && (int)w.Player.Weapon == i;
            Color box = eq ? Col.Rgba(40, 140, 180, 200) : owned ? Col.Rgba(20, 40, 55, 180) : Col.Rgba(10, 12, 16, 140);
            Raylib.DrawRectangleRounded(new Rectangle(sx + i * 44, sy, 38, 18), 0.3f, 4, box);
            Color tc = owned ? Color.White : Col.Rgba(80, 80, 90);
            DrawTextCentered(c.Font, (i + 1).ToString(), new Vector2(sx + i * 44 + 19, sy + 9), 14, tc);
        }

        float dash = 1f - Math.Clamp(w.Player.DashCd / 2.15f, 0f, 1f);
        DrawBar(28, Raylib.GetScreenHeight() - 36, 120, 8, dash, Col.Rgba(16, 20, 28, 180), Col.Rgba(90, 230, 200), "DASH");

        Enemy? boss = w.ActiveBoss();
        if (boss is not null)
        {
            float t = boss.MaxHp <= 0 ? 0 : boss.Hp / boss.MaxHp;
            string name = boss.Kind == EnemyKind.Hydra ? "SPACE BOSS" : "LEVIATHAN";
            DrawBar(sw * 0.5f - 220, 58, 440, 14, t, Col.Rgba(30, 10, 10, 200),
                boss.Kind == EnemyKind.Hydra ? Col.Rgba(190, 70, 255) : Col.Rgba(255, 70, 50), name);
        }

        if (w.BannerT > 0 && w.Banner.Length > 0)
        {
            float a = Math.Clamp(w.BannerT, 0f, 1f);
            Color bc = w.IsAbyss ? World.Rainbow(w.Time * 0.6f) : Color.White;
            DrawTextCentered(c.Font, w.Banner, new Vector2(sw * 0.5f, 110), 42, Col.Fade(bc, a));
        }
        if (w.IsAbyss && w.HintT > 0 && w.Hint.Length > 0)
        {
            float a = Math.Clamp(w.HintT / 2f, 0f, 1f);
            DrawTextCentered(c.Font, w.Hint, new Vector2(sw * 0.5f, 158), 20, Col.Fade(Col.Rgba(255, 240, 160), a));
        }

        DrawCrosshair(Raylib.GetMousePosition(), w.IsAbyss);
    }

    static string ComboCheer(int combo) => combo >= 8 ? "STAR POWER!" : combo >= 6 ? "MEGA!" : combo >= 4 ? "SUPER!" : "NICE!";

    static void DrawCrosshair(Vector2 m, bool playful = false)
    {
        Color c = playful ? Col.Rgba(255, 220, 80, 230) : Col.Rgba(140, 240, 255, 210);
        Raylib.DrawCircleLinesV(m, playful ? 16 : 12, c);
        Raylib.DrawLineV(m + new Vector2(-18, 0), m + new Vector2(-6, 0), c);
        Raylib.DrawLineV(m + new Vector2(6, 0), m + new Vector2(18, 0), c);
        Raylib.DrawLineV(m + new Vector2(0, -18), m + new Vector2(0, -6), c);
        Raylib.DrawLineV(m + new Vector2(0, 6), m + new Vector2(0, 18), c);
        Raylib.DrawCircleV(m, playful ? 3 : 2, playful ? Col.Rgba(255, 120, 200) : c);
        if (playful)
        {
            Raylib.DrawCircleLinesV(m, 22, Col.Rgba(255, 160, 220, 140));
        }
    }

    static void DrawBar(float x, float y, float w, float h, float t, Color back, Color fill, string label)
    {
        t = Math.Clamp(t, 0f, 1f);
        Raylib.DrawRectangleRounded(new Rectangle(x, y, w, h), 0.4f, 4, back);
        if (t > 0.01f)
            Raylib.DrawRectangleRounded(new Rectangle(x + 1, y + 1, (w - 2) * t, h - 2), 0.4f, 4, fill);
        Raylib.DrawTextEx(Raylib.GetFontDefault(), label, new Vector2(x, y - 13), 12, 1, Col.Rgba(200, 220, 230, 210));
    }

    static void DrawTinyBar(Vector2 center, float t, float w, Color fill)
    {
        t = Math.Clamp(t, 0f, 1f);
        Raylib.DrawRectangle((int)(center.X - w / 2), (int)center.Y, (int)w, 4, Col.Rgba(0, 0, 0, 160));
        Raylib.DrawRectangle((int)(center.X - w / 2), (int)center.Y, (int)(w * t), 4, fill);
    }

    static void DrawSprite(Texture2D tex, Vector2 pos, float size, float rot, Color tint)
    {
        if (tex.Id == 0)
        {
            Raylib.DrawCircleV(pos, size * 0.4f, tint);
            return;
        }
        float aspect = tex.Height / (float)Math.Max(1, tex.Width);
        float w = size;
        float h = size * aspect;
        var src = new Rectangle(0, 0, tex.Width, tex.Height);
        var dest = new Rectangle(pos.X, pos.Y, w, h);
        Raylib.DrawTexturePro(tex, src, dest, new Vector2(w * 0.5f, h * 0.5f), rot, tint);
    }

    static void DrawGlow(ContentPack c, Vector2 pos, float size, Color color, float alpha)
    {
        if (c.Glow.Id == 0) return;
        var src = new Rectangle(0, 0, c.Glow.Width, c.Glow.Height);
        var dest = new Rectangle(pos.X, pos.Y, size * 2, size * 2);
        Raylib.DrawTexturePro(c.Glow, src, dest, new Vector2(size, size), 0, Col.Fade(color, alpha));
    }

    static void DrawRectCentered(Vector2 pos, float w, float h, float deg, Color c)
    {
        Raylib.DrawRectanglePro(new Rectangle(pos.X, pos.Y, w, h), new Vector2(w * 0.5f, h * 0.5f), deg, c);
    }

    static Color PickupGlow(PickupKind k) => k switch
    {
        PickupKind.Health => Col.Rgba(255, 60, 70),
        PickupKind.Weapon => Col.Rgba(70, 220, 255),
        PickupKind.Shield => Col.Rgba(80, 180, 255),
        PickupKind.Star => Col.Rgba(255, 230, 70),
        _ => Col.Rgba(255, 210, 60),
    };

    static Color DeathTint(EnemyKind k) => k switch
    {
        EnemyKind.Scout => Col.Rgba(90, 220, 80),
        EnemyKind.Strafer => Col.Rgba(200, 90, 255),
        EnemyKind.Bruiser => Col.Rgba(255, 90, 50),
        EnemyKind.Wasp => Col.Rgba(255, 200, 60),
        EnemyKind.Spitter => Col.Rgba(70, 230, 180),
        EnemyKind.Prism => Col.Rgba(255, 80, 210),
        EnemyKind.Hunter => Col.Rgba(255, 150, 50),
        EnemyKind.Wraith => Col.Rgba(160, 90, 255),
        EnemyKind.Spire => Col.Rgba(255, 210, 80),
        _ => Col.Rgba(255, 80, 40),
    };

    public static void DrawText(Font font, string text, Vector2 pos, float size, Color color, bool right)
    {
        Vector2 m = Raylib.MeasureTextEx(font, text, size, 0.6f);
        if (right) pos.X -= m.X;
        Raylib.DrawTextEx(font, text, pos, size, 0.6f, color);
    }

    public static void DrawTextCentered(Font font, string text, Vector2 pos, float size, Color color)
    {
        Vector2 m = Raylib.MeasureTextEx(font, text, size, 0.6f);
        Raylib.DrawTextEx(font, text, pos - m * 0.5f, size, 0.6f, color);
    }

    public static void DrawMenuBackdrop(ContentPack c, float time)
    {
        int sw = Raylib.GetScreenWidth();
        int sh = Raylib.GetScreenHeight();
        if (c.Menu.Id != 0)
        {
            var src = new Rectangle(0, 0, c.Menu.Width, c.Menu.Height);
            Raylib.DrawTexturePro(c.Menu, src, new Rectangle(0, 0, sw, sh), Vector2.Zero, 0, Color.White);
        }
        else if (c.Nebula.Id != 0)
        {
            var src = new Rectangle(0, 0, c.Nebula.Width, c.Nebula.Height);
            Raylib.DrawTexturePro(c.Nebula, src, new Rectangle(0, 0, sw, sh), Vector2.Zero, 0, Color.White);
        }
        else Raylib.ClearBackground(Col.Rgba(6, 8, 16));
        Raylib.DrawRectangle(0, 0, sw, sh, Col.Rgba(4, 6, 14, 120 + (int)(20 * MathF.Sin(time))));
        DrawVignette(sw, sh);
    }

    static void DrawAbyss(World w, ContentPack c)
    {
        w.RefreshCamera();
        int sw = Raylib.GetScreenWidth();
        int sh = Raylib.GetScreenHeight();
        Texture2D sky = c.AbyssSky.Id != 0 ? c.AbyssSky : c.Nebula;
        if (sky.Id != 0)
        {
            var src = new Rectangle(0, 0, sky.Width, sky.Height);
            Raylib.DrawTexturePro(sky, src, new Rectangle(0, 0, sw, sh), Vector2.Zero, 0, Color.White);
        }
        else
        {
            Raylib.ClearBackground(Col.Rgba(18, 10, 8));
        }
        Raylib.DrawRectangle(0, 0, sw, sh, Col.Rgba(40, 10, 50, 40));
        Raylib.DrawRectangleGradientV(0, 0, sw, sh / 2, Col.Rgba(255, 140, 200, 28), Col.Rgba(0, 0, 0, 0));
        Raylib.DrawRectangleGradientV(0, sh / 2, sw, sh / 2, Col.Rgba(0, 0, 0, 0), Col.Rgba(20, 0, 40, 70));

        Raylib.BeginMode3D(w.Cam);
        DrawOpenSpace(w);
        DrawAbyssTerrain(w, c);

        foreach (Pickup p in w.Pickups)
        {
            if (!p.Alive) continue;
            Vector3 pos = w.ToWorld(p.Pos) + new Vector3(0, 1.2f + MathF.Sin(p.Age * 3.4f) * 0.25f, 0);
            Color col = PickupGlow(p.Kind);
            if (p.Kind == PickupKind.Star)
            {
                float spin = p.Age * 180f;
                Raylib.DrawCube(pos, 0.85f, 0.18f, 0.85f, col);
                Rlgl.PushMatrix();
                Rlgl.Translatef(pos.X, pos.Y, pos.Z);
                Rlgl.Rotatef(spin, 0, 1, 0);
                Rlgl.Rotatef(45, 0, 0, 1);
                Raylib.DrawCube(Vector3.Zero, 0.85f, 0.18f, 0.85f, Col.Rgba(255, 255, 180));
                Rlgl.PopMatrix();
                Raylib.DrawSphere(pos, 0.22f, Color.White);
            }
            else
            {
                Raylib.DrawSphere(pos, 0.55f, col);
                Raylib.DrawSphere(pos, 0.28f, Color.White);
            }
            Raylib.DrawCircle3D(w.ToWorld(p.Pos), 0.9f, Vector3.UnitX, 90, Col.Fade(col, 0.45f));
        }

        foreach (Bullet b in w.Bullets)
        {
            if (!b.Alive) continue;
            Vector3 pos = w.ToWorld(b.Pos, b.Alt);
            float br = b.Owner == BulletOwner.Player ? 1.65f : 1.15f;
            if (b.Style == WeaponKind.Nova) br = 2.3f;
            if (b.Style == WeaponKind.Rail) br = 1.35f;
            Vector3 velW = new(b.Vel.X * World.WorldScale, b.VelAlt, b.Vel.Y * World.WorldScale);
            if (velW.LengthSquared() > 0.01f)
            {
                Vector3 back = pos - Vector3.Normalize(velW) * br * 4.2f;
                Raylib.DrawCylinderEx(back, pos, br * 0.22f, br * 0.85f, 7, b.Tint);
            }
            Raylib.DrawSphere(pos, br * 1.55f, Col.Fade(b.Tint, 0.4f));
            Raylib.DrawSphere(pos, br, b.Tint);
            Raylib.DrawSphere(pos, br * 0.42f, Color.White);
        }

        foreach (Enemy e in w.Enemies)
        {
            if (!e.Alive) continue;
            DrawAlien3D(w, c, e);
        }

        if (w.Player.Alive)
            DrawShip3D(w, c);

        foreach (Particle p in w.Particles)
        {
            if (!p.Alive) continue;
            float t = Math.Clamp(p.Life / p.MaxLife, 0f, 1f);
            Vector3 pos = w.ToWorld(p.Pos, p.Alt);
            if (!p.Additive)
            {
                float grow = 1.2f + 1.1f * (1f - t);
                Raylib.DrawSphere(pos, MathF.Max(1.3f, p.Size * 0.085f * grow), Col.Fade(p.Color, t * 0.7f));
            }
            else
                Raylib.DrawSphere(pos, MathF.Max(0.28f, p.Size * World.WorldScale * 1.15f * t), Col.Fade(p.Color, t));
        }

        foreach (RingFx r in w.Rings)
        {
            if (!r.Alive) continue;
            float t = Math.Clamp(r.Life / r.MaxLife, 0f, 1f);
            Raylib.DrawCircle3D(w.ToWorld(r.Pos, r.Alt), MathF.Max(1.2f, r.Radius * World.WorldScale), Vector3.UnitX, 90, Col.Fade(r.Color, t));
            Raylib.DrawCircle3D(w.ToWorld(r.Pos, r.Alt), MathF.Max(1.2f, r.Radius * World.WorldScale), Vector3.UnitZ, 90, Col.Fade(r.Color, t * 0.7f));
        }

        DrawBooms(w);

        Raylib.EndMode3D();

        foreach (Particle p in w.Particles)
        {
            if (!p.Alive || p.Additive) continue;
            float t = Math.Clamp(p.Life / p.MaxLife, 0f, 1f);
            Vector2 sp = Raylib.GetWorldToScreen(w.ToWorld(p.Pos, p.Alt), w.Cam);
            float rad = 22f + 48f * (1f - t);
            Raylib.DrawCircleV(sp, rad, Col.Fade(p.Color, t * 0.55f));
        }

        foreach (Bullet b in w.Bullets)
        {
            if (!b.Alive || b.Owner != BulletOwner.Player) continue;
            Vector2 sp = Raylib.GetWorldToScreen(w.ToWorld(b.Pos, b.Alt), w.Cam);
            if (sp.X < -40 || sp.Y < -40 || sp.X > sw + 40 || sp.Y > sh + 40) continue;
            Raylib.DrawCircleV(sp, 18, Col.Fade(b.Tint, 0.45f));
            Raylib.DrawCircleV(sp, 9, b.Tint);
            Raylib.DrawCircleV(sp, 3, Color.White);
        }

        foreach (Boom b in w.Booms)
        {
            if (!b.Alive) continue;
            Vector2 sp = Raylib.GetWorldToScreen(b.Pos, w.Cam);
            float u = 1f - Math.Clamp(b.Life / Math.Max(0.01f, b.MaxLife), 0f, 1f);
            float rad = 28f + 90f * u;
            Raylib.DrawCircleV(sp, rad, Col.Fade(Col.Rgba(255, 120, 40), 0.45f * (1f - u)));
            Raylib.DrawCircleV(sp, rad * 0.45f, Col.Fade(Col.Rgba(255, 240, 160), 0.7f * (1f - u)));
        }

        foreach (Enemy e in w.Enemies)
        {
            if (!e.Alive) continue;
            Vector2 sp = Raylib.GetWorldToScreen(w.ToWorld(e.Pos, e.Alt) + new Vector3(0, 1.6f, 0), w.Cam);
            if (sp.X < -80 || sp.Y < -80 || sp.X > sw + 80 || sp.Y > sh + 80) continue;
            float size = e.Kind switch
            {
                EnemyKind.Hydra => 160f,
                EnemyKind.Spire => 96f,
                EnemyKind.Hunter => 88f,
                EnemyKind.Wraith => 90f,
                EnemyKind.Prism => 84f,
                _ => 80f,
            };
            float flap = 1f + 0.08f * MathF.Sin(e.Age * 11f);
            Color tint = e.Flash > 0 ? Col.Rgba(255, 240, 200) : Color.White;
            if (e.SpawnIn > 0) tint = Col.Fade(Color.White, 0.45f + 0.55f * (1f - Math.Clamp(e.SpawnIn, 0, 1)));
            DrawGlow(c, sp, size * 0.55f, DeathTint(e.Kind), 0.45f);
            DrawSprite(c.TexFor(e.Kind), sp, size * flap, 0f, tint);
            if (e.Kind is not (EnemyKind.Hydra or EnemyKind.Boss) && e.Hp < e.MaxHp)
                DrawTinyBar(sp + new Vector2(0, size * 0.42f), e.Hp / e.MaxHp, 48, Col.Rgba(255, 80, 70));
        }

        if (w.Player.Alive && c.Player.Id != 0)
        {
            Vector2 sp = Raylib.GetWorldToScreen(w.ToWorld(w.Player.Pos, w.Player.Alt), w.Cam);
            Color pt = w.Player.HurtFlash > 0 ? Col.Rgba(255, 140, 140) : Color.White;
            if (w.Player.IFrames > 0 && ((int)(w.Time * 20) % 2 == 0) && w.Player.HurtFlash <= 0)
                pt = Col.Fade(Color.White, 0.5f);
            float rot = w.Player.Angle * Raylib.RAD2DEG + 90f;
            DrawGlow(c, sp, 52, Col.Rgba(80, 230, 255), 0.5f);
            DrawSprite(c.Player, sp, 96, rot, pt);
        }

        foreach (Floater f in w.Floaters)
        {
            if (!f.Alive) continue;
            float t = Math.Clamp(f.Life / f.MaxLife, 0f, 1f);
            Vector2 scr = Raylib.GetWorldToScreen(w.ToWorld(f.Pos, f.Alt) + new Vector3(0, 2.2f, 0), w.Cam);
            DrawTextCentered(c.Font, f.Text, scr, 22, Col.Fade(f.Color, t));
        }

        int sw2 = Raylib.GetScreenWidth();
        int sh2 = Raylib.GetScreenHeight();
        Raylib.DrawRectangleGradientV(0, 0, sw2, 50, Col.Rgba(0, 0, 0, 80), Col.Rgba(0, 0, 0, 0));
        Raylib.DrawRectangleGradientV(0, sh2 - 50, sw2, 50, Col.Rgba(0, 0, 0, 0), Col.Rgba(0, 0, 0, 90));
        if (w.Player.HurtFlash > 0)
            Raylib.DrawRectangle(0, 0, sw2, sh2, Col.Fade(Col.Rgba(180, 20, 30), w.Player.HurtFlash * 1.6f));
    }

    static void DrawOpenSpace(World w)
    {
        Vector3 cam = w.Cam.Position;
        const float cell = 95f;
        int cx = (int)MathF.Floor(cam.X / cell);
        int cy = (int)MathF.Floor(cam.Y / cell);
        int cz = (int)MathF.Floor(cam.Z / cell);
        for (int ix = -2; ix <= 2; ix++)
        for (int iy = -2; iy <= 2; iy++)
        for (int iz = -2; iz <= 2; iz++)
        {
            int hx = cx + ix, hy = cy + iy, hz = cz + iz;
            int seed = hx * 73856093 ^ hy * 19349663 ^ hz * 83492791;
            var rng = new Random(seed);
            int n = 5 + (seed & 3);
            for (int k = 0; k < n; k++)
            {
                var p = new Vector3(
                    hx * cell + rng.NextSingle() * cell,
                    hy * cell + rng.NextSingle() * cell,
                    hz * cell + rng.NextSingle() * cell);
                if (Vector3.DistanceSquared(p, cam) < 80f) continue;
                float s = 0.07f + rng.NextSingle() * 0.14f;
                Color star = (seed + k) % 3 == 0 ? Col.Rgba(255, 180, 230, 220)
                    : (seed + k) % 3 == 1 ? Col.Rgba(180, 230, 255, 220)
                    : Col.Rgba(255, 240, 160, 220);
                Raylib.DrawSphere(p, s, star);
            }
            if ((seed & 7) == 0)
            {
                var rock = new Vector3(
                    hx * cell + rng.NextSingle() * cell,
                    hy * cell + rng.NextSingle() * cell,
                    hz * cell + rng.NextSingle() * cell);
                if (Vector3.DistanceSquared(rock, cam) < 400f) continue;
                float rs = 1.4f + rng.NextSingle() * 2.2f;
                Color candy = (seed & 24) switch
                {
                    0 => Col.Rgba(255, 110, 180),
                    8 => Col.Rgba(80, 220, 200),
                    16 => Col.Rgba(255, 210, 70),
                    _ => Col.Rgba(140, 160, 255),
                };
                Raylib.DrawSphere(rock, rs, candy);
                Raylib.DrawSphere(rock + new Vector3(0, rs * 0.15f, 0), rs * 0.55f, Color.White);
            }
        }
        Raylib.DrawSphere(new Vector3(48f, 26f, -62f), 9.5f, Col.Rgba(255, 236, 150));
        Raylib.DrawSphere(new Vector3(45.5f, 28.2f, -58f), 1.1f, Col.Rgba(40, 40, 70));
        Raylib.DrawSphere(new Vector3(51.2f, 28.4f, -58f), 1.1f, Col.Rgba(40, 40, 70));
        Raylib.DrawSphere(new Vector3(48.2f, 24.6f, -54f), 0.7f, Col.Rgba(255, 120, 160));
    }

    static void DrawAbyssTerrain(World w, ContentPack c)
    {
        if (c.OwnsGround && c.GroundModel.MeshCount > 0)
            Raylib.DrawModel(c.GroundModel, new Vector3(0, -0.02f, 0), 1f, Color.White);
        else
            Raylib.DrawPlane(Vector3.Zero, new Vector2(96, 74), Col.Rgba(180, 140, 220));

        // Floating candy islands
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + 0.2f;
            var island = new Vector3(MathF.Cos(a) * 42f, 6f + MathF.Sin(w.Time * 0.7f + i) * 1.4f, MathF.Sin(a) * 32f);
            Color top = i % 2 == 0 ? Col.Rgba(255, 150, 210) : Col.Rgba(120, 220, 255);
            Raylib.DrawCylinder(island, 3.4f, 4.2f, 1.2f, 10, top);
            Raylib.DrawSphere(island + new Vector3(0, 1.5f, 0), 1.1f, World.Rainbow(w.Time * 0.3f + i * 0.2f));
        }

        // Lollipop trees
        for (int i = 0; i < 10; i++)
        {
            float a = 0.5f + i * 0.61f;
            var p = new Vector3(MathF.Cos(a) * 28f, 0f, MathF.Sin(a) * 20f);
            Color candy = World.Rainbow(i * 0.13f);
            Raylib.DrawCylinder(p + new Vector3(0, 3.2f, 0), 0.22f, 0.28f, 6.4f, 6, Col.Rgba(255, 245, 250));
            Raylib.DrawSphere(p + new Vector3(0, 7.2f, 0), 1.8f, candy);
            Raylib.DrawSphere(p + new Vector3(0, 7.6f, 0), 0.7f, Color.White);
        }

        // Rainbow hoop
        for (int i = 0; i < 24; i++)
        {
            float a = i * MathF.Tau / 24f + w.Time * 0.15f;
            var p = new Vector3(MathF.Cos(a) * 18f, 8f + MathF.Sin(a * 2f) * 1.5f, MathF.Sin(a) * 18f);
            Raylib.DrawSphere(p, 0.55f, World.Rainbow(i / 24f + w.Time * 0.2f));
        }

        // Distant candy planets
        Raylib.DrawSphere(new Vector3(-55f, 22f, -40f), 7.5f, Col.Rgba(255, 110, 180));
        Raylib.DrawSphere(new Vector3(-52f, 24f, -38f), 2.4f, Col.Rgba(255, 200, 230));
        Raylib.DrawSphere(new Vector3(58f, 18f, -28f), 6.2f, Col.Rgba(90, 210, 255));
        Raylib.DrawCylinder(new Vector3(58f, 18f, -28f), 8.4f, 8.4f, 0.45f, 16, Col.Rgba(255, 220, 80));

        // Sparkle orbit
        for (int i = 0; i < 22; i++)
        {
            float a = w.Time * 0.55f + i * 0.28f;
            var orb = new Vector3(MathF.Cos(a) * (10 + i * 0.4f), 4.2f + MathF.Sin(w.Time * 1.4f + i) * 1.6f, MathF.Sin(a) * (8 + i * 0.2f));
            Raylib.DrawSphere(orb, 0.22f + (i % 3) * 0.08f, World.Rainbow(w.Time * 0.5f + i * 0.07f));
        }
    }

    static void DrawShip3D(World w, ContentPack c)
    {
        Vector3 feet = w.ToWorld(w.Player.Pos, w.Player.Alt);
        if (MathF.Abs(feet.X) < 52f && MathF.Abs(feet.Z) < 42f)
            Raylib.DrawCircle3D(new Vector3(feet.X, 0.04f, feet.Z), 1.6f, Vector3.UnitX, 90, Col.Rgba(0, 0, 0, 80));
        float pulse = 0.35f + 0.18f * (0.5f + 0.5f * MathF.Sin(w.Time * 22f));
        Raylib.DrawSphere(feet + new Vector3(0f, 0.15f, 0.2f), pulse, World.Rainbow(w.Time * 1.4f));
        if (w.Player.Shield > 0)
            Raylib.DrawSphereWires(feet + new Vector3(0, 1.1f, 0), 2.2f, 8, 8, Col.Rgba(40, 230, 210, 180));
        _ = c;
    }

    static void DrawAlien3D(World w, ContentPack c, Enemy e)
    {
        Texture2D tex = c.TexFor(e.Kind);
        Vector3 feet = w.ToWorld(e.Pos, e.Alt);
        Raylib.DrawCircle3D(new Vector3(feet.X, 0.04f, feet.Z), e.Radius * World.WorldScale * 1.1f, Vector3.UnitX, 90, Col.Rgba(0, 0, 0, 110));
        if (tex.Id == 0)
            return;

        float size = e.Kind switch
        {
            EnemyKind.Hydra => 14f,
            EnemyKind.Spire => 7.6f,
            EnemyKind.Hunter => 6.8f,
            EnemyKind.Wraith => 6.6f,
            EnemyKind.Prism => 6.2f,
            _ => 5.6f,
        };
        float bob = MathF.Sin(e.Age * 6.5f) * (e.Kind == EnemyKind.Wraith ? 0.55f : 0.32f);
        float flap = 1f + 0.14f * MathF.Sin(e.Age * 11f);
        float spin = e.Kind == EnemyKind.Wraith ? e.Age * 70f : e.Age * 18f;
        Color tint = e.Flash > 0 ? Col.Rgba(255, 240, 200) : Color.White;
        if (e.SpawnIn > 0) tint = Col.Fade(Color.White, 0.4f + 0.6f * (1f - Math.Clamp(e.SpawnIn, 0, 1)));
        Vector3 center = feet + new Vector3(0, size * 0.48f + bob, 0);
        Raylib.DrawSphere(center, size * 0.38f, Col.Fade(DeathTint(e.Kind), 0.22f));
        if (e.Flash > 0)
            Raylib.DrawSphere(center, size * 0.7f, Col.Fade(Color.White, 0.4f));
        Raylib.DrawBillboard(w.Cam, tex, center, size * flap, tint);
        _ = spin;
    }

    static void DrawBooms(World w)
    {
        foreach (Boom b in w.Booms)
        {
            if (!b.Alive) continue;
            float u = 1f - Math.Clamp(b.Life / Math.Max(0.01f, b.MaxLife), 0f, 1f);
            float fade = 1f - u;
            float r = b.Size * (0.35f + u * 1.7f);
            Raylib.DrawSphere(b.Pos, r * 1.35f, Col.Fade(Col.Rgba(255, 80, 30), 0.4f * fade));
            Raylib.DrawSphere(b.Pos, r, Col.Fade(b.Color, 0.85f * fade));
            Raylib.DrawSphere(b.Pos, r * 0.45f, Col.Fade(Col.Rgba(255, 250, 180), fade));
            if (u < 0.22f)
                Raylib.DrawSphere(b.Pos, r * 0.7f, Col.Fade(Color.White, 1f - u / 0.22f));
            Raylib.DrawCircle3D(b.Pos, r * 1.1f, Vector3.UnitX, 90, Col.Fade(Col.Rgba(255, 180, 60), fade));
            Raylib.DrawCircle3D(b.Pos, r * 1.1f, Vector3.UnitY, 0, Col.Fade(Col.Rgba(255, 140, 40), fade * 0.8f));
        }
    }
}
