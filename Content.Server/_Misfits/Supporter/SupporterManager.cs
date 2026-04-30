using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using Content.Shared._Misfits.Supporter;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server._Misfits.Supporter;

public interface ISupporterManager
{
    void Initialize();
    bool TryGetSupporter(NetUserId userId, [NotNullWhen(true)] out SupporterEntry? data);
    void SetSupporter(Guid userId, string username, string? title, string? nameColor);
    void RemoveSupporter(Guid userId);
    IReadOnlyList<SupporterEntry> GetAll();
}

public sealed class SupporterManager : ISupporterManager
{
    [Dependency] private readonly IResourceManager _res = default!;

    private static readonly ResPath SavePath = new("/supporters.json");
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, IncludeFields = true };

    private readonly Dictionary<Guid, SupporterEntry> _supporters = new();
    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = Logger.GetSawmill("supporter");
        Load();
    }

    public bool TryGetSupporter(NetUserId userId, [NotNullWhen(true)] out SupporterEntry? data)
    {
        return _supporters.TryGetValue(userId.UserId, out data);
    }

    public void SetSupporter(Guid userId, string username, string? title, string? nameColor)
    {
        _supporters[userId] = new SupporterEntry(userId, username, title, nameColor);
        Save();
    }

    public void RemoveSupporter(Guid userId)
    {
        _supporters.Remove(userId);
        Save();
    }

    public IReadOnlyList<SupporterEntry> GetAll() => _supporters.Values.ToList();

    private void Load()
    {
        try
        {
            if (!_res.UserData.Exists(SavePath))
                return;

            var json = _res.UserData.ReadAllText(SavePath);
            var entries = JsonSerializer.Deserialize<List<SupporterEntry>>(json, JsonOpts);
            if (entries == null)
                return;

            foreach (var entry in entries)
                _supporters[entry.UserId] = entry;

            _sawmill.Info($"Loaded {_supporters.Count} supporter(s).");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to load supporters.json: {ex}");
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_supporters.Values.ToList(), JsonOpts);
            _res.UserData.WriteAllText(SavePath, json);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to save supporters.json: {ex}");
        }
    }
}
