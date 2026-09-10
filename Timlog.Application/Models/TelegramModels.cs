using System.Text.Json.Serialization;

namespace Timlog.Application.Models;

public record TelegramUpdateDto(
    [property: JsonPropertyName("update_id")] long UpdateId,
    [property: JsonPropertyName("message")] TelegramMessageDto? Message,
    [property: JsonPropertyName("callback_query")] TelegramCallbackQueryDto? CallbackQuery
);

public record TelegramCallbackQueryDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("from")] TelegramUserDto? From,
    [property: JsonPropertyName("message")] TelegramMessageDto? Message,
    [property: JsonPropertyName("data")] string? Data
);

public record TelegramMessageDto(
    [property: JsonPropertyName("message_id")] long MessageId,
    [property: JsonPropertyName("from")] TelegramUserDto? From,
    [property: JsonPropertyName("chat")] TelegramChatDto Chat,
    [property: JsonPropertyName("text")] string? Text
);

public record TelegramUserDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("first_name")] string? FirstName,
    [property: JsonPropertyName("username")] string? Username
);

public record TelegramChatDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("type")] string Type
);

public record TelegramSendMessageRequest(
    [property: JsonPropertyName("chat_id")] long ChatId,
    [property: JsonPropertyName("text")] string Text
);