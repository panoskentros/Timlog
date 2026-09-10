using Timlog.Application.Commands;

namespace Timlog.Application.Interfaces;

public interface ISetupStateService
{
    SetupSession? GetSession(long chatId);
    void StartSession(long chatId);
    void ClearSession(long chatId);
}