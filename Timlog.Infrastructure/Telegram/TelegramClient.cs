using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Timlog.Application.Interfaces;

namespace Timlog.Infrastructure.Telegram;

public class TelegramClient : ITelegramClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramClient> _logger;

    public TelegramClient(HttpClient httpClient, ILogger<TelegramClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendTextMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        var payload = new { chat_id = chatId, text = text };
        var response = await _httpClient.PostAsJsonAsync("sendMessage", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Αποτυχία SendTextMessageAsync για ChatId: {ChatId}. Status: {StatusCode}, Error: {Error}", chatId, response.StatusCode, error);
        }
    }

    public async Task EditMessageTextAsync(long chatId, int messageId, string text, CancellationToken cancellationToken = default)
    {
        var payload = new { chat_id = chatId, message_id = messageId, text = text };
        var response = await _httpClient.PostAsJsonAsync("editMessageText", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Αποτυχία EditMessageTextAsync για ChatId: {ChatId}, MessageId: {MessageId}. Error: {Error}", chatId, messageId, error);
        }
    }

    public async Task SendTextMessageWithInlineKeyboardAsync(
        long chatId,
        string text,
        IEnumerable<IEnumerable<(string Text, string CallbackData)>> buttons,
        CancellationToken cancellationToken = default)
    {
        var keyboard = buttons.Select(row => row.Select(b => new { text = b.Text, callback_data = b.CallbackData }).ToArray()).ToArray();
        var payload = new
        {
            chat_id = chatId,
            text = text,
            reply_markup = new { inline_keyboard = keyboard }
        };

        var response = await _httpClient.PostAsJsonAsync("sendMessage", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Αποτυχία αποστολής InlineKeyboard για ChatId: {ChatId}. Error: {Error}", chatId, error);
        }
    }

    public async Task SendPhotoAsync(
        long chatId,
        Stream photoStream,
        string fileName,
        string caption,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(chatId.ToString()), "chat_id");
        content.Add(new StringContent(caption), "caption");
        content.Add(new StreamContent(photoStream), "photo", fileName);

        var response = await _httpClient.PostAsync("sendPhoto", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Αποτυχία αποστολής QR photo για ChatId: {ChatId}. Status: {StatusCode}, Error: {Error}", chatId, response.StatusCode, error);
        }
    }
}