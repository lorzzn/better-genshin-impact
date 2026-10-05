using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Script.Utils;
using BetterGenshinImpact.GameTask.Music.Model;
using BetterGenshinImpact.GameTask.Music.Service;
using BetterGenshinImpact.Helpers;
using Microsoft.ClearScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BetterGenshinImpact.Core.Script.Dependence;

/// <summary>Parameter binding for the existing score parser, timeline and playback service.</summary>
public static class MusicScript
{
    public static async Task Run(ScriptObject config, CancellationToken cancellationToken)
    {
        var files = ScriptObjectConverter.GetValue<string>(config, "files")?.ToArray();
        if (files == null || files.Length == 0) throw new ArgumentException("请配置曲谱 files 列表");
#if BETTERGI_PORTABLE
        var root = Path.Combine(Runtime.RuntimeEnvironment.LibraryRoot ?? Runtime.RuntimeEnvironment.AssetRoot, "User", "Music");
#else
        var root = Global.Absolute(@"User\Music");
#endif
        var parser = new MusicScoreParser();
        var profiles = new InstrumentProfileService();
        var outputProfile = ScriptObjectConverter.GetValue(config, "outputProfileName", "");
        var transpose = ScriptObjectConverter.GetValue(config, "transpose", 0);
        var disabledTracks = ScriptObjectConverter.GetValue<int>(config, "disabledTrackIndexes")?.ToHashSet() ?? [];
        List<PerformanceScore> queue = [];
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = ScriptUtils.NormalizePath(root, file);
            if (!parser.CanParse(path)) throw new ArgumentException($"不支持的曲谱格式：{file}");
            var score = await parser.ParseAsync(path, root, cancellationToken);
            if (!score.IsValid) throw new InvalidDataException($"曲谱 {file}：{score.Error}");
            score.OutputProfileName = profiles.Find(string.IsNullOrWhiteSpace(outputProfile) ? score.Instrument : outputProfile).Name;
            score.Transpose = transpose;
            foreach (var track in score.Tracks) track.IsEnabled = !disabledTracks.Contains(track.Index);
            queue.Add(score);
        }
        var speed = Convert.ToDouble(ScriptObjectConverter.GetValue<object>(config, "speed", 1.0));
        if (!double.IsFinite(speed)) throw new ArgumentException("播放速度必须为有限数值");
        var bpmValue = ScriptObjectConverter.GetValue<object?>(config, "customBpm", null);
        double? bpm = bpmValue == null ? null : Convert.ToDouble(bpmValue);
        if (bpm is { } value && (!double.IsFinite(value) || value < 1 || value > 1000))
            throw new ArgumentException("customBpm 必须在 1 到 1000 之间");
        var startSeconds = Convert.ToDouble(ScriptObjectConverter.GetValue<object>(config, "startSeconds", 0.0));
        if (!double.IsFinite(startSeconds) || startSeconds < 0 || startSeconds > TimeSpan.MaxValue.TotalSeconds)
            throw new ArgumentException("startSeconds 无效");
        var options = new MusicPlaybackOptions
        {
            InputMode = ScriptObjectConverter.GetValue(config, "inputMode", MusicInputMode.BackgroundPostMessage),
            PlaybackMode = ScriptObjectConverter.GetValue(config, "playbackMode", MusicPlaybackMode.Sequential),
            Speed = speed,
            CustomBpm = bpm,
            StartPosition = TimeSpan.FromSeconds(startSeconds),
            AutoSwitchInstrument = ScriptObjectConverter.GetValue(config, "autoSwitchInstrument", false),
        };
        var playback = new MusicPlaybackService(new MusicTimelineBuilder(profiles), profiles, new MusicInstrumentSwitcher(),
            [new PostMessageKeyInputTransport(), new SendInputKeyInputTransport()]);
        await playback.RunPlaylistAsync(queue, 0, options, cancellationToken);
    }
}
