using System.Collections.Concurrent;
using Timlog.Application.Interfaces;

namespace Timlog.Application.Commands;

public class SetupStateService : ISetupStateService
{
    private readonly ConcurrentDictionary<long, SetupSession> _sessions = new();

    public SetupSession? GetSession(long chatId)
    {
        _sessions.TryGetValue(chatId, out var session);
        return session;
    }

    public void StartSession(long chatId)
    {
        _sessions[chatId] = new SetupSession();
    }

    public void ClearSession(long chatId)
    {
        _sessions.TryRemove(chatId, out _);
    }
}