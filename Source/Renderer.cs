using System.Numerics;
using Raylib_cs;

namespace VoidHunter;

static class Renderer
{
    public static void DrawWorld(World w, ContentPack c)
    {
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
            if (!p.Alive) continue;
            float t = Math.Clamp(p.Life / p.MaxLife, 0f, 1f);
            float sz = p.Size * (0.4f + 0.6f * t);
            Color col = Col.Fade(p.Color, t);
            if (c.Glow.Id != 0)
                DrawGlow(c, p.Pos + shake, sz * 1.6f, col, 0.8f);
            else
                Raylib.DrawCircleV(p.Pos + shake, sz, col);
        }
        Raylib.EndBlendMode();
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
        DrawBar(28, 22, 260, 16, hp, Col.Rgba(40, 16, 18, 180), Col.Rgba(230, 55, 70), "HULL");
        float shv = w.Player.MaxShield <= 0 ? 0 : w.Player.Shield / w.Player.MaxShield;
        DrawBar(28, 44, 260, 10, shv, Col.Rgba(12, 24, 36, 180), Col.Rgba(70, 190, 255), "SHIELD");

        DrawText(c.Font, $"SCORE  {w.Score:N0}", new Vector2(sw - 28, 20), 22, Color.White, true);
        DrawText(c.Font, $"BEST  {SaveData.HighScore:N0}", new Vector2(sw - 28, 46), 16, Col.Rgba(180, 200, 220), true);
        if (w.Combo > 1)
            DrawText(c.Font, $"COMBO  x{w.Combo}", new Vector2(sw - 28, 70), 18, Col.Rgba(255, 210, 90), true);

        DrawTextCentered(c.Font, $"LEVEL {Math.Max(1, w.Wave)} / {World.FinalLevel}", new Vector2(sw * 0.5f, 26), 22, Col.Rgba(180, 230, 255));

        string wpn = $"{World.WeaponName(w.Player.Weapon)}  L{Math.Max(1, w.Player.Levels[(int)w.Player.Weapon])}";
        if (w.Player.Overdrive > 0) wpn += $"  OD {w.Player.Overdrive:0.0}";
        DrawTextCentered(c.Font, wpn, new Vector2(sw * 0.5f, Raylib.GetScreenHeight() - 38), 18, Col.Rgba(140, 230, 255));

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
            DrawBar(sw * 0.5f - 220, 52, 440, 14, t, Col.Rgba(30, 10, 10, 200), Col.Rgba(255, 70, 50), "LEVIATHAN");
        }

        if (w.BannerT > 0 && w.Banner.Length > 0)
        {
            float a = Math.Clamp(w.BannerT, 0f, 1f);
            DrawTextCentered(c.Font, w.Banner, new Vector2(sw * 0.5f, 110), 42, Col.Fade(Color.White, a));
        }

        DrawCrosshair(Raylib.GetMousePosition());
    }

    static void DrawCrosshair(Vector2 m)
    {
        Color c = Col.Rgba(140, 240, 255, 210);
        Raylib.DrawCircleLinesV(m, 12, c);
        Raylib.DrawLineV(m + new Vector2(-18, 0), m + new Vector2(-6, 0), c);
        Raylib.DrawLineV(m + new Vector2(6, 0), m + new Vector2(18, 0), c);
        Raylib.DrawLineV(m + new Vector2(0, -18), m + new Vector2(0, -6), c);
        Raylib.DrawLineV(m + new Vector2(0, 6), m + new Vector2(0, 18), c);
        Raylib.DrawCircleV(m, 2, c);
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
        _ => Col.Rgba(255, 210, 60),
    };

    static Color DeathTint(EnemyKind k) => k switch
    {
        EnemyKind.Scout => Col.Rgba(90, 220, 80),
        EnemyKind.Strafer => Col.Rgba(200, 90, 255),
        EnemyKind.Bruiser => Col.Rgba(255, 90, 50),
        EnemyKind.Wasp => Col.Rgba(255, 200, 60),
        EnemyKind.Spitter => Col.Rgba(70, 230, 180),
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
}
