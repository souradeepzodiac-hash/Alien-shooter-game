using System.Numerics;
using Raylib_cs;

namespace VoidHunter;

enum EnemyKind { Scout, Strafer, Bruiser, Wasp, Spitter, Boss, Prism, Hunter, Wraith, Spire, Hydra }
enum WeaponKind { Pulse, Spread, Rail, Nova }
enum PickupKind { Health, Weapon, Shield, Overdrive, Star }
enum BulletOwner { Player, Enemy }

sealed class Bullet
{
    public bool Alive;
    public BulletOwner Owner;
    public Vector2 Pos, Vel;
    public float Alt, VelAlt;
    public float Radius, Damage, Life, Splash;
    public int PierceLeft;
    public Color Tint;
    public WeaponKind Style;
}

sealed class Enemy
{
    public bool Alive;
    public EnemyKind Kind;
    public Vector2 Pos, Vel;
    public float Alt, Angle, Radius, Hp, MaxHp, Contact, Score;
    public float FireCd, Age, Flash, SpawnIn, ChargeT, ChargeCd;
    public float Spiral;
    public int Phase;
}

sealed class Pickup
{
    public bool Alive;
    public PickupKind Kind;
    public Vector2 Pos;
    public float Age, Life = 14f;
}

sealed class Particle
{
    public bool Alive;
    public Vector2 Pos, Vel;
    public float Alt, VelAlt;
    public float Life, MaxLife, Size, Drag;
    public Color Color;
    public bool Additive;
}

sealed class RingFx
{
    public bool Alive;
    public Vector2 Pos;
    public float Alt;
    public float Life, MaxLife, Radius, Grow;
    public Color Color;
}

sealed class Floater
{
    public bool Alive;
    public Vector2 Pos;
    public float Alt;
    public string Text = "";
    public float Life, MaxLife;
    public Color Color;
}

sealed class Boom
{
    public bool Alive;
    public Vector3 Pos;
    public float Life, MaxLife, Size;
    public Color Color;
}

sealed class StarPlanet
{
    public string Name = "";
    public Vector3 Pos;
    public float Radius;
    public Color Color;
    public bool Visited;
}

sealed class Player
{
    public Vector2 Pos, Vel;
    public Vector3 Vel3;
    public Vector3 Aim3 = new(0, 0, -1);
    public float Alt = 1.2f;
    public float Yaw;
    public float Pitch = -0.18f;
    public float Angle = -MathF.PI / 2f;
    public float Radius = 22f;
    public float Hp = 120f, MaxHp = 120f;
    public float Shield, MaxShield = 60f;
    public float IFrames, DashT, DashCd, FireCd, Overdrive, HurtFlash;
    public bool Alive = true;
    public WeaponKind Weapon = WeaponKind.Pulse;
    public readonly int[] Levels = [1, 0, 0, 0];
}

sealed class World
{
    public readonly Player Player = new();
    public readonly List<Enemy> Enemies = [];
    public readonly List<Bullet> Bullets = [];
    public readonly List<Pickup> Pickups = [];
    public readonly List<Particle> Particles = [];
    public readonly List<RingFx> Rings = [];
    public readonly List<Floater> Floaters = [];
    public readonly List<Boom> Booms = [];
    public readonly List<StarPlanet> Planets = [];
    public string NearPlanet = "";

    public const int FinalLevel = 10;
    public const float WorldScale = 0.045f;
    public int Chapter = 1;
    public int Score, Wave, Combo = 1, ComboKills;
    public int Kills, LevelKills, LevelScore, ClearBonus, Stars;
    public float ComboT, Shake, BannerT, WaveRest, GameOverDelay, Time, LevelTime, HintT;
    public string Banner = "";
    public string Hint = "";
    public string ResultGrade = "C";
    public bool WantsGameOver, WantsLevelClear, WantsVictory, WantsWorldGate, NewHigh, EndedInAbyss;
    public bool AutoPlay;
    public bool IsAbyss => Chapter >= 2;
    public string WorldName => IsAbyss ? "STAR SKY" : "RIFT";
    public Rectangle Playfield;
    public Vector2 ShakeOff;
    public Camera3D Cam;

    readonly AudioBus _audio;
    readonly List<(EnemyKind Kind, float Delay)> _queue = [];
    float _spawnWait;
    int _bossIndex;

    public World(AudioBus audio) => _audio = audio;

    public void StartNew()
    {
        Enemies.Clear(); Bullets.Clear(); Pickups.Clear();
        Particles.Clear(); Rings.Clear(); Floaters.Clear(); Booms.Clear();
        _queue.Clear();
        Score = 0; Wave = 0; Combo = 1; ComboKills = 0;
        Kills = 0; LevelKills = 0; LevelScore = 0; ClearBonus = 0;
        ComboT = 0; Shake = 0; BannerT = 0; WaveRest = 0.6f;
        GameOverDelay = 0; Time = 0; LevelTime = 0;
        WantsGameOver = false; WantsLevelClear = false; WantsVictory = false; WantsWorldGate = false; NewHigh = false;
        EndedInAbyss = false;
        ResultGrade = "C";
        Chapter = 1;
        _spawnWait = 0; _bossIndex = 0;
        Player.Pos = new Vector2(Playfield.X + Playfield.Width * 0.5f, Playfield.Y + Playfield.Height * 0.72f);
        Player.Vel = Vector2.Zero;
        Player.Hp = Player.MaxHp;
        Player.Shield = 0;
        Player.IFrames = 1.2f;
        Player.DashT = 0; Player.DashCd = 0; Player.FireCd = 0;
        Player.Overdrive = 0; Player.HurtFlash = 0;
        Player.Alive = true;
        Player.Weapon = WeaponKind.Pulse;
        Player.Levels[0] = 1; Player.Levels[1] = 0; Player.Levels[2] = 0; Player.Levels[3] = 0;
        Player.Angle = -MathF.PI / 2f;
        Banner = "ENGAGE";
        BannerT = 1.6f;
    }

    public void SyncPlayfield()
    {
        if (IsAbyss)
        {
            Playfield = new Rectangle(0, 0, 96f / WorldScale, 74f / WorldScale);
            return;
        }
        int w = Raylib.GetScreenWidth();
        int h = Raylib.GetScreenHeight();
        Playfield = new Rectangle(18, 18, w - 36, h - 36);
    }

    public void Update(float dt)
    {
        SyncPlayfield();
        Time += dt;
        if (Shake > 0)
        {
            Shake = MathF.Max(0, Shake - dt * 18f);
            ShakeOff = new Vector2(Rng.Float(-Shake, Shake), Rng.Float(-Shake, Shake));
        }
        else ShakeOff = Vector2.Zero;

        if (ComboT > 0)
        {
            ComboT -= dt;
            if (ComboT <= 0) { Combo = 1; ComboKills = 0; }
        }

        UpdatePlayer(dt);
        UpdateSpawns(dt);
        UpdateEnemies(dt);
        UpdateBullets(dt);
        UpdatePickups(dt);
        Collide();
        Enemies.RemoveAll(e => !e.Alive);
        UpdateFx(dt);

        if (BannerT > 0) BannerT -= dt;
        if (HintT > 0) HintT -= dt;
        if (Wave > 0 && Player.Alive && !WantsLevelClear && !WantsVictory)
            LevelTime += dt;

        TryResolveLevel();

        if (!Player.Alive)
        {
            GameOverDelay += dt;
            if (GameOverDelay > 1.35f && !WantsGameOver)
            {
                FinishRun(won: false);
                WantsGameOver = true;
            }
        }
    }

