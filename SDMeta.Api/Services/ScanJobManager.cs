using SDMeta.Api.Contracts;
using SDMeta.Processors;
using System.Collections.Concurrent;

namespace SDMeta.Api.Services;

public sealed class ScanJobManager(IServiceScopeFactory scopeFactory, PendingFileChangeQueue pendingQueue, ILogger<ScanJobManager> logger)
{
    private readonly ConcurrentDictionary<Guid, ScanJobState> _jobs = new();

    public ScanStateResponse StartFullScan()
    {
        var state = CreateState("full", addedCount: 0, removedCount: 0);
        _ = Task.Run(() => RunFullScanAsync(state));
        return state.ToResponse();
    }

    public ScanStateResponse StartPartialScan(PartialScanRequest request)
    {
        var pending = request.UsePendingWatcherQueue ? pendingQueue.Dequeue() : (Added: Array.Empty<string>(), Removed: Array.Empty<string>());

        var added = pending.Added
            .Concat(request.Added ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var removed = pending.Removed
            .Concat(request.Removed ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var state = CreateState("partial", added.Length, removed.Length);
        _ = Task.Run(() => RunPartialScanAsync(state, added, removed));
        return state.ToResponse();
    }

    public bool TryGet(Guid scanId, out ScanStateResponse? response)
    {
        if (_jobs.TryGetValue(scanId, out var state))
        {
            response = state.ToResponse();
            return true;
        }

        response = null;
        return false;
    }

    public async Task<ScanStateResponse?> WaitForUpdateAsync(Guid scanId, long afterRevision, CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested == false)
        {
            if (_jobs.TryGetValue(scanId, out var state) == false)
            {
                return null;
            }

            var snapshot = state.ToResponse();
            if (snapshot.Revision > afterRevision || IsTerminal(snapshot.Status))
            {
                return snapshot;
            }

            try
            {
                await Task.Delay(250, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        return null;
    }

    private static bool IsTerminal(ScanStatus status) => status is ScanStatus.Completed or ScanStatus.Failed;

    private ScanJobState CreateState(string type, int addedCount, int removedCount)
    {
        var state = new ScanJobState
        {
            ScanId = Guid.NewGuid(),
            Type = type,
            Status = ScanStatus.Queued,
            AddedCount = addedCount,
            RemovedCount = removedCount,
            Progress = 0,
            StartedUtc = DateTime.UtcNow,
            Revision = 1,
        };

        _jobs[state.ScanId] = state;
        return state;
    }

    private async Task RunFullScanAsync(ScanJobState state)
    {
        using var scope = scopeFactory.CreateScope();
        var rescan = scope.ServiceProvider.GetRequiredService<Rescan>();

        void ProgressHandler(object? _, float progress) => UpdateProgress(state, progress);

        rescan.ProgressNotification += ProgressHandler;

        SetStatus(state, ScanStatus.Running);

        try
        {
            pendingQueue.Reset();
            await rescan.ProcessImageFiles();
            Complete(state);
        }
        catch (Exception ex)
        {
            Fail(state, ex);
        }
        finally
        {
            rescan.ProgressNotification -= ProgressHandler;
        }
    }

    private async Task RunPartialScanAsync(ScanJobState state, IReadOnlyList<string> added, IReadOnlyList<string> removed)
    {
        using var scope = scopeFactory.CreateScope();
        var rescan = scope.ServiceProvider.GetRequiredService<Rescan>();

        void ProgressHandler(object? _, float progress) => UpdateProgress(state, progress);

        rescan.ProgressNotification += ProgressHandler;

        SetStatus(state, ScanStatus.Running);

        try
        {
            await rescan.PartialRescan(added, removed);
            Complete(state);
        }
        catch (Exception ex)
        {
            Fail(state, ex);
        }
        finally
        {
            rescan.ProgressNotification -= ProgressHandler;
        }
    }

    private static void UpdateProgress(ScanJobState state, float progress)
    {
        lock (state)
        {
            state.Progress = Math.Clamp(progress, 0, 100);
            state.Revision++;
        }
    }

    private static void SetStatus(ScanJobState state, ScanStatus status)
    {
        lock (state)
        {
            state.Status = status;
            state.Revision++;
        }
    }

    private static void Complete(ScanJobState state)
    {
        lock (state)
        {
            state.Status = ScanStatus.Completed;
            state.Progress = 100;
            state.CompletedUtc = DateTime.UtcNow;
            state.Revision++;
        }
    }

    private void Fail(ScanJobState state, Exception ex)
    {
        logger.LogError(ex, "Scan {ScanId} failed", state.ScanId);

        lock (state)
        {
            state.Status = ScanStatus.Failed;
            state.Error = ex.Message;
            state.CompletedUtc = DateTime.UtcNow;
            state.Revision++;
        }
    }

    private sealed class ScanJobState
    {
        public Guid ScanId { get; set; }
        public required string Type { get; set; }
        public ScanStatus Status { get; set; }
        public float Progress { get; set; }
        public int AddedCount { get; set; }
        public int RemovedCount { get; set; }
        public DateTime StartedUtc { get; set; }
        public DateTime? CompletedUtc { get; set; }
        public string? Error { get; set; }
        public long Revision { get; set; }

        public ScanStateResponse ToResponse()
        {
            lock (this)
            {
                return new ScanStateResponse(
                    ScanId,
                    Type,
                    Status,
                    Progress,
                    AddedCount,
                    RemovedCount,
                    StartedUtc,
                    CompletedUtc,
                    Error,
                    Revision);
            }
        }
    }
}

