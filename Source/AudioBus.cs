using Raylib_cs;

namespace VoidHunter;

sealed class Voice
{
    readonly Sound[] _voices;
    int _i;

    public Voice(Sound src, int copies)
    {
        copies = Math.Max(1, copies);
        _voices = new Sound[copies];
        _voices[0] = src;
        for (int i = 1; i < copies; i++)
            _voices[i] = src.FrameCount > 0 ? Raylib.LoadSoundAlias(src) : src;
    }

    public void Play(float pitch = 1f, float vol = 1f)
    {
        Sound s = _voices[_i++ % _voices.Length];
        if (s.FrameCount == 0) return;
        Raylib.SetSoundPitch(s, Math.Clamp(pitch, 0.6f, 1.6f));
        Raylib.SetSoundVolume(s, Math.Clamp(vol, 0f, 1f));
        Raylib.PlaySound(s);
    }
}

sealed class AudioBus : IDisposable
{
    readonly List<Sound> _owned = [];
    readonly List<Sound> _aliases = [];
    Voice _pulse = null!;
    Voice _spread = null!;
    Voice _rail = null!;
    Voice _nova = null!;
    Voice _hit = null!;
    Voice _explode = null!;
    Voice _explodeBig = null!;
    Voice _hurt = null!;
    Voice _pickup = null!;
    Voice _dash = null!;
    Voice _ui = null!;
    Voice _uiOk = null!;
    Voice _wave = null!;
    Voice _boss = null!;
    Voice _shield = null!;
    Music _theme;
    Music _battle;
    Music _current;
    bool _hasTheme, _hasBattle, _muted;
    int _playing = -1;

    public void Load()
    {
        _pulse = LoadVoice("shoot_pulse.wav", 6);
        _spread = LoadVoice("shoot_spread.wav", 6);
        _rail = LoadVoice("shoot_rail.wav", 3);
        _nova = LoadVoice("shoot_nova.wav", 3);
        _hit = LoadVoice("hit.wav", 8);
        _explode = LoadVoice("explode.wav", 6);
        _explodeBig = LoadVoice("explode_big.wav", 3);
        _hurt = LoadVoice("hurt.wav", 3);
        _pickup = LoadVoice("pickup.wav", 3);
        _dash = LoadVoice("dash.wav", 2);
        _ui = LoadVoice("ui.wav", 2);
        _uiOk = LoadVoice("ui_ok.wav", 2);
        _wave = LoadVoice("wave.wav", 1);
        _boss = LoadVoice("boss.wav", 1);
        _shield = LoadVoice("shield.wav", 3);

        _theme = TryMusic("music_theme.wav", out _hasTheme);
        _battle = TryMusic("music_battle.wav", out _hasBattle);
        if (_hasTheme)
        {
            _theme.Looping = true;
            Raylib.SetMusicVolume(_theme, 0.42f);
        }
        if (_hasBattle)
        {
            _battle.Looping = true;
            Raylib.SetMusicVolume(_battle, 0.38f);
        }
    }

    Voice LoadVoice(string file, int copies)
    {
        string path = Paths.Audio(file);
        if (!File.Exists(path))
            return new Voice(default, 1);
        Sound s = Raylib.LoadSound(path);
        _owned.Add(s);
        var v = new Voice(s, copies);
        return v;
    }

    static Music TryMusic(string file, out bool ok)
    {
        string path = Paths.Audio(file);
        ok = File.Exists(path);
        return ok ? Raylib.LoadMusicStream(path) : default;
    }

    public void Update()
    {
        if (_current.FrameCount > 0)
            Raylib.UpdateMusicStream(_current);
    }

    public void PlayTheme() => Switch(0);
    public void PlayBattle() => Switch(1);

    void Switch(int which)
    {
        bool has = which == 0 ? _hasTheme : _hasBattle;
        if (!has) return;
        if (_playing == which)
        {
            if (_current.FrameCount > 0 && !Raylib.IsMusicStreamPlaying(_current))
                Raylib.PlayMusicStream(_current);
            return;
        }
        if (_current.FrameCount > 0)
            Raylib.StopMusicStream(_current);
        _current = which == 0 ? _theme : _battle;
        _playing = which;
        Raylib.PlayMusicStream(_current);
    }

    public void PauseMusic(bool pause)
    {
        if (_current.FrameCount == 0) return;
        if (pause) Raylib.PauseMusicStream(_current);
        else Raylib.ResumeMusicStream(_current);
    }

    public void ToggleMute()
    {
        _muted = !_muted;
        float m = _muted ? 0f : 1f;
        Raylib.SetMasterVolume(m);
    }

    public void Shoot(WeaponKind w)
    {
        switch (w)
        {
            case WeaponKind.Pulse: _pulse.Play(Rng.Float(0.94f, 1.08f), 0.42f); break;
            case WeaponKind.Spread: _spread.Play(Rng.Float(0.92f, 1.06f), 0.38f); break;
            case WeaponKind.Rail: _rail.Play(Rng.Float(0.95f, 1.05f), 0.55f); break;
            default: _nova.Play(1f, 0.5f); break;
        }
    }

    public void Hit() => _hit.Play(Rng.Float(0.9f, 1.15f), 0.35f);
    public void Explode(bool big) { if (big) _explodeBig.Play(1f, 0.7f); else _explode.Play(Rng.Float(0.88f, 1.12f), 0.55f); }
    public void Hurt() => _hurt.Play(1f, 0.65f);
    public void Pickup() => _pickup.Play(1f, 0.55f);
    public void Dash() => _dash.Play(1f, 0.5f);
    public void Ui() => _ui.Play(1f, 0.35f);
    public void UiOk() => _uiOk.Play(1f, 0.4f);
    public void Wave() => _wave.Play(1f, 0.5f);
    public void Boss() => _boss.Play(1f, 0.7f);
    public void Shield() => _shield.Play(1f, 0.5f);

    public void Dispose()
    {
        if (_current.FrameCount > 0) Raylib.StopMusicStream(_current);
        if (_hasTheme) Raylib.UnloadMusicStream(_theme);
        if (_hasBattle) Raylib.UnloadMusicStream(_battle);
        foreach (Sound s in _owned)
            if (s.FrameCount > 0) Raylib.UnloadSound(s);
    }
}