    void UpdatePlayer(float dt)
    {
        if (!Player.Alive) return;
        Player.FireCd = MathF.Max(0, Player.FireCd - dt);
        Player.DashCd = MathF.Max(0, Player.DashCd - dt);
        Player.IFrames = MathF.Max(0, Player.IFrames - dt);
        Player.HurtFlash = MathF.Max(0, Player.HurtFlash - dt);
        Player.Overdrive = MathF.Max(0, Player.Overdrive - dt);
        if (Player.DashT > 0) Player.DashT = MathF.Max(0, Player.DashT - dt);

        Vector2 wish = Vector2.Zero;
        Vector2 aim = Raylib.GetMousePosition() - Player.Pos;
        bool fire = Raylib.IsMouseButtonDown(MouseButton.Left) || Raylib.IsKeyDown(KeyboardKey.Space);
        bool dash = Raylib.IsMouseButtonPressed(MouseButton.Right)
                    || Raylib.IsKeyPressed(KeyboardKey.LeftShift)
                    || Raylib.IsKeyPressed(KeyboardKey.RightShift);

        if (AutoPlay)
        {
            wish = V.FromAngle(Time * 1.3f);
            Enemy? n = NearestEnemy();
            aim = n is null ? new Vector2(0, -1) : n.Pos - Player.Pos;
            fire = true;
            if (Player.DashCd <= 0 && Rng.Chance(0.008f)) dash = true;
        }
        else if (!IsAbyss)
        {
            if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) wish.X -= 1;
            if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) wish.X += 1;
            if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up)) wish.Y -= 1;
            if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down)) wish.Y += 1;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.One)) TrySetWeapon(WeaponKind.Pulse);
        if (Raylib.IsKeyPressed(KeyboardKey.Two)) TrySetWeapon(WeaponKind.Spread);
        if (Raylib.IsKeyPressed(KeyboardKey.Three)) TrySetWeapon(WeaponKind.Rail);
        if (Raylib.IsKeyPressed(KeyboardKey.Four)) TrySetWeapon(WeaponKind.Nova);
        float wheel = Raylib.GetMouseWheelMove();
        if (wheel > 0) CycleWeapon(1);
        if (wheel < 0) CycleWeapon(-1);

        if (IsAbyss && !AutoPlay)
        {
            PilotAbyss(dt, dash);
        }
        else
        {
            if (wish.LengthSquared() > 0) wish = V.Norm(wish);
            float speed = Player.DashT > 0 ? 820f : 355f;
            if (dash && Player.DashCd <= 0)
            {
                Vector2 dir = wish.LengthSquared() > 0.01f ? wish : V.FromAngle(Player.Angle);
                Player.DashT = 0.16f;
                Player.DashCd = 2.15f;
                Player.IFrames = MathF.Max(Player.IFrames, 0.16f);
                speed = 820f;
                wish = dir;
                _audio.Dash();
                Burst(Player.Pos, -dir * 80f, 14, Col.Rgba(120, 230, 255), 220, 9);
                Ring(Player.Pos, 18, 280, Col.Rgba(80, 210, 255), 0.28f);
            }
            Player.Vel = wish * speed;
            Player.Pos += Player.Vel * dt;
            Player.Pos = V.ClampTo(Player.Pos, Playfield, Player.Radius);
            if (aim.LengthSquared() > 16f) Player.Angle = V.Ang(aim);
        }

        if (fire && Player.FireCd <= 0)
            FireWeapon();

        if ((IsAbyss ? Player.Vel.LengthSquared() > 1f : wish.LengthSquared() > 0.1f) || Player.DashT > 0)
        {
            Vector2 back = Player.Pos - V.FromAngle(Player.Angle) * 18f;
            SpawnParticle(back, -V.FromAngle(Player.Angle) * Rng.Float(40, 120) + V.Perp(V.FromAngle(Player.Angle)) * Rng.Float(-30, 30),
                Rng.Float(0.12f, 0.28f), Rng.Float(5, 11), IsAbyss ? Rainbow(Time * 2f) : Col.Rgba(80, 230, 255, 200), true, IsAbyss ? Player.Alt : 0f);
        }
    }

    void PilotAbyss(float dt, bool dash)
    {
        if (Cam.FovY < 1f) RefreshCamera();
        AimPlaneAtMouse();

        Vector2 wish = Vector2.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) wish.X -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) wish.X += 1;
        if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up)) wish.Y -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down)) wish.Y += 1;
        if (wish.LengthSquared() > 0) wish = V.Norm(wish);

        float speed = Player.DashT > 0 ? 1400f : 820f;
        if (dash && Player.DashCd <= 0)
        {
            Vector2 dir = wish.LengthSquared() > 0.01f ? wish : new Vector2(MathF.Sin(Player.Yaw), -MathF.Cos(Player.Yaw));
            wish = V.Norm(dir);
            speed = 980f;
            Player.DashT = 0.16f;
            Player.DashCd = 2.15f;
            Player.IFrames = MathF.Max(Player.IFrames, 0.16f);
            _audio.Dash();
            Burst(Player.Pos, -wish * 80f, 14, Col.Rgba(120, 230, 255), 220, 9);
            Ring(Player.Pos, 18, 280, Col.Rgba(80, 210, 255), 0.28f);
        }

        Player.Vel = wish * speed;
        Player.Vel3 = Vector3.Zero;
        Player.Pos += Player.Vel * dt;

        float climb = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.E)) climb += 28f;
        if (Raylib.IsKeyDown(KeyboardKey.Q) || Raylib.IsKeyDown(KeyboardKey.LeftControl)) climb -= 28f;
        Player.Alt += climb * dt;
        Player.Alt = Math.Clamp(Player.Alt, 0.35f, 260f);
        TryLandPlanets();
        RefreshCamera();
    }

    void AimPlaneAtMouse()
    {
        // Same as Rift: nose points at the cursor. Camera stays put so it does not spin.
        Vector2 mouse = Raylib.GetMousePosition();
        Vector2 ship = Raylib.GetWorldToScreen(ToWorld(Player.Pos, Player.Alt), Cam);
        Vector2 d = mouse - ship;
        if (d.LengthSquared() > 8f)
        {
            Player.Angle = MathF.Atan2(d.Y, d.X);
            Player.Yaw = MathF.Atan2(d.X, -d.Y);
        }
        Player.Pitch = 0f;
        float cs = MathF.Cos(Player.Yaw);
        float sn = MathF.Sin(Player.Yaw);
        Player.Aim3 = new Vector3(sn, 0f, -cs);
    }

    void TrySetWeapon(WeaponKind w)
    {
        if (Player.Levels[(int)w] > 0)
            Player.Weapon = w;
    }

    void CycleWeapon(int dir)
    {
        int i = (int)Player.Weapon;
        for (int n = 0; n < 4; n++)
        {
            i = (i + dir + 4) % 4;
            if (Player.Levels[i] > 0) { Player.Weapon = (WeaponKind)i; return; }
        }
    }

    void FireWeapon()
    {
        int lv = Math.Max(1, Player.Levels[(int)Player.Weapon]);
        float od = Player.Overdrive > 0 ? 0.62f : 1f;
        Vector2 dir = V.FromAngle(Player.Angle);
        float reach = IsAbyss ? 1.85f : 1f;
        if (IsAbyss)
        {
            dir = new Vector2(MathF.Sin(Player.Yaw), -MathF.Cos(Player.Yaw));
            if (dir.LengthSquared() < 0.0001f) dir = V.FromAngle(Player.Angle);
            else dir = V.Norm(dir);
        }
        Vector2 muzzle = Player.Pos + dir * (IsAbyss ? 80f : 28f);

        switch (Player.Weapon)
        {
            case WeaponKind.Pulse:
                Player.FireCd = (lv >= 3 ? 0.075f : lv == 2 ? 0.09f : 0.11f) * od;
                int extra = lv >= 2 ? 1 : 0;
                for (int i = 0; i <= extra; i++)
                {
                    Vector2 off = extra > 0 ? V.Perp(dir) * (i == 0 ? -8f : 8f) : Vector2.Zero;
                    SpawnBullet(BulletOwner.Player, muzzle + off, dir * 940f * reach, 5.5f, 12 + lv * 2, 1.1f * reach, 0, 0,
                        IsAbyss ? Rainbow(Time * 0.9f + i * 0.12f) : Col.Rgba(90, 240, 255), WeaponKind.Pulse);
                }
                break;
            case WeaponKind.Spread:
                Player.FireCd = (lv >= 3 ? 0.14f : 0.17f) * od;
                int shots = lv >= 3 ? 7 : lv == 2 ? 5 : 3;
                float spread = 0.42f + lv * 0.04f;
                for (int i = 0; i < shots; i++)
                {
                    float t = shots == 1 ? 0 : (i / (float)(shots - 1) - 0.5f);
                    float ang = V.Ang(dir) + t * spread;
                    Vector2 d = V.FromAngle(ang);
                    SpawnBullet(BulletOwner.Player, muzzle, d * 820f * reach, 4.5f, 7 + lv, 0.9f * reach, 0, 0,
                        IsAbyss ? Rainbow(Time * 0.8f + i * 0.1f) : Col.Rgba(190, 120, 255), WeaponKind.Spread);
                }
                break;
            case WeaponKind.Rail:
                Player.FireCd = (lv >= 3 ? 0.38f : 0.48f) * od;
                SpawnBullet(BulletOwner.Player, muzzle, dir * 1500f * reach, 7f, 42 + lv * 10, 0.7f * reach, lv >= 2 ? 6 : 3, 0,
                    IsAbyss ? Col.Rgba(255, 90, 220) : Col.Rgba(255, 230, 120), WeaponKind.Rail);
                Shake = MathF.Max(Shake, 3.5f);
                break;
            default:
                Player.FireCd = (lv >= 3 ? 0.42f : 0.55f) * od;
                SpawnBullet(BulletOwner.Player, muzzle, dir * 520f * reach, 9f, 26 + lv * 6, 1.4f * reach, 0, (78f + lv * 10) * reach,
                    IsAbyss ? Col.Rgba(255, 170, 40) : Col.Rgba(255, 140, 60), WeaponKind.Nova);
                break;
        }

        _audio.Shoot(Player.Weapon);
        Burst(muzzle, dir, IsAbyss ? 14 : 6, IsAbyss ? Rainbow(Time) : Col.Rgba(200, 240, 255), IsAbyss ? 320 : 180, IsAbyss ? 14 : 5, IsAbyss ? Player.Alt : 0f);
        if (IsAbyss)
            Blast(ToWorld(muzzle, Player.Alt), 2.4f, Rainbow(Time), 0.22f);
    }

    public static Color Rainbow(float t)
    {
        t = t - MathF.Floor(t);
        float a = t * MathF.Tau;
        return Col.Rgba(
            (int)(140 + 115 * MathF.Sin(a)),
            (int)(140 + 115 * MathF.Sin(a + 2.1f)),
            (int)(140 + 115 * MathF.Sin(a + 4.2f)));
    }

    void TryResolveLevel()
    {
        if (!Player.Alive || WantsLevelClear || WantsVictory || WantsGameOver || WantsWorldGate)
            return;
        if (Wave <= 0 || WaveRest > 0)
            return;
        if (_queue.Count > 0 || Enemies.Count > 0)
            return;

        if (AutoPlay)
        {
            if (Wave >= FinalLevel) FinishRun(won: true);
            else ContinueNextLevel();
            return;
        }

        SealLevel();
        if (Wave >= FinalLevel)
        {
            if (Chapter == 1)
            {
                WantsLevelClear = true;
                WantsWorldGate = true;
            }
            else FinishRun(won: true);
        }
        else WantsLevelClear = true;
    }

    void UpdateSpawns(float dt)
    {
        if (WaveRest > 0)
        {
            WaveRest -= dt;
            if (WaveRest <= 0)
                BeginWave(Wave + 1);
            return;
        }

        if (_queue.Count == 0)
        {
            _spawnWait = 0;
            return;
        }

        _spawnWait -= dt;
        while (_queue.Count > 0 && _spawnWait <= 0)
        {
            var (kind, delay) = _queue[0];
            _queue.RemoveAt(0);
            SpawnEnemy(kind, EdgePoint());
            _spawnWait = _queue.Count == 0 ? 0f : delay;
        }
    }

    void BeginWave(int wave)
    {
        Wave = wave;
        LevelKills = 0;
        LevelScore = 0;
        LevelTime = 0;
        _queue.Clear();
        _audio.Wave();
        bool boss = wave % 5 == 0;
        void Add(EnemyKind k, int n, float gap)
        {
            for (int i = 0; i < n; i++) _queue.Add((k, gap));
        }

        if (IsAbyss)
        {
            if (boss)
            {
                _bossIndex++;
                _queue.Add((EnemyKind.Hydra, 0.1f));
                int escorts = 5 + _bossIndex;
                for (int i = 0; i < escorts; i++)
                    _queue.Add((Rng.Chance(0.5f) ? EnemyKind.Wraith : EnemyKind.Prism, 0.16f));
                Banner = "BIG ALIEN BOSS!";
                BannerT = 2.6f;
                _audio.Boss();
                for (int i = 1; i < _queue.Count; i++)
                {
                    int j = Rng.Int(1, _queue.Count);
                    (_queue[i], _queue[j]) = (_queue[j], _queue[i]);
                }
                return;
            }
            if (wave == 1) Add(EnemyKind.Prism, 8, 0.32f);
            else if (wave == 2) { Add(EnemyKind.Prism, 6, 0.28f); Add(EnemyKind.Hunter, 3, 0.45f); }
            else if (wave == 3) { Add(EnemyKind.Wraith, 10, 0.22f); Add(EnemyKind.Spire, 2, 0.55f); }
            else
            {
                Add(EnemyKind.Prism, 5 + wave, 0.22f);
                Add(EnemyKind.Hunter, 2 + wave / 3, 0.34f);
                Add(EnemyKind.Wraith, 4 + wave / 2, 0.2f);
                Add(EnemyKind.Spire, Math.Max(1, wave / 3), 0.5f);
            }
            for (int i = 0; i < _queue.Count; i++)
            {
                int j = Rng.Int(0, _queue.Count);
                (_queue[i], _queue[j]) = (_queue[j], _queue[i]);
                var (k, d) = _queue[i];
                _queue[i] = (k, d * Rng.Float(0.4f, 1.85f));
            }
            Banner = wave == 1 ? "CATCH THE STARS!" : $"LEVEL {wave}  GO GO GO!";
            BannerT = 2.1f;
            return;
        }

        if (boss)
        {
            _bossIndex++;
            _queue.Add((EnemyKind.Boss, 0.1f));
            int escorts = 4 + _bossIndex;
            for (int i = 0; i < escorts; i++)
                _queue.Add((Rng.Chance(0.5f) ? EnemyKind.Wasp : EnemyKind.Scout, 0.18f));
            Banner = _bossIndex == 1 ? "LEVIATHAN" : $"LEVIATHAN {_bossIndex}";
            BannerT = 2.4f;
            _audio.Boss();
            return;
        }

        if (wave == 1) Add(EnemyKind.Scout, 8, 0.28f);
        else if (wave == 2) { Add(EnemyKind.Scout, 6, 0.22f); Add(EnemyKind.Strafer, 3, 0.45f); }
        else if (wave == 3) { Add(EnemyKind.Scout, 6, 0.2f); Add(EnemyKind.Wasp, 8, 0.12f); Add(EnemyKind.Bruiser, 2, 0.6f); }
        else
        {
            int scouts = 4 + wave;
            int wasps = 4 + wave / 2;
            int strafers = 2 + wave / 3;
            int spit = Math.Max(0, wave / 3);
            int tanks = Math.Max(1, wave / 4);
            Add(EnemyKind.Scout, scouts, 0.16f);
            Add(EnemyKind.Wasp, wasps, 0.1f);
            Add(EnemyKind.Strafer, strafers, 0.32f);
            Add(EnemyKind.Spitter, spit, 0.5f);
            Add(EnemyKind.Bruiser, tanks, 0.55f);
        }
    }

    public void JumpToWave(int wave)
    {
        Wave = Math.Max(0, wave - 1);
        WaveRest = 0.15f;
        Enemies.Clear();
        _queue.Clear();
        WantsLevelClear = false;
        WantsVictory = false;
        WantsWorldGate = false;
        Banner = wave % 5 == 0 ? (IsAbyss ? "HYDRA SIGNATURE" : "RIFT SIGNATURE") : $"LEVEL {wave}";
        BannerT = 1.6f;
    }

    public void SealLevel()
    {
        float hull = Player.MaxHp <= 0 ? 0 : Player.Hp / Player.MaxHp;
        ResultGrade = hull >= 0.85f ? "S" : hull >= 0.65f ? "A" : hull >= 0.4f ? "B" : hull >= 0.2f ? "C" : "D";
        ClearBonus = 150 + Wave * 50 + (Wave % 5 == 0 ? 400 : 0);
        if (ResultGrade is "S" or "A") ClearBonus += 200;
        Score += ClearBonus;
        LevelScore += ClearBonus;
        Banner = "";
        BannerT = 0;
        _audio.Wave();
    }

    public void FinishRun(bool won)
    {
        NewHigh = SaveData.TryRecord(Score);
        if (won)
        {
            EndedInAbyss = IsAbyss;
            WantsVictory = true;
            WantsLevelClear = false;
            WantsWorldGate = false;
            Banner = "VICTORY";
            BannerT = 2f;
            _audio.Boss();
        }
    }

    public void ContinueNextLevel()
    {
        WantsLevelClear = false;
        WantsVictory = false;
        WantsWorldGate = false;
        Player.Hp = MathF.Min(Player.MaxHp, Player.Hp + 18f);
        Player.IFrames = 0.8f;
        WaveRest = 0.35f;
        Banner = (Wave + 1) % 5 == 0 ? (IsAbyss ? "HYDRA SIGNATURE" : "RIFT SIGNATURE") : $"LEVEL {Wave + 1}";
        BannerT = 1.8f;
        _audio.Wave();
    }

    public void RetryCurrentLevel()
    {
        WantsGameOver = false;
        WantsLevelClear = false;
        WantsVictory = false;
        WantsWorldGate = false;
        GameOverDelay = 0;
        Enemies.Clear();
        Bullets.Clear();
        Pickups.Clear();
        Booms.Clear();
        Particles.Clear();
        _queue.Clear();
        _spawnWait = 0;
        Player.Alive = true;
        Player.Hp = Player.MaxHp;
        Player.Shield = 0;
        Player.IFrames = 1.1f;
        Player.HurtFlash = 0;
        Player.DashT = 0;
        Player.DashCd = 0;
        Player.Pos = new Vector2(Playfield.X + Playfield.Width * 0.5f, Playfield.Y + Playfield.Height * 0.72f);
        Player.Vel = Vector2.Zero;
        int retry = Math.Max(1, Wave);
        Wave = retry - 1;
        WaveRest = 0.25f;
        Banner = retry % 5 == 0 ? (IsAbyss ? "HYDRA SIGNATURE" : "RIFT SIGNATURE") : $"LEVEL {retry}";
        BannerT = 1.6f;
    }

    public void SimulateWaveCleared(int wave)
    {
        StartNew();
        Banner = "";
        BannerT = 0;
        Wave = Math.Max(1, wave);
        WaveRest = 0;
        _queue.Clear();
        Enemies.Clear();
        _spawnWait = 0.28f;
        WantsLevelClear = false;
        WantsVictory = false;
        Player.Alive = true;
        Player.Hp = 90;
        Score = 480;
        Kills = 8;
        LevelKills = 8;
        LevelScore = 480;
        LevelTime = 12;
    }

    public void PrepareDemoResult(string kind)
    {
        StartNew();
        Banner = "";
        BannerT = 0;
        NewHigh = false;
        if (kind == "win")
        {
            Wave = FinalLevel; Score = 18640; Kills = 146; LevelKills = 18;
            LevelScore = 3200; LevelTime = 74; Player.Hp = 96; Player.Alive = true;
            ClearBonus = 650; ResultGrade = "S"; WantsVictory = true;
        }
        else if (kind == "lose")
        {
            Wave = 4; Score = 2480; Kills = 31; LevelKills = 7;
            LevelScore = 540; LevelTime = 38; Player.Hp = 0; Player.Alive = false;
            ClearBonus = 0; ResultGrade = "D"; WantsGameOver = true;
        }
        else if (kind == "gate")
        {
            Wave = FinalLevel; Chapter = 1; Score = 12400; Kills = 98; LevelKills = 16;
            LevelScore = 2800; LevelTime = 68; Player.Hp = 88; Player.Alive = true;
            ClearBonus = 900; ResultGrade = "S"; WantsLevelClear = true; WantsWorldGate = true;
        }
        else if (kind == "abyss")
        {
            EnterAbyss();
            WaveRest = 0.2f;
        }
        else
        {
            Wave = 3; Score = 3120; Kills = 28; LevelKills = 9;
            LevelScore = 880; LevelTime = 41; Player.Hp = 84; Player.Alive = true;
            ClearBonus = 350; ResultGrade = "A"; WantsLevelClear = true;
        }
    }

    public void EnterAbyss()
    {
        Chapter = 2;
        WantsLevelClear = false;
        WantsWorldGate = false;
        WantsVictory = false;
        WantsGameOver = false;
        Enemies.Clear();
        Bullets.Clear();
        Pickups.Clear();
        Particles.Clear();
        Rings.Clear();
        Floaters.Clear();
        Booms.Clear();
        _queue.Clear();
        _spawnWait = 0;
        _bossIndex = 0;
        Wave = 0;
        WaveRest = 0.85f;
        LevelKills = 0;
        LevelScore = 0;
        LevelTime = 0;
        Player.Hp = Player.MaxHp;
        Player.Shield = MathF.Min(Player.MaxShield, Player.Shield + 20f);
        Player.IFrames = 1.4f;
        Player.Alive = true;
        Player.HurtFlash = 0;
        SyncPlayfield();
        Player.Pos = new Vector2(Playfield.X + Playfield.Width * 0.5f, Playfield.Y + Playfield.Height * 0.5f);
        Player.Vel = Vector2.Zero;
        Player.Vel3 = Vector3.Zero;
        Player.Alt = 4.5f;
        Player.Aim3 = new Vector3(0, 0, -1);
        Player.Yaw = 0f;
        Player.Pitch = -0.18f;
        Stars = 0;
        Banner = "LET'S FLY!";
        BannerT = 2.5f;
        Hint = "MOVE MOUSE TO LOOK  •  ARROWS FLY  •  FLY INTO PLANETS TO LAND";
        HintT = 7f;
        SeedPlanets();
        RefreshCamera();
        _audio.Boss();
    }

    public Vector3 ToWorld(Vector2 p, float alt = 0f)
    {
        float cx = Playfield.X + Playfield.Width * 0.5f;
        float cy = Playfield.Y + Playfield.Height * 0.5f;
        return new Vector3((p.X - cx) * WorldScale, alt, (p.Y - cy) * WorldScale);
    }

    public Vector2 FromWorld(Vector3 w)
    {
        float cx = Playfield.X + Playfield.Width * 0.5f;
        float cy = Playfield.Y + Playfield.Height * 0.5f;
        return new Vector2(w.X / WorldScale + cx, w.Z / WorldScale + cy);
    }

    public Vector3 LookDir()
    {
        float cp = MathF.Cos(Player.Pitch);
        var d = new Vector3(MathF.Sin(Player.Yaw) * cp, MathF.Sin(Player.Pitch), -MathF.Cos(Player.Yaw) * cp);
        return d.LengthSquared() > 0.0001f ? Vector3.Normalize(d) : new Vector3(0, 0, -1);
    }

    public void RefreshCamera()
    {
        Vector3 p = ToWorld(Player.Pos, Player.Alt);
        Vector3 shake = new(ShakeOff.X * 0.02f, 0f, ShakeOff.Y * 0.02f);
        int sw = Math.Max(1, Raylib.GetScreenWidth());
        int sh = Math.Max(1, Raylib.GetScreenHeight());
        Vector2 m = Raylib.GetMousePosition();
        float mx = Math.Clamp(m.X / sw * 2f - 1f, -1f, 1f);
        float my = Math.Clamp(m.Y / sh * 2f - 1f, -1f, 1f);
        float yaw = mx * 1.05f;
        float pitch = Math.Clamp(0.48f - my * 0.92f, -0.50f, 1.28f);
        float dist = 31f;
        float cp = MathF.Cos(pitch);
        Vector3 back = new(
            MathF.Sin(yaw) * cp * dist,
            MathF.Sin(pitch) * dist + 1.4f,
            MathF.Cos(yaw) * cp * dist);
        Cam.Position = p + back + shake;
        Cam.Target = p + new Vector3(0f, 1.1f, 0f) + shake;
        Cam.Up = Vector3.UnitY;
        Cam.FovY = 58f;
        Cam.Projection = CameraProjection.Perspective;
    }

    public float CombatRadius(Enemy e) => IsAbyss ? MathF.Max(e.Radius * 4f, 64f) : e.Radius;
    public float PlayerCombatRadius() => IsAbyss ? 56f : Player.Radius;

    Vector2 VisibleEdgePoint()
    {
        if (Cam.FovY < 1f) RefreshCamera();
        int sw = Raylib.GetScreenWidth();
        int sh = Raylib.GetScreenHeight();
        int side = Rng.Int(0, 3);
        Vector2 scr = side switch
        {
            0 => new Vector2(Rng.Float(70, sw - 70), 36),
            1 => new Vector2(40, Rng.Float(60, sh * 0.52f)),
            _ => new Vector2(sw - 40, Rng.Float(60, sh * 0.52f)),
        };
        Ray ray = Raylib.GetScreenToWorldRay(scr, Cam);
        float t = 22f;
        if (MathF.Abs(ray.Direction.Y) > 0.0012f)
        {
            float hit = (Player.Alt - ray.Position.Y) / ray.Direction.Y;
            if (hit > 6f) t = hit;
        }
        Vector3 wpos = ray.Position + ray.Direction * t;
        wpos.Y = Player.Alt;
        return FromWorld(wpos);
    }

    void KeepEnemyInView(Enemy e)
    {
        Vector3 ew = ToWorld(e.Pos, e.Alt);
        Vector3 camF = Cam.Target - Cam.Position;
        if (camF.LengthSquared() < 0.01f) return;
        camF = Vector3.Normalize(camF);
        if (Vector3.Dot(ew - Cam.Position, camF) < 6f)
        {
            Vector3 front = ToWorld(Player.Pos, e.Alt) + new Vector3(Rng.Float(-10f, 10f), 0f, -16f);
            e.Pos = FromWorld(front);
            return;
        }
        Vector2 sp = Raylib.GetWorldToScreen(ew, Cam);
        int sw = Raylib.GetScreenWidth();
        int sh = Raylib.GetScreenHeight();
        if (sp.X < 8 || sp.X > sw - 8 || sp.Y < 8 || sp.Y > sh * 0.88f)
            e.Vel = V.Norm(Player.Pos - e.Pos) * 260f;
    }

    void SeedPlanets()
    {
        Planets.Clear();
        NearPlanet = "";
        Planets.Add(new StarPlanet { Name = "CANDY MOON", Pos = new Vector3(0, 8, -28), Radius = 18f, Color = Col.Rgba(255, 220, 90) });
        Planets.Add(new StarPlanet { Name = "MINT RING", Pos = new Vector3(-26, 9, -18), Radius = 15f, Color = Col.Rgba(90, 230, 200) });
        Planets.Add(new StarPlanet { Name = "BERRY WORLD", Pos = new Vector3(26, 8, -16), Radius = 15f, Color = Col.Rgba(255, 120, 180) });
        Planets.Add(new StarPlanet { Name = "STAR DOCK", Pos = new Vector3(0, 12, -48), Radius = 20f, Color = Col.Rgba(120, 180, 255) });
    }

    void TryLandPlanets()
    {
        NearPlanet = "";
        Vector3 p = ToWorld(Player.Pos, Player.Alt);
        foreach (StarPlanet pl in Planets)
        {
            float dx = p.X - pl.Pos.X;
            float dz = p.Z - pl.Pos.Z;
            float dist = MathF.Sqrt(dx * dx + dz * dz);
            if (dist < pl.Radius * 8f)
                NearPlanet = dist < pl.Radius + 6f
                    ? (pl.Visited ? $"ON  {pl.Name}" : $"LAND ON  {pl.Name}  — FLY INTO IT")
                    : $"FLY TO  {pl.Name}   {dist:0} m";
            if (dist < pl.Radius + 8f && MathF.Abs(p.Y - pl.Pos.Y) < pl.Radius + 14f)
            {
                Player.Alt = pl.Pos.Y + pl.Radius * 0.45f;
                if (!pl.Visited)
                {
                    pl.Visited = true;
                    Banner = $"TOUCHED  {pl.Name}!";
                    BannerT = 2.4f;
                    Stars += 8;
                    Score += 250;
                    LevelScore += 250;
                    Blast(pl.Pos + new Vector3(0, pl.Radius * 0.5f, 0), 6.5f, pl.Color, 0.7f);
                    SmokePuff(Player.Pos, Player.Alt, false);
                    Float(Player.Pos, "+PLANET", pl.Color, Player.Alt);
                    _audio.Wave();
                }
            }
        }
    }

    Vector2 EdgePoint()
    {
        if (IsAbyss)
            return VisibleEdgePoint();
        int side = Rng.Int(0, 4);
        float m = 36f;
        return side switch
        {
            0 => new Vector2(Rng.Float(Playfield.X, Playfield.X + Playfield.Width), Playfield.Y - m),
            1 => new Vector2(Rng.Float(Playfield.X, Playfield.X + Playfield.Width), Playfield.Y + Playfield.Height + m),
            2 => new Vector2(Playfield.X - m, Rng.Float(Playfield.Y, Playfield.Y + Playfield.Height)),
            _ => new Vector2(Playfield.X + Playfield.Width + m, Rng.Float(Playfield.Y, Playfield.Y + Playfield.Height)),
        };
    }

    void SpawnEnemy(EnemyKind kind, Vector2 pos)
    {
        var e = new Enemy { Alive = true, Kind = kind, Pos = pos, Age = 0, SpawnIn = 0.45f };
        float scale = 1f + (Wave - 1) * 0.06f;
        switch (kind)
        {
            case EnemyKind.Scout:
                e.Radius = 18; e.MaxHp = 22 * scale; e.Contact = 10; e.Score = 60; break;
            case EnemyKind.Strafer:
                e.Radius = 22; e.MaxHp = 40 * scale; e.Contact = 12; e.Score = 90; e.FireCd = Rng.Float(0.4f, 1.1f); break;
            case EnemyKind.Bruiser:
                e.Radius = 28; e.MaxHp = 140 * scale; e.Contact = 22; e.Score = 180; e.ChargeCd = Rng.Float(1.2f, 2.4f); break;
            case EnemyKind.Wasp:
                e.Radius = 13; e.MaxHp = 10 * scale; e.Contact = 7; e.Score = 30; break;
            case EnemyKind.Spitter:
                e.Radius = 23; e.MaxHp = 55 * scale; e.Contact = 12; e.Score = 130; e.FireCd = Rng.Float(0.5f, 1.2f); break;
            case EnemyKind.Prism:
                e.Radius = 20; e.MaxHp = 34 * scale; e.Contact = 12; e.Score = 80; e.FireCd = Rng.Float(0.5f, 1.2f); break;
            case EnemyKind.Hunter:
                e.Radius = 20; e.MaxHp = 70 * scale; e.Contact = 18; e.Score = 140; e.ChargeCd = Rng.Float(0.8f, 1.8f); break;
            case EnemyKind.Wraith:
                e.Radius = 16; e.MaxHp = 18 * scale; e.Contact = 9; e.Score = 70; e.FireCd = Rng.Float(0.8f, 1.6f); break;
            case EnemyKind.Spire:
                e.Radius = 22; e.MaxHp = 90 * scale; e.Contact = 14; e.Score = 160; e.FireCd = Rng.Float(0.6f, 1.3f); break;
            case EnemyKind.Hydra:
                e.Radius = 68; e.MaxHp = 1100 + _bossIndex * 380; e.Contact = 30; e.Score = 3200 + _bossIndex * 900;
                e.SpawnIn = 1.1f; e.FireCd = 0.7f; e.Phase = 1;
                pos = VisibleEdgePoint();
                e.Pos = pos;
                Ring(pos, 22, 460, Col.Rgba(180, 60, 255), 0.7f);
                Shake = 9;
                break;
            default:
                e.Radius = 62; e.MaxHp = 850 + _bossIndex * 320; e.Contact = 28; e.Score = 2500 + _bossIndex * 800;
                e.SpawnIn = 1.1f; e.FireCd = 0.8f; e.Phase = 1;
                pos = Player.Pos + new Vector2(0, -300);
                e.Pos = pos;
                Ring(pos, 20, 420, Col.Rgba(255, 80, 40), 0.7f);
                Shake = 8;
                break;
        }
        e.Hp = e.MaxHp;
        e.Angle = V.Ang(Player.Pos - e.Pos);
        e.Spiral = Rng.Float(0, MathF.Tau);
        if (IsAbyss) e.Alt = Player.Alt + Rng.Float(-3.5f, 6f);
        Enemies.Add(e);
        Ring(e.Pos, 8, 140, Col.Rgba(180, 80, 255), 0.35f);
    }

    void UpdateEnemies(float dt)
    {
        Vector2 p = Player.Pos;
        for (int i = Enemies.Count - 1; i >= 0; i--)
        {
            Enemy e = Enemies[i];
            if (!e.Alive) { Enemies.RemoveAt(i); continue; }
            e.Age += dt;
            e.Flash = MathF.Max(0, e.Flash - dt);
            if (e.SpawnIn > 0)
            {
                e.SpawnIn -= dt;
                e.Angle = V.Ang(p - e.Pos);
                if (IsAbyss)
                    e.Alt += (Player.Alt - e.Alt) * MathF.Min(1f, 3f * dt);
                continue;
            }

            Vector2 to = p - e.Pos;
            float dist = MathF.Max(1f, to.Length());
            Vector2 dir = to / dist;
            e.Angle = V.Ang(to);

            switch (e.Kind)
            {
                case EnemyKind.Scout:
                    e.Vel = Vector2.Lerp(e.Vel, dir * 165f + V.Perp(dir) * MathF.Sin(e.Age * 5f) * 50f, 0.08f);
                    break;
                case EnemyKind.Strafer:
                {
                    float desired = 250f;
                    Vector2 radial = dir * (dist > desired ? 80f : dist < desired - 40 ? -110f : 0);
                    Vector2 tan = V.Perp(dir) * (MathF.Sin(e.Age * 0.7f) >= 0 ? 170f : -170f);
                    e.Vel = Vector2.Lerp(e.Vel, radial + tan, 0.1f);
                    e.FireCd -= dt;
                    if (e.FireCd <= 0 && Player.Alive)
                    {
                        e.FireCd = MathF.Max(0.7f, 1.35f - Wave * 0.04f);
                        Vector2 lead = V.Norm(p + Player.Vel * 0.15f - e.Pos);
                        SpawnBullet(BulletOwner.Enemy, e.Pos + lead * e.Radius, lead * 340f, 5f, 8f, 2.4f, 0, 0,
                            Col.Rgba(255, 90, 200), WeaponKind.Pulse);
                    }
                    break;
                }
                case EnemyKind.Bruiser:
                    e.ChargeCd -= dt;
                    if (e.ChargeT > 0)
                    {
                        e.ChargeT -= dt;
                        e.Vel = dir * 390f;
                    }
                    else
                    {
                        e.Vel = Vector2.Lerp(e.Vel, dir * 88f, 0.06f);
                        if (e.ChargeCd <= 0)
                        {
                            e.ChargeT = 0.48f;
                            e.ChargeCd = 2.6f;
                            Ring(e.Pos, 10, 90, Col.Rgba(255, 90, 40), 0.2f);
                        }
                    }
                    break;
                case EnemyKind.Wasp:
                {
                    Vector2 jitter = V.FromAngle(e.Age * 7f + e.Pos.X * 0.01f) * 90f;
                    e.Vel = Vector2.Lerp(e.Vel, dir * 250f + jitter, 0.14f);
                    break;
                }
                case EnemyKind.Spitter:
                {
                    float desired = 330f;
                    Vector2 radial = dir * (dist > desired + 40 ? 120f : dist < desired - 30 ? -140f : 0);
                    Vector2 tan = V.Perp(dir) * 90f * MathF.Sin(e.Age * 0.9f);
                    e.Vel = Vector2.Lerp(e.Vel, radial + tan, 0.1f);
                    e.FireCd -= dt;
                    if (e.FireCd <= 0 && Player.Alive)
                    {
                        e.FireCd = MathF.Max(0.65f, 1.15f - Wave * 0.03f);
                        Vector2 lead = V.Norm(p + Player.Vel * 0.22f - e.Pos);
                        SpawnBullet(BulletOwner.Enemy, e.Pos + lead * e.Radius, lead * 400f, 6f, 12f, 2.6f, 0, 0,
                            Col.Rgba(80, 255, 170), WeaponKind.Pulse);
                    }
                    break;
                }
                case EnemyKind.Prism:
                {
                    Vector2 tan = V.Perp(dir) * 160f * (MathF.Sin(e.Age * 1.1f) >= 0 ? 1f : -1f);
                    e.Vel = Vector2.Lerp(e.Vel, dir * 70f + tan, 0.1f);
                    e.FireCd -= dt;
                    if (e.FireCd <= 0 && Player.Alive)
                    {
                        e.FireCd = 1.05f;
                        for (int s = -1; s <= 1; s++)
                        {
                            Vector2 d = V.FromAngle(e.Angle + s * 0.28f);
                            SpawnBullet(BulletOwner.Enemy, e.Pos + d * e.Radius, d * 360f, 5f, 9f, 2.2f, 0, 0,
                                Col.Rgba(255, 90, 220), WeaponKind.Spread);
                        }
                    }
                    break;
                }
                case EnemyKind.Hunter:
                    e.ChargeCd -= dt;
                    if (e.ChargeT > 0)
                    {
                        e.ChargeT -= dt;
                        e.Vel = dir * 460f;
                    }
                    else
                    {
                        e.Vel = Vector2.Lerp(e.Vel, dir * 150f, 0.12f);
                        if (e.ChargeCd <= 0)
                        {
                            e.ChargeT = 0.38f;
                            e.ChargeCd = 1.9f;
                            Ring(e.Pos, 8, 80, Col.Rgba(255, 140, 40), 0.18f);
                        }
                    }
                    break;
                case EnemyKind.Wraith:
                    e.Vel = Vector2.Lerp(e.Vel, dir * 210f + V.Perp(dir) * MathF.Sin(e.Age * 6f) * 80f, 0.16f);
                    e.FireCd -= dt;
                    if (e.FireCd <= 0 && Player.Alive)
                    {
                        e.FireCd = 1.8f;
                        e.Pos = p + V.FromAngle(Rng.Float(0, MathF.Tau)) * Rng.Float(160, 260);
                        if (IsAbyss) e.Alt = Player.Alt;
                        else e.Pos = V.ClampTo(e.Pos, Playfield, e.Radius);
                        e.SpawnIn = 0.18f;
                        Vector2 shot = V.Norm(p - e.Pos);
                        SpawnBullet(BulletOwner.Enemy, e.Pos + shot * e.Radius, shot * 380f, 5f, 10f, 2.2f, 0, 0,
                            Col.Rgba(160, 90, 255), WeaponKind.Pulse);
                    }
                    break;
                case EnemyKind.Spire:
                {
                    float desired = 340f;
                    Vector2 radial = dir * (dist > desired + 50 ? 90f : dist < desired - 40 ? -130f : 0);
                    e.Vel = Vector2.Lerp(e.Vel, radial, 0.08f);
                    e.FireCd -= dt;
                    if (e.FireCd <= 0 && Player.Alive)
                    {
                        e.FireCd = 0.85f;
                        Vector2 lead = V.Norm(p + Player.Vel * 0.25f - e.Pos);
                        SpawnBullet(BulletOwner.Enemy, e.Pos + lead * e.Radius, lead * 430f, 7f, 14f, 2.8f, 0, 0,
                            Col.Rgba(255, 200, 80), WeaponKind.Rail);
                    }
                    break;
                }
                default:
                    UpdateBoss(e, dt, dir, dist);
                    break;
            }

            if (IsAbyss)
            {
                HuntPlayer(e, dt);
                Vector2 weave = V.Perp(dir) * MathF.Sin(e.Age * 1.4f + e.Spiral) * 110f;
                e.Vel = Vector2.Lerp(e.Vel, e.Vel + weave, 0.08f);
            }
            e.Pos += e.Vel * dt;
            if (IsAbyss) KeepEnemyInView(e);
            if (!IsAbyss)
            {
                if (e.Kind is EnemyKind.Boss or EnemyKind.Hydra)
                    e.Pos = V.ClampTo(e.Pos, Playfield, e.Radius);
                else
                    e.Pos = V.ClampTo(e.Pos, Expand(Playfield, 8), e.Radius);
            }
        }
    }

    void HuntPlayer(Enemy e, float dt)
    {
        float altGap = Player.Alt - e.Alt;
        e.Alt += altGap * MathF.Min(1f, 3.2f * dt);
        float dist = Vector2.Distance(e.Pos, Player.Pos);
        if (dist > 1400f)
        {
            Vector2 dir = V.Norm(Player.Pos - e.Pos);
            e.Vel = Vector2.Lerp(e.Vel, dir * 420f, 0.06f);
        }
    }

    static Rectangle Expand(Rectangle r, float m) => new(r.X - m, r.Y - m, r.Width + m * 2, r.Height + m * 2);

    void UpdateBoss(Enemy e, float dt, Vector2 dir, float dist)
    {
        if (e.Hp < e.MaxHp * 0.3f) e.Phase = 3;
        else if (e.Hp < e.MaxHp * 0.6f) e.Phase = 2;

        e.ChargeCd -= dt;
        e.FireCd -= dt;
        Vector2 hold = IsAbyss
            ? Player.Pos
            : new Vector2(Playfield.X + Playfield.Width * 0.5f, Playfield.Y + 160);
        Vector2 drift = V.FromAngle(e.Age * 0.6f) * 70f;
        e.Vel = Vector2.Lerp(e.Vel, (hold + drift - e.Pos) * 1.4f + dir * 20f, 0.04f);

        if (e.Phase >= 3 && e.ChargeCd <= 0 && dist > 80)
        {
            e.ChargeT = 0.55f;
            e.ChargeCd = 3.2f;
        }
        if (e.ChargeT > 0)
        {
            e.ChargeT -= dt;
            e.Vel = dir * 340f;
        }

        if (e.FireCd <= 0)
        {
            float gap = e.Phase >= 3 ? 0.09f : e.Phase == 2 ? 0.12f : 0.16f;
            e.FireCd = gap;
            e.Spiral += 0.38f;
            int arms = 3 + e.Phase;
            for (int a = 0; a < arms; a++)
            {
                Vector2 d = V.FromAngle(e.Spiral + a * (MathF.Tau / arms));
                Color shot = e.Kind == EnemyKind.Hydra ? Col.Rgba(190, 70, 255) : Col.Rgba(255, 110, 50);
                SpawnBullet(BulletOwner.Enemy, e.Pos + d * e.Radius * 0.6f, d * 280f, 6f, 11f, 3.2f, 0, 0,
                    shot, WeaponKind.Pulse);
            }
        }

        if (e.Phase >= 2 && (int)(e.Age * 2) != (int)((e.Age - dt) * 2) && (int)e.Age % 2 == 0)
        {
            int ring = 14 + e.Phase * 2;
            for (int i = 0; i < ring; i++)
            {
                Vector2 d = V.FromAngle(i * MathF.Tau / ring + e.Age);
                SpawnBullet(BulletOwner.Enemy, e.Pos + d * 30f, d * 240f, 5.5f, 10f, 3f, 0, 0,
                    Col.Rgba(255, 60, 90), WeaponKind.Spread);
            }
        }
    }

    void UpdateBullets(float dt)
    {
        for (int i = Bullets.Count - 1; i >= 0; i--)
        {
            Bullet b = Bullets[i];
            if (!b.Alive) { Bullets.RemoveAt(i); continue; }
            b.Life -= dt;
            b.Pos += b.Vel * dt;
            b.Alt += b.VelAlt * dt;
            bool outOfPlay = IsAbyss
                ? Vector2.DistanceSquared(b.Pos, Player.Pos) > 2200f * 2200f || b.Alt < -30f || b.Alt > 140f
                : !Raylib.CheckCollisionCircleRec(b.Pos, b.Radius, Expand(Playfield, 80));
            if (b.Life <= 0 || outOfPlay)
            {
                b.Alive = false;
                continue;
            }
            if (b.Owner == BulletOwner.Player)
            {
                Color trail = b.Tint;
                SpawnParticle(b.Pos, -V.Norm(b.Vel) * 20f, 0.16f, b.Radius * 1.6f, trail, true, b.Alt);
            }
        }
    }

    void UpdatePickups(float dt)
    {
        for (int i = Pickups.Count - 1; i >= 0; i--)
        {
            Pickup p = Pickups[i];
            if (!p.Alive) { Pickups.RemoveAt(i); continue; }
            p.Age += dt;
            p.Life -= dt;
            if (p.Life <= 0) p.Alive = false;
            if (IsAbyss && p.Kind == PickupKind.Star && Player.Alive)
            {
                Vector2 to = Player.Pos - p.Pos;
                if (to.LengthSquared() > 1f) p.Pos += V.Norm(to) * 420f * dt;
            }
            bool near = IsAbyss
                ? Vector3.DistanceSquared(ToWorld(p.Pos, Player.Alt), ToWorld(Player.Pos, Player.Alt)) < 6.2f * 6.2f
                : Vector2.DistanceSquared(p.Pos, Player.Pos) < 42f * 42f;
            if (Player.Alive && near)
                Collect(p);
        }
    }

    void Collect(Pickup p)
    {
        p.Alive = false;
        _audio.Pickup();
        Ring(p.Pos, 10, 140, Col.Rgba(255, 230, 120), 0.3f);
        switch (p.Kind)
        {
            case PickupKind.Health:
                Player.Hp = MathF.Min(Player.MaxHp, Player.Hp + 38f);
                Float(p.Pos, "+HULL", Col.Rgba(255, 90, 90));
                break;
            case PickupKind.Shield:
                Player.Shield = MathF.Min(Player.MaxShield, Player.Shield + 40f);
                Float(p.Pos, "+SHIELD", Col.Rgba(90, 200, 255));
                break;
            case PickupKind.Overdrive:
                Player.Overdrive = MathF.Max(Player.Overdrive, 8.5f);
                Float(p.Pos, "OVERDRIVE", Col.Rgba(255, 210, 70));
                Banner = "OVERDRIVE";
                BannerT = 1.2f;
                break;
            case PickupKind.Weapon:
                UpgradeWeapon();
                break;
            case PickupKind.Star:
                Stars++;
                Score += 25 * Combo;
                LevelScore += 25 * Combo;
                Float(p.Pos, "STAR!", Col.Rgba(255, 230, 80));
                if (Stars > 0 && Stars % 10 == 0)
                {
                    Player.Hp = MathF.Min(Player.MaxHp, Player.Hp + 16f);
                    Banner = "STAR BONUS!";
                    BannerT = 1.3f;
                    Float(Player.Pos, "+HEALTH", Col.Rgba(255, 120, 180));
                }
                break;
        }
    }

    void UpgradeWeapon()
    {
        int cur = (int)Player.Weapon;
        if (Player.Levels[cur] < 3)
        {
            Player.Levels[cur]++;
            Float(Player.Pos + new Vector2(0, -30), $"{WeaponName(Player.Weapon)} L{Player.Levels[cur]}", Col.Rgba(120, 230, 255));
            return;
        }
        for (int i = 0; i < 4; i++)
        {
            if (Player.Levels[i] == 0)
            {
                Player.Levels[i] = 1;
                Player.Weapon = (WeaponKind)i;
                Float(Player.Pos + new Vector2(0, -30), $"UNLOCK {WeaponName(Player.Weapon)}", Col.Rgba(255, 220, 90));
                return;
            }
        }
        Player.Overdrive = MathF.Max(Player.Overdrive, 6f);
        Score += 400;
        Float(Player.Pos, "+400", Col.Rgba(255, 220, 80));
    }

    public static string WeaponName(WeaponKind w) => w switch
    {
        WeaponKind.Pulse => "PULSE",
        WeaponKind.Spread => "SPREAD",
        WeaponKind.Rail => "RAIL",
        _ => "NOVA",
    };

    public static string KidWeaponName(WeaponKind w) => w switch
    {
        WeaponKind.Pulse => "BLASTER",
        WeaponKind.Spread => "SPRINKLES",
        WeaponKind.Rail => "ZAPPER",
        _ => "BOOM",
    };

    void Collide()
    {
        foreach (Bullet b in Bullets)
        {
            if (!b.Alive || b.Owner != BulletOwner.Player) continue;
            foreach (Enemy e in Enemies)
            {
                if (!e.Alive || e.SpawnIn > 0) continue;
                if (IsAbyss)
                {
                    Vector3 bw = ToWorld(b.Pos, b.Alt);
                    Vector3 ew = ToWorld(e.Pos, e.Alt);
                    float wr = (b.Radius + CombatRadius(e)) * WorldScale;
                    if (Vector3.DistanceSquared(bw, ew) > wr * wr) continue;
                }
                else if (Vector2.DistanceSquared(b.Pos, e.Pos) > (b.Radius + CombatRadius(e)) * (b.Radius + CombatRadius(e)))
                    continue;
                HurtEnemy(e, b.Damage, b.Pos);
                if (b.Splash > 0)
                {
                    Ring(b.Pos, 12, b.Splash * 2.2f, Col.Rgba(255, 160, 60), 0.25f);
                    foreach (Enemy o in Enemies)
                    {
                        if (!o.Alive || o == e) continue;
                        if (Vector2.DistanceSquared(o.Pos, b.Pos) < b.Splash * b.Splash)
                            HurtEnemy(o, b.Damage * 0.55f, b.Pos);
                    }
                }
                if (b.PierceLeft > 0) b.PierceLeft--;
                else { b.Alive = false; break; }
            }
        }

        if (!Player.Alive) return;

        foreach (Bullet b in Bullets)
        {
            if (!b.Alive || b.Owner != BulletOwner.Enemy) continue;
            if (IsAbyss)
            {
                Vector3 bw = ToWorld(b.Pos, b.Alt);
                Vector3 pw = ToWorld(Player.Pos, Player.Alt);
                float wr = (b.Radius + PlayerCombatRadius()) * WorldScale;
                if (Vector3.DistanceSquared(bw, pw) > wr * wr) continue;
            }
            else if (Vector2.DistanceSquared(b.Pos, Player.Pos) > (b.Radius + PlayerCombatRadius()) * (b.Radius + PlayerCombatRadius()))
                continue;
            b.Alive = false;
            HurtPlayer(b.Damage, b.Pos);
        }

        foreach (Enemy e in Enemies)
        {
            if (!e.Alive || e.SpawnIn > 0) continue;
            if (IsAbyss)
            {
                if (Vector3.DistanceSquared(ToWorld(e.Pos, e.Alt), ToWorld(Player.Pos, Player.Alt))
                    > MathF.Pow((CombatRadius(e) + PlayerCombatRadius()) * WorldScale, 2f))
                    continue;
            }
            else if (Vector2.DistanceSquared(e.Pos, Player.Pos) > (CombatRadius(e) + PlayerCombatRadius()) * (CombatRadius(e) + PlayerCombatRadius()))
                continue;
            HurtPlayer(e.Contact, e.Pos);
            e.Vel = V.Norm(e.Pos - Player.Pos) * 220f;
        }
    }

    void HurtEnemy(Enemy e, float dmg, Vector2 at)
    {
        e.Hp -= dmg;
        e.Flash = 0.08f;
        _audio.Hit();
        Burst(at, e.Pos - at, 7, Col.Rgba(255, 230, 160), 200, 6, IsAbyss ? e.Alt : 0f);
        if (e.Hp <= 0) KillEnemy(e);
    }

    void KillEnemy(Enemy e)
    {
        e.Alive = false;
        bool boss = e.Kind is EnemyKind.Boss or EnemyKind.Hydra;
        ComboKills++;
        Combo = Math.Min(8, 1 + ComboKills / 3);
        ComboT = 2.3f;
        int gained = (int)(e.Score * Combo);
        Score += gained;
        Kills++;
        LevelKills++;
        LevelScore += gained;
        Float(e.Pos, $"+{gained}", boss ? Col.Rgba(255, 200, 80) : Col.Rgba(230, 230, 240), IsAbyss ? e.Alt : 0f);
        if (IsAbyss)
        {
            if (Combo == 2) Float(e.Pos + new Vector2(0, -28), "NICE!", Col.Rgba(120, 255, 180), e.Alt);
            else if (Combo == 4) Float(e.Pos + new Vector2(0, -28), "SUPER!", Col.Rgba(255, 210, 80), e.Alt);
            else if (Combo == 6) Float(e.Pos + new Vector2(0, -28), "MEGA!", Col.Rgba(255, 120, 220), e.Alt);
            else if (Combo >= 8) Float(e.Pos + new Vector2(0, -28), "STAR POWER!", Col.Rgba(255, 240, 90), e.Alt);
            int n = boss ? 8 : 2 + Combo / 3;
            for (int i = 0; i < n && Pickups.Count < 90; i++)
                SpawnPickup(PickupKind.Star, e.Pos + V.FromAngle(Rng.Float(0, MathF.Tau)) * Rng.Float(12, 70));
            Vector3 at = ToWorld(e.Pos, e.Alt + 1.2f);
            Blast(at, boss ? 9.5f : MathF.Max(3.8f, CombatRadius(e) * WorldScale * 2.2f), DeathColor(e.Kind), boss ? 0.85f : 0.5f);
            Burst(e.Pos, Vector2.Zero, boss ? 42 : 28, DeathColor(e.Kind), boss ? 520 : 380, boss ? 22 : 16, e.Alt);
        }
        _audio.Explode(boss);
        Shake = MathF.Max(Shake, boss ? 14f : 4.5f);
        Burst(e.Pos, Vector2.Zero, boss ? 56 : 22, DeathColor(e.Kind), boss ? 360 : 240, boss ? 16 : 9, IsAbyss ? e.Alt : 0f);
        Ring(e.Pos, e.Radius, boss ? 520 : 180, DeathColor(e.Kind), boss ? 0.7f : 0.35f, IsAbyss ? e.Alt : 0f);
        SmokePuff(e.Pos, IsAbyss ? e.Alt : 0f, boss);

        float drop = e.Kind switch
        {
            EnemyKind.Scout => 0.07f,
            EnemyKind.Strafer => 0.11f,
            EnemyKind.Bruiser => 0.2f,
            EnemyKind.Wasp => 0.04f,
            EnemyKind.Spitter => 0.14f,
            EnemyKind.Prism => 0.1f,
            EnemyKind.Hunter => 0.16f,
            EnemyKind.Wraith => 0.08f,
            EnemyKind.Spire => 0.18f,
            _ => 1f,
        };
        if (boss)
        {
            SpawnPickup(PickupKind.Health, e.Pos + new Vector2(-40, 0));
            SpawnPickup(PickupKind.Weapon, e.Pos + new Vector2(40, 0));
            SpawnPickup(PickupKind.Shield, e.Pos + new Vector2(0, 36));
            if (Rng.Chance(0.7f)) SpawnPickup(PickupKind.Overdrive, e.Pos);
        }
        else if (Rng.Chance(IsAbyss ? MathF.Min(0.55f, drop * 1.8f) : drop))
        {
            PickupKind k = Rng.Pick(PickupKind.Health, PickupKind.Weapon, PickupKind.Shield, PickupKind.Overdrive, PickupKind.Health);
            SpawnPickup(k, e.Pos);
        }
    }

    static Color DeathColor(EnemyKind k) => k switch
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

    void HurtPlayer(float dmg, Vector2 at)
    {
        if (Player.IFrames > 0 || !Player.Alive) return;
        if (Player.Shield > 0)
        {
            Player.Shield -= dmg;
            _audio.Shield();
            Ring(Player.Pos, 16, 90, Col.Rgba(80, 180, 255), 0.2f);
            if (Player.Shield < 0)
            {
                dmg = -Player.Shield;
                Player.Shield = 0;
            }
            else
            {
                Player.IFrames = 0.35f;
                return;
            }
        }
        Player.Hp -= dmg;
        Player.IFrames = 0.75f;
        Player.HurtFlash = 0.18f;
        Shake = MathF.Max(Shake, 7f);
        _audio.Hurt();
        Burst(at, Player.Pos - at, 16, Col.Rgba(255, 70, 70), 260, 8);
        if (Player.Hp <= 0)
        {
            Player.Hp = 0;
            Player.Alive = false;
            _audio.Explode(true);
            Burst(Player.Pos, Vector2.Zero, 48, Col.Rgba(90, 230, 255), 400, 14);
            Ring(Player.Pos, 20, 420, Col.Rgba(90, 230, 255), 0.7f);
            Shake = 16;
        }
    }

    void SpawnBullet(BulletOwner owner, Vector2 pos, Vector2 vel, float radius, float dmg, float life, int pierce, float splash, Color tint, WeaponKind style)
    {
        if (Bullets.Count > 420) return;
        float alt = 1.1f;
        float velAlt = 0f;
        if (IsAbyss)
        {
            radius *= owner == BulletOwner.Player ? 3.2f : 2.2f;
            if (owner == BulletOwner.Player)
            {
                life *= 1.5f;
                radius *= 1.6f;
                alt = Player.Alt;
                velAlt = 0f;
            }
            else
            {
                alt = Player.Alt;
                float reach = MathF.Max(40f, vel.Length());
                velAlt = (Player.Alt - alt) * WorldScale * 0.15f;
                _ = reach;
            }
        }
        Bullets.Add(new Bullet
        {
            Alive = true, Owner = owner, Pos = pos, Vel = vel, Alt = alt, VelAlt = velAlt, Radius = radius,
            Damage = dmg, Life = life, PierceLeft = pierce, Splash = splash, Tint = tint, Style = style
        });
    }

    void SpawnPickup(PickupKind kind, Vector2 pos)
    {
        if (Pickups.Count > 110) return;
        if (!IsAbyss) pos = V.ClampTo(pos, Playfield, 20);
        Pickups.Add(new Pickup { Alive = true, Kind = kind, Pos = pos, Life = kind == PickupKind.Star ? 8f : 14f });
    }

    void SpawnParticle(Vector2 pos, Vector2 vel, float life, float size, Color color, bool add, float alt = 0f, float velAlt = 0f)
    {
        if (Particles.Count > 700) return;
        Particles.Add(new Particle
        {
            Alive = true, Pos = pos, Vel = vel, Alt = alt, VelAlt = velAlt, Life = life, MaxLife = life,
            Size = size, Drag = add ? 2.4f : 0.85f, Color = color, Additive = add
        });
    }

    void SmokePuff(Vector2 pos, float alt, bool big)
    {
        int n = big ? 34 : 20;
        for (int i = 0; i < n; i++)
        {
            float g = Rng.Float(88, 175);
            Color c = Col.Rgba((int)g, (int)(g * 0.96f), (int)(g * 0.9f), 230);
            Vector2 jitter = V.FromAngle(Rng.Float(0, MathF.Tau)) * Rng.Float(6, big ? 70 : 42);
            SpawnParticle(pos + jitter, jitter * Rng.Float(0.4f, 1.1f) + new Vector2(0, Rng.Float(-40, -8)),
                Rng.Float(0.7f, 1.55f), Rng.Float(22, big ? 62 : 40), c, false, alt + Rng.Float(-0.4f, 1.8f), Rng.Float(8f, 26f));
        }
    }

    void Burst(Vector2 pos, Vector2 toward, int n, Color c, float speed, float size, float alt = 0f)
    {
        Vector2 baseDir = toward.LengthSquared() > 1 ? V.Norm(toward) : Vector2.Zero;
        for (int i = 0; i < n; i++)
        {
            Vector2 d = baseDir.LengthSquared() > 0
                ? V.Norm(baseDir + V.FromAngle(Rng.Float(0, MathF.Tau)) * 0.8f)
                : V.FromAngle(Rng.Float(0, MathF.Tau));
            SpawnParticle(pos, d * Rng.Float(speed * 0.3f, speed), Rng.Float(0.25f, 0.7f), Rng.Float(size * 0.4f, size), c, true,
                alt, Rng.Float(-12f, 28f));
        }
    }

    void Ring(Vector2 pos, float r, float grow, Color c, float life, float alt = 0f)
    {
        Rings.Add(new RingFx { Alive = true, Pos = pos, Alt = alt, Radius = r, Grow = grow, Color = c, Life = life, MaxLife = life });
    }

    void Float(Vector2 pos, string text, Color c, float alt = 0f)
    {
        Floaters.Add(new Floater { Alive = true, Pos = pos + new Vector2(Rng.Float(-8, 8), -8), Alt = alt, Text = text, Life = 1.15f, MaxLife = 1.15f, Color = c });
    }

    public void Blast(Vector3 pos, float size, Color c, float life)
    {
        if (Booms.Count > 40) Booms.RemoveAt(0);
        Booms.Add(new Boom { Alive = true, Pos = pos, Size = size, Color = c, Life = life, MaxLife = life });
        Ring(FromWorld(pos), 12, 420, c, life, pos.Y);
    }

    void UpdateFx(float dt)
    {
        for (int i = Particles.Count - 1; i >= 0; i--)
        {
            Particle p = Particles[i];
            p.Life -= dt;
            if (p.Life <= 0) { Particles.RemoveAt(i); continue; }
            p.Vel *= MathF.Max(0, 1f - p.Drag * dt);
            p.Pos += p.Vel * dt;
            p.Alt += p.VelAlt * dt;
            p.VelAlt -= 18f * dt;
        }
        for (int i = Rings.Count - 1; i >= 0; i--)
        {
            RingFx r = Rings[i];
            r.Life -= dt;
            if (r.Life <= 0) { Rings.RemoveAt(i); continue; }
            r.Radius += r.Grow * dt;
        }
        for (int i = Floaters.Count - 1; i >= 0; i--)
        {
            Floater f = Floaters[i];
            f.Life -= dt;
            if (f.Life <= 0) { Floaters.RemoveAt(i); continue; }
            f.Pos.Y -= 28f * dt;
            f.Alt += 6f * dt;
        }
        for (int i = Booms.Count - 1; i >= 0; i--)
        {
            Boom b = Booms[i];
            b.Life -= dt;
            if (b.Life <= 0) Booms.RemoveAt(i);
        }
    }

    Enemy? NearestEnemy()
    {
        Enemy? best = null;
        float bestD = float.MaxValue;
        foreach (Enemy e in Enemies)
        {
            if (!e.Alive) continue;
            float d = Vector2.DistanceSquared(e.Pos, Player.Pos);
            if (d < bestD) { bestD = d; best = e; }
        }
        return best;
    }

    public Enemy? ActiveBoss()
    {
        foreach (Enemy e in Enemies)
            if (e.Alive && e.Kind is EnemyKind.Boss or EnemyKind.Hydra) return e;
        return null;
    }
}
