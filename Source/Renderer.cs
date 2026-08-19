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

        DrawTextCentered(c.Font, w.WorldName, new Vector2(sw * 0.5f, 14), 14, w.IsAbyss ? Col.Rgba(220, 140, 255) : Col.Rgba(140, 210, 240));
        DrawTextCentered(c.Font, $"LEVEL {Math.Max(1, w.Wave)} / {World.FinalLevel}", new Vector2(sw * 0.5f, 34), 22, Col.Rgba(180, 230, 255));

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
            string name = boss.Kind == EnemyKind.Hydra ? "HYDRA" : "LEVIATHAN";
            DrawBar(sw * 0.5f - 220, 58, 440, 14, t, Col.Rgba(30, 10, 10, 200),
                boss.Kind == EnemyKind.Hydra ? Col.Rgba(190, 70, 255) : Col.Rgba(255, 70, 50), name);
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
        if (c.Nebula.Id != 0)
        {
            var src = new Rectangle(0, 0, c.Nebula.Width, c.Nebula.Height);
            Raylib.DrawTexturePro(c.Nebula, src, new Rectangle(0, 0, sw, sh), Vector2.Zero, 0, Col.Rgba(210, 130, 170, 255));
        }
        else
        {
            Raylib.ClearBackground(Col.Rgba(10, 4, 16));
        }
        Raylib.DrawRectangleGradientV(0, 0, sw, sh, Col.Rgba(40, 8, 20, 70), Col.Rgba(6, 2, 14, 160));

        Raylib.BeginMode3D(w.Cam);
        DrawAbyssTerrain(w);

        foreach (Pickup p in w.Pickups)
        {
            if (!p.Alive) continue;
            Vector3 pos = w.ToWorld(p.Pos) + new Vector3(0, 1.2f + MathF.Sin(p.Age * 3.4f) * 0.25f, 0);
            Color col = PickupGlow(p.Kind);
            Raylib.DrawSphere(pos, 0.55f, col);
            Raylib.DrawSphere(pos, 0.28f, Color.White);
            Raylib.DrawCircle3D(w.ToWorld(p.Pos), 0.9f, Vector3.UnitX, 90, Col.Fade(col, 0.45f));
        }

        foreach (Bullet b in w.Bullets)
        {
            if (!b.Alive) continue;
            Vector3 pos = w.ToWorld(b.Pos) + new Vector3(0, 0.7f, 0);
            Raylib.DrawSphere(pos, MathF.Max(0.18f, b.Radius * World.WorldScale * 0.9f), b.Tint);
        }

        foreach (Enemy e in w.Enemies)
        {
            if (!e.Alive) continue;
            DrawAlien3D(w, e);
        }

        if (w.Player.Alive)
            DrawShip3D(w);

        foreach (Particle p in w.Particles)
        {
            if (!p.Alive) continue;
            float t = Math.Clamp(p.Life / p.MaxLife, 0f, 1f);
            Vector3 pos = w.ToWorld(p.Pos) + new Vector3(0, 0.6f, 0);
            Raylib.DrawSphere(pos, MathF.Max(0.06f, p.Size * World.WorldScale * 0.35f * t), Col.Fade(p.Color, t));
        }

        foreach (RingFx r in w.Rings)
        {
            if (!r.Alive) continue;
            float t = Math.Clamp(r.Life / r.MaxLife, 0f, 1f);
            Raylib.DrawCircle3D(w.ToWorld(r.Pos) + new Vector3(0, 0.05f, 0), r.Radius * World.WorldScale, Vector3.UnitX, 90, Col.Fade(r.Color, t));
        }

        Raylib.EndMode3D();

        foreach (Floater f in w.Floaters)
        {
            if (!f.Alive) continue;
            float t = Math.Clamp(f.Life / f.MaxLife, 0f, 1f);
            Vector2 scr = Raylib.GetWorldToScreen(w.ToWorld(f.Pos) + new Vector3(0, 2.2f, 0), w.Cam);
            DrawTextCentered(c.Font, f.Text, scr, 16, Col.Fade(f.Color, t));
        }

        DrawVignette(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        if (w.Player.HurtFlash > 0)
            Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), Col.Fade(Col.Rgba(180, 20, 30), w.Player.HurtFlash * 1.6f));
        DrawCrosshair(Raylib.GetMousePosition());
    }

    static void DrawAbyssTerrain(World w)
    {
        for (int x = -18; x <= 18; x++)
        {
            for (int z = -14; z <= 14; z++)
            {
                int h = (x * 73856093) ^ (z * 19349663);
                float bump = ((h & 7) - 3) * 0.04f;
                bool vein = (x * 2 + z * 5) % 13 == 0;
                Color tile = vein
                    ? Col.Rgba(78, 22, 40)
                    : ((x + z) & 1) == 0 ? Col.Rgba(32, 20, 28) : Col.Rgba(20, 12, 20);
                Raylib.DrawCube(new Vector3(x * 2.4f, -0.18f + bump, z * 2.4f), 2.32f, 0.28f + bump, 2.32f, tile);
                if (vein)
                    Raylib.DrawCube(new Vector3(x * 2.4f, 0.02f, z * 2.4f), 0.18f, 0.08f, 2.2f, Col.Rgba(255, 70, 90, 180));
            }
        }

        var rng = new Random(11);
        for (int i = 0; i < 26; i++)
        {
            float ang = rng.NextSingle() * MathF.Tau;
            float rad = 34f + rng.NextSingle() * 16f;
            var pos = new Vector3(MathF.Cos(ang) * rad, rng.NextSingle() * 2.2f, MathF.Sin(ang) * rad * 0.78f);
            float s = 1.6f + rng.NextSingle() * 4.5f;
            Raylib.DrawCube(pos, s, s * (0.6f + rng.NextSingle()), s * 0.8f, Col.Rgba(18 + rng.Next(16), 8, 22 + rng.Next(18)));
        }

        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + 0.2f;
            var baseP = new Vector3(MathF.Cos(a) * 31f, 0, MathF.Sin(a) * 24f);
            Raylib.DrawCylinder(baseP + new Vector3(0, 2.2f, 0), 0.35f, 1.1f, 5.4f, 7, Col.Rgba(36, 14, 40));
            Raylib.DrawSphere(baseP + new Vector3(0, 5.3f, 0), 0.7f, Col.Rgba(255, 70, 110, 200));
        }

        for (int i = 0; i < 16; i++)
        {
            float a = w.Time * 0.12f + i * 0.8f;
            var orb = new Vector3(MathF.Cos(a) * (16 + i * 0.7f), 5f + MathF.Sin(w.Time * 0.7f + i) * 1.6f, MathF.Sin(a * 0.85f) * (11 + i * 0.35f));
            Raylib.DrawSphere(orb, 0.16f + (i % 3) * 0.06f, Col.Rgba(255, 90, 140, 160));
        }
    }

    static void DrawShip3D(World w)
    {
        Vector3 p = w.ToWorld(w.Player.Pos) + new Vector3(0, 0.9f, 0);
        Color body = w.Player.HurtFlash > 0 ? Col.Rgba(255, 120, 120) : Col.Rgba(210, 230, 245);
        Rlgl.PushMatrix();
        Rlgl.Translatef(p.X, p.Y, p.Z);
        Rlgl.Rotatef(-w.Player.Angle * Raylib.RAD2DEG, 0, 1, 0);
        Raylib.DrawCube(new Vector3(0.35f, 0, 0), 1.8f, 0.38f, 0.55f, body);
        Raylib.DrawCube(new Vector3(-0.1f, 0, -0.85f), 0.7f, 0.12f, 1.4f, Col.Rgba(70, 200, 230));
        Raylib.DrawCube(new Vector3(-0.1f, 0, 0.85f), 0.7f, 0.12f, 1.4f, Col.Rgba(70, 200, 230));
        Raylib.DrawSphere(new Vector3(0.55f, 0.18f, 0), 0.18f, Col.Rgba(40, 80, 120));
        Raylib.DrawSphere(new Vector3(-0.7f, 0.05f, 0), 0.2f, Col.Rgba(80, 240, 255));
        Rlgl.PopMatrix();
        if (w.Player.Shield > 0)
            Raylib.DrawSphereWires(p, 1.35f, 8, 8, Col.Rgba(80, 180, 255, 180));
    }

    static void DrawAlien3D(World w, Enemy e)
    {
        Vector3 p = w.ToWorld(e.Pos);
        float bob = MathF.Sin(e.Age * 3.1f) * 0.12f;
        Color tint = e.Flash > 0 ? Color.White : DeathTint(e.Kind);
        float yaw = -e.Angle * Raylib.RAD2DEG;
        Rlgl.PushMatrix();
        Rlgl.Translatef(p.X, p.Y + 0.85f + bob, p.Z);
        Rlgl.Rotatef(yaw, 0, 1, 0);
        switch (e.Kind)
        {
            case EnemyKind.Prism:
                DrawPrismAlien(e.Age, tint);
                break;
            case EnemyKind.Hunter:
                DrawHunterAlien(e.Age, tint);
                break;
            case EnemyKind.Wraith:
                DrawWraithAlien(e.Age, tint);
                break;
            case EnemyKind.Spire:
                DrawSpireAlien(e.Age, tint);
                break;
            case EnemyKind.Hydra:
                DrawHydraAlien(e.Age, tint);
                break;
            default:
                Raylib.DrawCube(Vector3.Zero, 1.2f, 1.2f, 1.2f, tint);
                break;
        }
        Rlgl.PopMatrix();
    }

    static void DrawPrismAlien(float age, Color tint)
    {
        Color shell = Col.Rgba(190, 40, 160);
        Color glow = Col.Rgba(255, 90, 210);
        Rlgl.PushMatrix();
        Rlgl.Rotatef(age * 80f, 0, 1, 0);
        Rlgl.Rotatef(35f, 1, 0, 1);
        Raylib.DrawCube(Vector3.Zero, 0.95f, 1.7f, 0.95f, eMix(shell, tint));
        Raylib.DrawCubeWires(Vector3.Zero, 1.15f, 1.9f, 1.15f, glow);
        Rlgl.PopMatrix();
        Raylib.DrawSphere(new Vector3(0, 0.35f, 0), 0.28f, glow);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f + age * 40f;
            Rlgl.PushMatrix();
            Rlgl.Rotatef(a, 0, 1, 0);
            Raylib.DrawCube(new Vector3(0.85f, -0.15f, 0), 0.7f, 0.18f, 0.18f, shell);
            Raylib.DrawSphere(new Vector3(1.2f, -0.15f, 0), 0.12f, glow);
            Rlgl.PopMatrix();
        }
    }

    static void DrawHunterAlien(float age, Color tint)
    {
        Color hide = eMix(Col.Rgba(170, 55, 28), tint);
        Color claw = Col.Rgba(255, 170, 70);
        Raylib.DrawCube(new Vector3(0.15f, 0.1f, 0), 1.5f, 0.55f, 0.7f, hide);
        Raylib.DrawSphere(new Vector3(0.95f, 0.18f, 0), 0.38f, hide);
        Raylib.DrawSphere(new Vector3(1.18f, 0.28f, 0.16f), 0.1f, Col.Rgba(255, 80, 40));
        Raylib.DrawSphere(new Vector3(1.18f, 0.28f, -0.16f), 0.1f, Col.Rgba(255, 80, 40));
        Raylib.DrawCube(new Vector3(1.25f, 0.0f, 0.22f), 0.55f, 0.1f, 0.12f, claw);
        Raylib.DrawCube(new Vector3(1.25f, 0.0f, -0.22f), 0.55f, 0.1f, 0.12f, claw);
        Raylib.DrawCube(new Vector3(-0.85f, 0.05f, 0), 0.7f, 0.22f, 0.22f, Col.Rgba(90, 30, 20));
        float stomp = MathF.Sin(age * 10f) * 0.12f;
        for (int s = -1; s <= 1; s += 2)
        {
            Raylib.DrawCube(new Vector3(0.35f, -0.45f + stomp * s, 0.38f * s), 0.18f, 0.55f, 0.12f, hide);
            Raylib.DrawCube(new Vector3(-0.25f, -0.45f - stomp * s, 0.38f * s), 0.18f, 0.55f, 0.12f, hide);
        }
        _ = tint;
    }

    static void DrawWraithAlien(float age, Color tint)
    {
        Color veil = Col.Fade(eMix(Col.Rgba(120, 50, 200), tint), 0.8f);
        Raylib.DrawSphere(new Vector3(0, 0.35f, 0), 0.62f, veil);
        Raylib.DrawSphere(new Vector3(0, 0.55f, 0.18f), 0.14f, Col.Rgba(255, 160, 255));
        Raylib.DrawCylinder(new Vector3(0, -0.15f, 0), 0.55f, 0.12f, 0.9f, 8, Col.Fade(veil, 0.55f));
        for (int i = 0; i < 5; i++)
        {
            float a = i * 1.1f;
            float sway = MathF.Sin(age * 4f + a) * 0.22f;
            var t1 = new Vector3(MathF.Cos(a) * 0.25f, -0.55f, MathF.Sin(a) * 0.25f + sway * 0.2f);
            var t2 = t1 + new Vector3(sway, -0.55f, MathF.Sin(age * 3 + i) * 0.15f);
            Raylib.DrawSphere(t1, 0.1f, veil);
            Raylib.DrawSphere(t2, 0.08f, Col.Rgba(180, 80, 255, 160));
        }
    }

    static void DrawSpireAlien(float age, Color tint)
    {
        Color bark = eMix(Col.Rgba(70, 50, 28), tint);
        Raylib.DrawCylinder(new Vector3(0, -0.2f, 0), 0.7f, 0.95f, 0.45f, 8, bark);
        Raylib.DrawCylinder(new Vector3(0, 0.7f, 0), 0.22f, 0.38f, 1.6f, 7, eMix(Col.Rgba(90, 70, 40), tint));
        Vector3 head = new(0, 1.7f + MathF.Sin(age * 2f) * 0.05f, 0);
        Raylib.DrawSphere(head, 0.42f, Col.Rgba(40, 28, 16));
        Raylib.DrawSphere(head + new Vector3(0.28f, 0.05f, 0), 0.16f, Col.Rgba(255, 210, 80));
        for (int i = 0; i < 5; i++)
        {
            Rlgl.PushMatrix();
            Rlgl.Translatef(head.X, head.Y, head.Z);
            Rlgl.Rotatef(i * 72f + age * 20f, 1, 0, 0);
            Raylib.DrawCube(new Vector3(0, 0.45f, 0), 0.12f, 0.7f, 0.28f, Col.Rgba(180, 40, 50));
            Rlgl.PopMatrix();
        }
    }

    static void DrawHydraAlien(float age, Color tint)
    {
        Color hide = eMix(Col.Rgba(90, 25, 110), tint);
        Raylib.DrawSphere(new Vector3(0, 0.7f, 0), 1.55f, hide);
        Raylib.DrawSphere(new Vector3(0, 0.7f, 0), 0.55f, Col.Rgba(255, 50, 120));
        for (int i = 0; i < 5; i++)
        {
            float a = i * MathF.Tau / 5f + age * 0.7f;
            var head = new Vector3(MathF.Cos(a) * 2.3f, 1.9f + MathF.Sin(age * 2.4f + i) * 0.35f, MathF.Sin(a) * 2.3f);
            Raylib.DrawCylinderEx(new Vector3(0, 1.0f, 0), head, 0.32f, 0.18f, 6, hide);
            Raylib.DrawSphere(head, 0.48f, hide);
            Raylib.DrawSphere(head + new Vector3(0, 0.12f, 0.16f), 0.1f, Col.Rgba(255, 90, 40));
            Raylib.DrawSphere(head + new Vector3(0, 0.12f, -0.16f), 0.1f, Col.Rgba(255, 90, 40));
            Raylib.DrawCube(head + new Vector3(0.28f, -0.05f, 0.12f), 0.35f, 0.08f, 0.08f, Col.Rgba(40, 10, 20));
            Raylib.DrawCube(head + new Vector3(0.28f, -0.05f, -0.12f), 0.35f, 0.08f, 0.08f, Col.Rgba(40, 10, 20));
        }
    }

    static Color eMix(Color a, Color flash) => flash.R == 255 && flash.G == 255 && flash.B == 255 ? Color.White : a;
}
