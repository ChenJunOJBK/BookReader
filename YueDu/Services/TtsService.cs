using System;
using System.Collections.Generic;
using System.Speech.Synthesis;

namespace YueDu.Services;

/// <summary>
/// 听书（语音合成）服务
/// </summary>
public class TtsService : IDisposable
{
    private SpeechSynthesizer? _synth;
    private string _currentText = string.Empty;
    private int _startOffset;
    private bool _isSpeaking;
    private bool _isPaused;
    private readonly object _lock = new();

    public event Action? SpeakStarted;
    public event Action? SpeakCompleted;
    /// <summary>朗读进度变化（当前字符偏移）</summary>
    public event Action<int>? PositionChanged;

    public bool IsSpeaking
    {
        get { lock (_lock) return _isSpeaking; }
    }

    public bool IsPaused
    {
        get { lock (_lock) return _isPaused; }
    }

    /// <summary>获取系统已安装的语音列表</summary>
    public List<InstalledVoice> GetInstalledVoices()
    {
        EnsureSynth();
        var voices = new List<InstalledVoice>();
        foreach (var v in _synth!.GetInstalledVoices())
        {
            if (v.Enabled) voices.Add(v);
        }
        return voices;
    }

    /// <summary>语速 -10 ~ 10</summary>
    public int Rate
    {
        get { EnsureSynth(); return _synth!.Rate; }
        set { EnsureSynth(); _synth!.Rate = Math.Clamp(value, -10, 10); }
    }

    /// <summary>音量 0 ~ 100</summary>
    public int Volume
    {
        get { EnsureSynth(); return _synth!.Volume; }
        set { EnsureSynth(); _synth!.Volume = Math.Clamp(value, 0, 100); }
    }

    /// <summary>选择语音</summary>
    public void SelectVoice(string voiceName)
    {
        EnsureSynth();
        try
        {
            if (!string.IsNullOrEmpty(voiceName))
                _synth!.SelectVoice(voiceName);
        }
        catch { /* 忽略无效语音 */ }
    }

    private void EnsureSynth()
    {
        if (_synth == null)
        {
            _synth = new SpeechSynthesizer();
            _synth.SpeakProgress += (s, e) =>
            {
                PositionChanged?.Invoke(e.CharacterPosition + _startOffset);
            };
            _synth.SpeakCompleted += (s, e) =>
            {
                lock (_lock) { _isSpeaking = false; _isPaused = false; }
                SpeakCompleted?.Invoke();
            };
        }
    }

    /// <summary>开始朗读（可从指定偏移开始）</summary>
    public void Speak(string text, int startOffset = 0)
    {
        Stop();
        _currentText = text ?? string.Empty;
        _startOffset = Math.Clamp(startOffset, 0, Math.Max(0, _currentText.Length - 1));

        EnsureSynth();
        lock (_lock) { _isSpeaking = true; _isPaused = false; }
        SpeakStarted?.Invoke();

        // 从偏移位置开始朗读子串
        string textToSpeak = _startOffset > 0 && _startOffset < _currentText.Length
            ? _currentText.Substring(_startOffset)
            : _currentText;
        _synth!.SpeakAsync(textToSpeak);
    }

    /// <summary>暂停</summary>
    public void Pause()
    {
        EnsureSynth();
        if (_synth!.State == SynthesizerState.Speaking)
        {
            _synth.Pause();
            lock (_lock) { _isPaused = true; }
        }
    }

    /// <summary>继续</summary>
    public void Resume()
    {
        EnsureSynth();
        if (_synth!.State == SynthesizerState.Paused)
        {
            _synth.Resume();
            lock (_lock) { _isPaused = false; }
        }
    }

    /// <summary>停止</summary>
    public void Stop()
    {
        EnsureSynth();
        try
        {
            _synth!.SpeakAsyncCancelAll();
        }
        catch { }
        lock (_lock) { _isSpeaking = false; _isPaused = false; }
    }

    public void Dispose()
    {
        Stop();
        _synth?.Dispose();
        _synth = null;
    }
}
