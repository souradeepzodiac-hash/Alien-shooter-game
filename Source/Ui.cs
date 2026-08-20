using System.Numerics;
using Raylib_cs;

namespace VoidHunter;

enum Screen { Menu, HowTo, Playing, Paused, GameOver, LevelClear, Victory }

sealed class MenuController
{
    public int Index;
    readonly AudioBus _audio;
    int _lastHover = -1;

    public MenuController(AudioBus audio) => _audio = audio;

    public void Reset(int index = 0) => Index = index;

    public int Update(string[] items, Rectangle[] rects)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
        {
            Index = (Index + 1) % items.Length;
            _audio.Ui();
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
        {
            Index = (Index - 1 + items.Length) % items.Length;
            _audio.Ui();
        }

        Vector2 m = Raylib.GetMousePosition();
        for (int i = 0; i < rects.Length; i++)
        {
            if (Raylib.CheckCollisionPointRec(m, rects[i]))
            {
                if (_lastHover != i) { _audio.Ui(); _lastHover = i; }
                Index = i;
                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    _audio.UiOk();
                    return i;
                }
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            _audio.UiOk();
            return Index;
        }
        return -1;
    }

    public void DrawButtons(ContentPack c, string[] items, Rectangle[] rects)
    {
        for (int i = 0; i < items.Length; i++)
        {
            bool sel = i == Index;
            Color fill = sel ? Col.Rgba(18, 90, 120, 210) : Col.Rgba(8, 14, 22, 180);
            Color line = sel ? Col.Rgba(90, 230, 255) : Col.Rgba(70, 110, 140, 160);
            Raylib.DrawRectangleRounded(rects[i], 0.25f, 6, fill);
            Raylib.DrawRectangleRoundedLinesEx(rects[i], 0.25f, 6, 1.5f, line);
            Vector2 center = new(rects[i].X + rects[i].Width * 0.5f, rects[i].Y + rects[i].Height * 0.5f);
            Renderer.DrawTextCentered(c.Font, items[i], center, 22, sel ? Color.White : Col.Rgba(200, 214, 226));
        }
    }
}

static class Screens
{
    public static void DrawMain(ContentPack c, MenuController menu, float time)
    {
        Renderer.DrawMenuBackdrop(c, time);
        int sw = Raylib.GetScreenWidth();
        int sh = Raylib.GetScreenHeight();
        Renderer.DrawTextCentered(c.Font, "VOID HUNTER", new Vector2(sw * 0.5f, sh * 0.16f), 64, Color.White);
        Renderer.DrawTextCentered(c.Font, "HOLD THE RIFT. THEN RACE THE STAR SKY!", new Vector2(sw * 0.5f, sh * 0.18f + 48), 18, Col.Rgba(140, 220, 255));
        Renderer.DrawTextCentered(c.Font, $"BEST  {SaveData.HighScore:N0}", new Vector2(sw * 0.5f, sh * 0.18f + 78), 20, Col.Rgba(255, 210, 90));

        var items = MainItems;
        var rects = ButtonColumn(sw, sh * 0.36f, items.Length);
        menu.DrawButtons(c, items, rects);
        Renderer.DrawTextCentered(c.Font, "WASD MOVE   MOUSE AIM   LMB / SPACE FIRE   RMB / SHIFT DASH   ESC PAUSE",
            new Vector2(sw * 0.5f, sh - 36), 14, Col.Rgba(170, 190, 210, 180));
    }

    static readonly string[] MainItems = ["RIFT CAMPAIGN", "STAR SKY  3D", "HOW TO PLAY", "QUIT"];

    public static int UpdateMain(MenuController menu)
    {
        var rects = ButtonColumn(Raylib.GetScreenWidth(), Raylib.GetScreenHeight() * 0.36f, MainItems.Length);
        return menu.Update(MainItems, rects);
    }

