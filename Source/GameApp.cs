using Raylib_cs;

namespace VoidHunter;

static class GameApp
{
    public static void Run(string[] args)
    {
        bool smoke = args.Any(a => a.Equals("--smoke", StringComparison.OrdinalIgnoreCase));
        bool menuSmoke = args.Any(a => a.Equals("--menu-smoke", StringComparison.OrdinalIgnoreCase));
        bool clearTest = args.Any(a => a.Equals("--clear-test", StringComparison.OrdinalIgnoreCase));
        bool resultSmoke = args.Any(a => a.Equals("--result-smoke", StringComparison.OrdinalIgnoreCase));
        bool winSmoke = args.Any(a => a.Equals("--win-smoke", StringComparison.OrdinalIgnoreCase));
        bool loseSmoke = args.Any(a => a.Equals("--lose-smoke", StringComparison.OrdinalIgnoreCase));
        string? shot = null;
        int startWave = 0;
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("--screenshot", StringComparison.OrdinalIgnoreCase))
                shot = args[i + 1];
            if (args[i].Equals("--wave", StringComparison.OrdinalIgnoreCase) && int.TryParse(args[i + 1], out int w))
                startWave = w;
        }

        Paths.Discover();
        SaveData.Load();

        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint | ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1600, 900, "VOID HUNTER");
        Raylib.SetWindowMinSize(1180, 680);
        Raylib.SetTargetFPS(smoke ? 60 : 60);
        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.InitAudioDevice();
        Raylib.HideCursor();
        Raylib.SetMasterVolume(1f);

        using var content = new ContentPack();
        content.Load();
        string iconPath = Paths.Sprite("player.png");
        if (File.Exists(iconPath))
        {
            Image icon = Raylib.LoadImage(iconPath);
            Raylib.SetWindowIcon(icon);
            Raylib.UnloadImage(icon);
        }
        using var audio = new AudioBus();
        audio.Load();

        var world = new World(audio);
        world.SyncPlayfield();
        var menu = new MenuController(audio);
        var screen = smoke ? Screen.Playing : Screen.Menu;
        float time = 0;
        float smokeT = 0;
        float menuLock = 0;
        if (clearTest)
        {
            world.SimulateWaveCleared(1);
            screen = Screen.Playing;
            audio.PlayBattle();
        }
        else if (resultSmoke || winSmoke || loseSmoke)
        {
            world.PrepareDemoResult(winSmoke ? "win" : loseSmoke ? "lose" : "clear");
            screen = winSmoke ? Screen.Victory : loseSmoke ? Screen.GameOver : Screen.LevelClear;
            menu.Reset();
            audio.PlayTheme();
        }
        else if (smoke)
        {
            world.AutoPlay = true;
            world.StartNew();
            if (startWave > 1) world.JumpToWave(startWave);
            audio.PlayBattle();
        }
        else audio.PlayTheme();

        while (!Raylib.WindowShouldClose())
        {
            float dt = Math.Clamp(Raylib.GetFrameTime(), 0f, 1f / 20f);
            time += dt;
            try { audio.Update(); }
            catch (Exception ex) { throw new InvalidOperationException("audio.Update", ex); }

            if (Raylib.IsKeyPressed(KeyboardKey.F11))
                Raylib.ToggleBorderlessWindowed();
            if (Raylib.IsKeyPressed(KeyboardKey.M))
                audio.ToggleMute();
            if (menuLock > 0) menuLock -= dt;

            switch (screen)
            {
                case Screen.Menu:
                    audio.PlayTheme();
                    audio.PauseMusic(false);
                    int pick = Screens.UpdateMain(menu);
                    if (pick == 0) StartGame(world, audio, menu, ref screen);
                    else if (pick == 1) screen = Screen.HowTo;
                    else if (pick == 2) Quit();
                    break;
                case Screen.HowTo:
                    if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Enter)
                        || Raylib.IsMouseButtonPressed(MouseButton.Left))
                    {
                        audio.UiOk();
                        screen = Screen.Menu;
                    }
                    break;
                case Screen.Playing:
                    audio.PlayBattle();
                    audio.PauseMusic(false);
                    if (!smoke && (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.P)))
                    {
                        screen = Screen.Paused;
                        menu.Reset();
                        audio.Ui();
                        audio.PauseMusic(true);
                        break;
                    }
                    world.Update(dt);
                    if (world.WantsVictory)
                    {
                        screen = Screen.Victory;
                        menu.Reset();
                        menuLock = 0.45f;
                        audio.PlayTheme();
                    }
                    else if (world.WantsLevelClear)
                    {
                        screen = Screen.LevelClear;
                        menu.Reset();
                        menuLock = 0.45f;
                        audio.PauseMusic(true);
                    }
                    else if (world.WantsGameOver)
                    {
                        screen = Screen.GameOver;
                        menu.Reset();
                        menuLock = 0.45f;
                        audio.PlayTheme();
                    }
                    break;
                case Screen.Paused:
                    int p = menuLock > 0 ? -1 : Screens.UpdatePause(menu);
                    if (p == 0 || Raylib.IsKeyPressed(KeyboardKey.Escape))
                    {
                        screen = Screen.Playing;
                        audio.PauseMusic(false);
                    }
                    else if (p == 1)
                    {
                        world.RetryCurrentLevel();
                        screen = Screen.Playing;
                        audio.PauseMusic(false);
                    }
                    else if (p == 2)
                    {
                        screen = Screen.Menu;
                        menu.Reset();
                        audio.PlayTheme();
                    }
                    else if (p == 3) Quit();
                    break;
                case Screen.GameOver:
                    int g = menuLock > 0 ? -1 : Screens.UpdateGameOver(menu);
                    if (g == 0)
                    {
                        world.RetryCurrentLevel();
                        screen = Screen.Playing;
                        audio.PlayBattle();
                    }
                    else if (g == 1)
                    {
                        screen = Screen.Menu;
                        menu.Reset();
                    }
                    else if (g == 2) Quit();
                    break;
                case Screen.LevelClear:
                    int cleared = menuLock > 0 ? -1 : Screens.UpdateLevelClear(menu);
                    if (cleared == 0)
                    {
                        world.ContinueNextLevel();
                        screen = Screen.Playing;
                        audio.PlayBattle();
                        audio.PauseMusic(false);
                    }
                    else if (cleared == 1)
                    {
                        screen = Screen.Menu;
                        menu.Reset();
                        audio.PlayTheme();
                    }
                    else if (cleared == 2) Quit();
                    break;
                case Screen.Victory:
                    int v = menuLock > 0 ? -1 : Screens.UpdateVictory(menu);
                    if (v == 0) StartGame(world, audio, menu, ref screen);
                    else if (v == 1)
                    {
                        screen = Screen.Menu;
                        menu.Reset();
                    }
                    else if (v == 2) Quit();
                    break;
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Col.Rgba(4, 6, 12));
            switch (screen)
            {
                case Screen.Menu:
                    Screens.DrawMain(content, menu, time);
                    break;
                case Screen.HowTo:
                    Screens.DrawHowTo(content);
                    break;
                case Screen.Playing:
                    Renderer.DrawWorld(world, content);
                    Renderer.DrawHud(world, content);
                    break;
                case Screen.Paused:
                    Screens.DrawPause(world, content, menu);
                    break;
                case Screen.GameOver:
                    Screens.DrawGameOver(world, content, menu);
                    break;
                case Screen.LevelClear:
                    Screens.DrawLevelClear(world, content, menu);
                    break;
                case Screen.Victory:
                    Screens.DrawVictory(world, content, menu);
                    break;
            }
            Raylib.EndDrawing();

            if (smoke || menuSmoke || resultSmoke || winSmoke || loseSmoke || clearTest)
            {
                smokeT += dt;
                float wait = menuSmoke || resultSmoke || winSmoke || loseSmoke ? 1.15f : clearTest ? 1.4f : 3.4f;
                if (smokeT >= wait)
                {
                    string dest = shot ?? Path.Combine(AppContext.BaseDirectory, "smoke.png");
                    try
                    {
                        string? dir = Path.GetDirectoryName(dest);
                        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                        Image grab = Raylib.LoadImageFromScreen();
                        Raylib.ExportImage(grab, dest);
                        Raylib.UnloadImage(grab);
                    }
                    catch (Exception ex)
                    {
                        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "shot.log"), ex.ToString());
                    }
                    break;
                }
            }
        }

        Raylib.ShowCursor();
        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }

    static void StartGame(World world, AudioBus audio, MenuController menu, ref Screen screen)
    {
        world.AutoPlay = false;
        world.StartNew();
        screen = Screen.Playing;
        menu.Reset();
        audio.PlayBattle();
    }

    static void Quit() => Raylib.CloseWindow();
}
