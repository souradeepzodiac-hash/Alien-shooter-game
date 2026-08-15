using System.Numerics;
using Raylib_cs;

namespace VoidHunter;

enum EnemyKind { Scout, Strafer, Bruiser, Wasp, Spitter, Boss }
enum WeaponKind { Pulse, Spread, Rail, Nova }
enum PickupKind { Health, Weapon, Shield, Overdrive }
enum BulletOwner { Player, Enemy }

sealed class Bullet
{
    public bool Alive;
    public BulletOwner Owner;
    public Vector2 Pos, Vel;
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
    public float Angle, Radius, Hp, MaxHp, Contact, Score;
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
    public float Life, MaxLife, Size, Drag;
    public Color Color;
    public bool Additive;
}

sealed class RingFx
{
    public bool Alive;
    public Vector2 Pos;
    public float Life, MaxLife, Radius, Grow;
    public Color Color;
}

sealed class Floater
{
    public bool Alive;
    public Vector2 Pos;
    public string Text = "";
    public float Life, MaxLife;
    public Color Color;
}

sealed class Player
{
    public Vector2 Pos, Vel;
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

    public const int FinalLevel = 10;
    public int Score, Wave, Combo = 1, ComboKills;
    public int Kills, LevelKills, LevelScore, ClearBonus;
    public float ComboT, Shake, BannerT, WaveRest, GameOverDelay, Time, LevelTime;
    public string Banner = "";
    public string ResultGrade = "C";
    public bool WantsGameOver, WantsLevelClear, WantsVictory, NewHigh;
    public bool AutoPlay;
    public Rectangle Playfield;
    public Vector2 ShakeOff;

    readonly AudioBus _audio;
    readonly List<(EnemyKind Kind, float Delay)> _queue = [];
    float _spawnWait;
    int _bossIndex;

    public World(AudioBus audio) => _audio = audio;