    public static void DrawHowTo(ContentPack c)
    {
        Renderer.DrawMenuBackdrop(c, 0);
        int sw = Raylib.GetScreenWidth();
        float y = 90;
        Renderer.DrawTextCentered(c.Font, "BRIEFING", new Vector2(sw * 0.5f, y), 40, Color.White);
        y += 60;
        string[] lines =
        [
            "Pilot the interceptor. Survive the rift swarms.",
            "WASD or arrows move. Mouse aims. Left click or Space fires.",
            "Right click or Shift dashes — brief invulnerability, short cooldown.",
            "Star Sky: point the mouse to turn. Arrows fly. Catch stars. Aliens chase you anywhere!",
            "Keys 1-4 and mouse wheel switch unlocked weapons.",
            "PULSE is rapid. SPREAD covers arcs. RAIL pierces. NOVA detonates.",
            "Weapon crates upgrade the equipped gun, then unlock the next.",
            "Hull orbs repair. Hex tokens raise a shield. Gold cores start Overdrive.",
            "Choose your war: Rift Campaign (2D) or Abyss World (3D), 10 levels each.",
            "After Rift 10 you may enter the Abyss or claim victory and stop there.",
            "Every fifth Rift level a Leviathan arrives. In the Abyss, Hydras hunt you.",
            "After each level, review your result and choose NEXT LEVEL or QUIT.",
            "If your hull hits zero you lose that run. Retry the level or quit.",
            "Combo kills multiply score. ESC pauses. F11 fullscreen. M mutes.",
            "",
            "Click or press ESC / ENTER to return.",
        ];
        foreach (string line in lines)
        {
            Renderer.DrawTextCentered(c.Font, line, new Vector2(sw * 0.5f, y), 18, Col.Rgba(210, 220, 230));
            y += 28;
        }
    }

