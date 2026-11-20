using System.Diagnostics;
using Tsudev.Audit.Core.Progress;

namespace Tsudev.Audit.Core.Logging;

/// <summary>
/// Chen giua bo bao tien trinh va man hinh: chuyen tiep NGUYEN moi loi goi, dong
/// thoi ghi <c>step.ok</c>/<c>step.fail</c> kem thoi luong vao <see cref="RunLog"/>.
/// Day la phan co gia tri nhat khi ho tro tu xa: buoc nao hong, buoc nao cham.
/// </summary>
public sealed class LoggingProgressSink(IProgressSink inner, RunLog log) : IProgressSink
{
    private readonly Stopwatch _watch = new();
    private string? _label;

    public void BeginStep(string label)
    {
        _label = label;
        _watch.Restart();
        inner.BeginStep(label);
    }

    public void EndStep()
    {
        log.Info("step.ok", null, ("step", _label), ("ms", _watch.ElapsedMilliseconds));
        _label = null;
        inner.EndStep();
    }

    public void FailStep(string reason)
    {
        log.Warn("step.fail", reason, ("step", _label), ("ms", _watch.ElapsedMilliseconds));
        _label = null;
        inner.FailStep(reason);
    }

    public void Note(string message)
    {
        log.Info("note", message, ("step", _label));
        inner.Note(message);
    }
}