    public void StartNew()
    {
        Enemies.Clear(); Bullets.Clear(); Pickups.Clear();
        Particles.Clear(); Rings.Clear(); Floaters.Clear();
        _queue.Clear();
        Score = 0; Wave = 0; Combo = 1; ComboKills = 0;
        Kills = 0; LevelKills = 0; LevelScore = 0; ClearBonus = 0;
        ComboT = 0; Shake = 0; BannerT = 0; WaveRest = 0.6f;
        GameOverDelay = 0; Time = 0; LevelTime = 0;
        WantsGameOver = false; WantsLevelClear = false; WantsVictory = false; NewHigh = false;
        ResultGrade = "C";
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
        else
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

        if (wish.LengthSquared() > 0) wish = V.Norm(wish);
        float speed = Player.DashT > 0 ? 820f : 355f;
        Player.Vel = wish * speed;
        Player.Pos += Player.Vel * dt;
        Player.Pos = V.ClampTo(Player.Pos, Playfield, Player.Radius);

        if (aim.LengthSquared() > 16f) Player.Angle = V.Ang(aim);

        if (dash && Player.DashCd <= 0)
        {
            Vector2 dir = wish.LengthSquared() > 0 ? wish : V.FromAngle(Player.Angle);
            Player.Vel = dir * 820f;
            Player.DashT = 0.16f;
            Player.DashCd = 2.15f;
            Player.IFrames = MathF.Max(Player.IFrames, 0.16f);
            _audio.Dash();
            Burst(Player.Pos, V.FromAngle(Player.Angle + MathF.PI), 14, Col.Rgba(120, 230, 255), 220, 9);
            Ring(Player.Pos, 18, 280, Col.Rgba(80, 210, 255), 0.28f);
        }

        if (fire && Player.FireCd <= 0)
            FireWeapon();

        if (wish.LengthSquared() > 0.1f || Player.DashT > 0)
        {
            Vector2 back = Player.Pos - V.FromAngle(Player.Angle) * 18f;
            SpawnParticle(back, -V.FromAngle(Player.Angle) * Rng.Float(40, 120) + V.Perp(V.FromAngle(Player.Angle)) * Rng.Float(-30, 30),
                Rng.Float(0.12f, 0.28f), Rng.Float(5, 11), Col.Rgba(80, 230, 255, 200), true);
        }
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
        Vector2 muzzle = Player.Pos + dir * 28f;

        switch (Player.Weapon)
        {
            case WeaponKind.Pulse:
                Player.FireCd = (lv >= 3 ? 0.075f : lv == 2 ? 0.09f : 0.11f) * od;
                int extra = lv >= 2 ? 1 : 0;
                for (int i = 0; i <= extra; i++)
                {
                    Vector2 off = extra > 0 ? V.Perp(dir) * (i == 0 ? -8f : 8f) : Vector2.Zero;
                    SpawnBullet(BulletOwner.Player, muzzle + off, dir * 940f, 5.5f, 12 + lv * 2, 1.1f, 0, 0,
                        Col.Rgba(90, 240, 255), WeaponKind.Pulse);
                }
                break;
            case WeaponKind.Spread:
                Player.FireCd = (lv >= 3 ? 0.14f : 0.17f) * od;
                int shots = lv >= 3 ? 7 : lv == 2 ? 5 : 3;
                float spread = 0.42f + lv * 0.04f;
                for (int i = 0; i < shots; i++)
                {
                    float t = shots == 1 ? 0 : (i / (float)(shots - 1) - 0.5f);
                    Vector2 d = V.FromAngle(Player.Angle + t * spread);
                    SpawnBullet(BulletOwner.Player, muzzle, d * 820f, 4.5f, 7 + lv, 0.9f, 0, 0,
                        Col.Rgba(190, 120, 255), WeaponKind.Spread);
                }
                break;
            case WeaponKind.Rail:
                Player.FireCd = (lv >= 3 ? 0.38f : 0.48f) * od;
                SpawnBullet(BulletOwner.Player, muzzle, dir * 1500f, 7f, 42 + lv * 10, 0.55f, lv >= 2 ? 6 : 3, 0,
                    Col.Rgba(255, 230, 120), WeaponKind.Rail);
                Shake = MathF.Max(Shake, 3.5f);
                break;
            default:
                Player.FireCd = (lv >= 3 ? 0.42f : 0.55f) * od;
                SpawnBullet(BulletOwner.Player, muzzle, dir * 520f, 9f, 26 + lv * 6, 1.4f, 0, 78f + lv * 10,
                    Col.Rgba(255, 140, 60), WeaponKind.Nova);
                break;
        }

        _audio.Shoot(Player.Weapon);
        Burst(muzzle, dir, 6, Col.Rgba(200, 240, 255), 180, 5);
    }

    void TryResolveLevel()
    {
        if (!Player.Alive || WantsLevelClear || WantsVictory || WantsGameOver)
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
        if (Wave >= FinalLevel) FinishRun(won: true);
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

        void Add(EnemyKind k, int n, float gap)
        {
            for (int i = 0; i < n; i++) _queue.Add((k, gap));
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
        Banner = wave % 5 == 0 ? "RIFT SIGNATURE" : $"LEVEL {wave}";
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
            WantsVictory = true;
            WantsLevelClear = false;
            Banner = "VICTORY";
            BannerT = 2f;
            _audio.Boss();
        }
    }

    public void ContinueNextLevel()
    {
        WantsLevelClear = false;
        WantsVictory = false;
        Player.Hp = MathF.Min(Player.MaxHp, Player.Hp + 18f);
        Player.IFrames = 0.8f;
        WaveRest = 0.35f;
        Banner = (Wave + 1) % 5 == 0 ? "RIFT SIGNATURE" : $"LEVEL {Wave + 1}";
        BannerT = 1.8f;
        _audio.Wave();
    }

    public void RetryCurrentLevel()
    {
        WantsGameOver = false;
        WantsLevelClear = false;
        WantsVictory = false;
        GameOverDelay = 0;
        Enemies.Clear();
        Bullets.Clear();
        Pickups.Clear();
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
        Banner = retry % 5 == 0 ? "RIFT SIGNATURE" : $"LEVEL {retry}";
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
        else
        {
            Wave = 3; Score = 3120; Kills = 28; LevelKills = 9;
            LevelScore = 880; LevelTime = 41; Player.Hp = 84; Player.Alive = true;
            ClearBonus = 350; ResultGrade = "A"; WantsLevelClear = true;
        }
    }