    public static void DrawPause(World w, ContentPack c, MenuController menu)
    {
        Renderer.DrawWorld(w, c);
        Renderer.DrawHud(w, c);
        int sw = Raylib.GetScreenWidth();
        int sh = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, sw, sh, Col.Rgba(4, 6, 12, 160));
        Renderer.DrawTextCentered(c.Font, "PAUSED", new Vector2(sw * 0.5f, sh * 0.22f), 48, Color.White);
        Renderer.DrawTextCentered(c.Font, $"{w.WorldName}   LEVEL  {Math.Max(1, w.Wave)} / {World.FinalLevel}", new Vector2(sw * 0.5f, sh * 0.22f + 42), 18, Col.Rgba(180, 220, 240));
        var items = PauseItems;
        var rects = ButtonColumn(sw, sh * 0.36f, items.Length);
        menu.DrawButtons(c, items, rects);
    }

    static readonly string[] PauseItems = ["RESUME", "RESTART LEVEL", "MAIN MENU", "QUIT GAME"];

    public static int UpdatePause(MenuController menu)
    {
        var rects = ButtonColumn(Raylib.GetScreenWidth(), Raylib.GetScreenHeight() * 0.36f, PauseItems.Length);
        return menu.Update(PauseItems, rects);
    }

    public static void DrawGameOver(World w, ContentPack c, MenuController menu)
    {
        Renderer.DrawWorld(w, c);
        int sw = Raylib.GetScreenWidth();
        int sh = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, sw, sh, Col.Rgba(8, 4, 8, 170));
        DrawResultCard(c, sw * 0.5f, sh * 0.18f, "YOU LOST", "THE SWARM TOOK THE RIFT", Col.Rgba(255, 80, 80), w, lost: true);
        var items = LoseItems;
        var rects = ButtonColumn(sw, sh * 0.58f, items.Length);
        menu.DrawButtons(c, items, rects);
    }

    static readonly string[] LoseItems = ["RETRY LEVEL", "MAIN MENU", "QUIT GAME"];

    public static int UpdateGameOver(MenuController menu)
    {
        var rects = ButtonColumn(Raylib.GetScreenWidth(), Raylib.GetScreenHeight() * 0.58f, LoseItems.Length);
        return menu.Update(LoseItems, rects);
    }

    public static void DrawLevelClear(World w, ContentPack c, MenuController menu)
    {
        Renderer.DrawWorld(w, c);
        Renderer.DrawHud(w, c);
        int sw = Raylib.GetScreenWidth();
        int sh = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, sw, sh, Col.Rgba(4, 10, 18, 200));
        string title = w.WantsWorldGate ? "RIFT CONQUERED" : w.IsAbyss ? "STAR LEVEL DONE!" : "LEVEL CLEARED";
        string sub = w.WantsWorldGate ? "CHOOSE YOUR PATH" : $"{w.WorldName}  LEVEL {w.Wave}  COMPLETE";
        DrawResultCard(c, sw * 0.5f, sh * 0.14f, title, sub, w.WantsWorldGate ? Col.Rgba(210, 140, 255) : Col.Rgba(90, 230, 180), w, lost: false);
        var items = ClearItemsFor(w);
        var rects = ButtonColumn(sw, w.WantsWorldGate ? sh * 0.54f : sh * 0.58f, items.Length, w.WantsWorldGate ? 54 : 62);
        menu.DrawButtons(c, items, rects);
    }

    static readonly string[] ClearItems = ["NEXT LEVEL", "MAIN MENU", "QUIT GAME"];
    static readonly string[] GateItems = ["ENTER STAR SKY", "CLAIM VICTORY", "MAIN MENU", "QUIT GAME"];
    static string[] ClearItemsFor(World w) => w.WantsWorldGate ? GateItems : ClearItems;

    public static int UpdateLevelClear(World w, MenuController menu)
    {
        var items = ClearItemsFor(w);
        float top = w.WantsWorldGate ? Raylib.GetScreenHeight() * 0.54f : Raylib.GetScreenHeight() * 0.58f;
        var rects = ButtonColumn(Raylib.GetScreenWidth(), top, items.Length, w.WantsWorldGate ? 54 : 62);
        return menu.Update(items, rects);
    }

    public static void DrawVictory(World w, ContentPack c, MenuController menu)
    {
        Renderer.DrawWorld(w, c);
        int sw = Raylib.GetScreenWidth();
        int sh = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, sw, sh, Col.Rgba(4, 10, 16, 170));
        string sub = w.EndedInAbyss ? "YOU SAVED THE STARS!" : "THE RIFT IS SEALED";
        DrawResultCard(c, sw * 0.5f, sh * 0.16f, "YOU WON", sub, Col.Rgba(255, 220, 90), w, lost: false);
        var items = WinItems;
        var rects = ButtonColumn(sw, sh * 0.58f, items.Length);
        menu.DrawButtons(c, items, rects);
    }

    static readonly string[] WinItems = ["PLAY AGAIN", "MAIN MENU", "QUIT GAME"];

    public static int UpdateVictory(MenuController menu)
    {
        var rects = ButtonColumn(Raylib.GetScreenWidth(), Raylib.GetScreenHeight() * 0.58f, WinItems.Length);
        return menu.Update(WinItems, rects);
    }

    static void DrawResultCard(ContentPack c, float cx, float top, string title, string sub, Color titleColor, World w, bool lost)
    {
        Renderer.DrawTextCentered(c.Font, title, new Vector2(cx, top), 50, titleColor);
        Renderer.DrawTextCentered(c.Font, sub, new Vector2(cx, top + 46), 18, Col.Rgba(200, 220, 230));

        float y = top + 88;
        DrawStat(c, cx, y, "TOTAL SCORE", $"{w.Score:N0}");
        DrawStat(c, cx, y + 26, lost ? "LEVEL REACHED" : "LEVEL", $"{w.WorldName}  {Math.Max(1, w.Wave)} / {World.FinalLevel}");
        DrawStat(c, cx, y + 52, "KILLS", $"{w.Kills}   (this level {w.LevelKills})");
        DrawStat(c, cx, y + 78, "LEVEL TIME", FormatTime(w.LevelTime));
        if (!lost)
        {
            DrawStat(c, cx, y + 104, "HULL LEFT", $"{Math.Clamp(w.Player.Hp / Math.Max(1f, w.Player.MaxHp), 0f, 1f) * 100f:0}%");
            DrawStat(c, cx, y + 130, "GRADE / BONUS", $"{w.ResultGrade}    +{w.ClearBonus}");
            if (w.IsAbyss)
                DrawStat(c, cx, y + 156, "STARS CAUGHT", $"{w.Stars}");
        }
        else
        {
            DrawStat(c, cx, y + 104, "BEST", $"{SaveData.HighScore:N0}");
            if (w.IsAbyss)
                DrawStat(c, cx, y + 130, "STARS CAUGHT", $"{w.Stars}");
        }
        if (w.NewHigh)
            Renderer.DrawTextCentered(c.Font, "NEW HIGH SCORE", new Vector2(cx, y + (lost ? 136 : 160)), 20, Col.Rgba(255, 210, 80));
    }

    static void DrawStat(ContentPack c, float cx, float y, string label, string value)
    {
        Renderer.DrawText(c.Font, label, new Vector2(cx - 200, y), 16, Col.Rgba(150, 175, 195), false);
        Renderer.DrawText(c.Font, value, new Vector2(cx + 200, y), 16, Color.White, true);
    }

    static string FormatTime(float seconds)
    {
        int t = Math.Max(0, (int)seconds);
        return $"{t / 60:00}:{t % 60:00}";
    }

    static Rectangle[] ButtonColumn(int sw, float top, int n, float gap = 62)
    {
        var r = new Rectangle[n];
        const float w = 300, h = 48;
        for (int i = 0; i < n; i++)
            r[i] = new Rectangle(sw * 0.5f - w * 0.5f, top + i * gap, w, h);
        return r;
    }
}
