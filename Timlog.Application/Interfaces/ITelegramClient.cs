namespace Timlog.Application.Interfaces;

public interface ITelegramClient
{
    Task SendTextMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);
    Task SendPhotoAsync(long chatId, Stream photoStream, string fileName, string caption, CancellationToken cancellationToken);

    Task SendTextMessageWithInlineKeyboardAsync(long chatId, string text, IEnumerable<IEnumerable<(string Text, string CallbackData)>> inlineKeyboard, CancellationToken cancellationToken = default);
    Task EditMessageTextAsync(long chatId, int messageId, string text, CancellationToken cancellationToken = default);
}