    Vector2 EdgePoint()
    {
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
            default:
                e.Radius = 62; e.MaxHp = 850 + _bossIndex * 320; e.Contact = 28; e.Score = 2500 + _bossIndex * 800;
                e.SpawnIn = 1.1f; e.FireCd = 0.8f; e.Phase = 1;
                pos = new Vector2(Playfield.X + Playfield.Width * 0.5f, Playfield.Y + 110);
                e.Pos = pos;
                Ring(pos, 20, 420, Col.Rgba(255, 80, 40), 0.7f);
                Shake = 8;
                break;
        }
        e.Hp = e.MaxHp;
        e.Angle = V.Ang(Player.Pos - e.Pos);
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
                default:
                    UpdateBoss(e, dt, dir, dist);
                    break;
            }

            e.Pos += e.Vel * dt;
            // soft keep inside with slack so they can enter from edges
            if (e.Kind != EnemyKind.Boss)
                e.Pos = V.ClampTo(e.Pos, Expand(Playfield, 8), e.Radius);
            else
                e.Pos = V.ClampTo(e.Pos, Playfield, e.Radius);
        }
    }

    static Rectangle Expand(Rectangle r, float m) => new(r.X - m, r.Y - m, r.Width + m * 2, r.Height + m * 2);

    void UpdateBoss(Enemy e, float dt, Vector2 dir, float dist)
    {
        if (e.Hp < e.MaxHp * 0.3f) e.Phase = 3;
        else if (e.Hp < e.MaxHp * 0.6f) e.Phase = 2;

        e.ChargeCd -= dt;
        e.FireCd -= dt;
        Vector2 hold = new(Playfield.X + Playfield.Width * 0.5f, Playfield.Y + 160);
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
                SpawnBullet(BulletOwner.Enemy, e.Pos + d * e.Radius * 0.6f, d * 280f, 6f, 11f, 3.2f, 0, 0,
                    Col.Rgba(255, 110, 50), WeaponKind.Pulse);
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
            if (b.Life <= 0 || !Raylib.CheckCollisionCircleRec(b.Pos, b.Radius, Expand(Playfield, 80)))
            {
                b.Alive = false;
                continue;
            }
            if (b.Owner == BulletOwner.Player)
            {
                Color trail = b.Tint;
                SpawnParticle(b.Pos, -V.Norm(b.Vel) * 20f, 0.12f, b.Radius * 1.2f, trail, true);
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
            if (Player.Alive && Vector2.DistanceSquared(p.Pos, Player.Pos) < 42f * 42f)
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

    void Collide()
    {
        foreach (Bullet b in Bullets)
        {
            if (!b.Alive || b.Owner != BulletOwner.Player) continue;
            foreach (Enemy e in Enemies)
            {
                if (!e.Alive || e.SpawnIn > 0) continue;
                if (Vector2.DistanceSquared(b.Pos, e.Pos) > (b.Radius + e.Radius) * (b.Radius + e.Radius)) continue;
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
            if (Vector2.DistanceSquared(b.Pos, Player.Pos) > (b.Radius + Player.Radius) * (b.Radius + Player.Radius)) continue;
            b.Alive = false;
            HurtPlayer(b.Damage, b.Pos);
        }

        foreach (Enemy e in Enemies)
        {
            if (!e.Alive || e.SpawnIn > 0) continue;
            if (Vector2.DistanceSquared(e.Pos, Player.Pos) > (e.Radius + Player.Radius) * (e.Radius + Player.Radius)) continue;
            HurtPlayer(e.Contact, e.Pos);
            e.Vel = V.Norm(e.Pos - Player.Pos) * 220f;
        }
    }

    void HurtEnemy(Enemy e, float dmg, Vector2 at)
    {
        e.Hp -= dmg;
        e.Flash = 0.08f;
        _audio.Hit();
        Burst(at, e.Pos - at, 7, Col.Rgba(255, 230, 160), 200, 6);
        if (e.Hp <= 0) KillEnemy(e);
    }

    void KillEnemy(Enemy e)
    {
        e.Alive = false;
        bool boss = e.Kind == EnemyKind.Boss;
        ComboKills++;
        Combo = Math.Min(8, 1 + ComboKills / 3);
        ComboT = 2.3f;
        int gained = (int)(e.Score * Combo);
        Score += gained;
        Kills++;
        LevelKills++;
        LevelScore += gained;
        Float(e.Pos, $"+{gained}", boss ? Col.Rgba(255, 200, 80) : Col.Rgba(230, 230, 240));
        _audio.Explode(boss);
        Shake = MathF.Max(Shake, boss ? 14f : 4.5f);
        Burst(e.Pos, Vector2.Zero, boss ? 56 : 22, DeathColor(e.Kind), boss ? 360 : 240, boss ? 16 : 9);
        Ring(e.Pos, e.Radius, boss ? 520 : 180, DeathColor(e.Kind), boss ? 0.7f : 0.35f);

        float drop = e.Kind switch
        {
            EnemyKind.Scout => 0.07f,
            EnemyKind.Strafer => 0.11f,
            EnemyKind.Bruiser => 0.2f,
            EnemyKind.Wasp => 0.04f,
            EnemyKind.Spitter => 0.14f,
            _ => 1f,
        };
        if (boss)
        {
            SpawnPickup(PickupKind.Health, e.Pos + new Vector2(-40, 0));
            SpawnPickup(PickupKind.Weapon, e.Pos + new Vector2(40, 0));
            SpawnPickup(PickupKind.Shield, e.Pos + new Vector2(0, 36));
            if (Rng.Chance(0.7f)) SpawnPickup(PickupKind.Overdrive, e.Pos);
        }
        else if (Rng.Chance(drop))
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
        Bullets.Add(new Bullet
        {
            Alive = true, Owner = owner, Pos = pos, Vel = vel, Radius = radius,
            Damage = dmg, Life = life, PierceLeft = pierce, Splash = splash, Tint = tint, Style = style
        });
    }

    void SpawnPickup(PickupKind kind, Vector2 pos)
    {
        pos = V.ClampTo(pos, Playfield, 20);
        Pickups.Add(new Pickup { Alive = true, Kind = kind, Pos = pos });
    }

    void SpawnParticle(Vector2 pos, Vector2 vel, float life, float size, Color color, bool add)
    {
        if (Particles.Count > 520) return;
        Particles.Add(new Particle
        {
            Alive = true, Pos = pos, Vel = vel, Life = life, MaxLife = life,
            Size = size, Drag = 2.4f, Color = color, Additive = add
        });
    }

    void Burst(Vector2 pos, Vector2 toward, int n, Color c, float speed, float size)
    {
        Vector2 baseDir = toward.LengthSquared() > 1 ? V.Norm(toward) : Vector2.Zero;
        for (int i = 0; i < n; i++)
        {
            Vector2 d = baseDir.LengthSquared() > 0
                ? V.Norm(baseDir + V.FromAngle(Rng.Float(0, MathF.Tau)) * 0.8f)
                : V.FromAngle(Rng.Float(0, MathF.Tau));
            SpawnParticle(pos, d * Rng.Float(speed * 0.3f, speed), Rng.Float(0.2f, 0.55f), Rng.Float(size * 0.4f, size), c, true);
        }
    }

    void Ring(Vector2 pos, float r, float grow, Color c, float life)
    {
        Rings.Add(new RingFx { Alive = true, Pos = pos, Radius = r, Grow = grow, Color = c, Life = life, MaxLife = life });
    }

    void Float(Vector2 pos, string text, Color c)
    {
        Floaters.Add(new Floater { Alive = true, Pos = pos + new Vector2(Rng.Float(-8, 8), -8), Text = text, Life = 0.9f, MaxLife = 0.9f, Color = c });
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
            if (e.Alive && e.Kind == EnemyKind.Boss) return e;
        return null;
    }
}
