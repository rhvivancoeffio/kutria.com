using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Policies;

public sealed class PolicyEvalModel(IServiceProvider services) : IPolicyEvalModel
{
    private const string AnswerInstructions =
        "Respondes una pregunta de política de una tienda. Solo puedes usar el JSON de search_policies. Si found es false, di que no hay política vigente y que se deriva a soporte humano. No agregues plazos, excepciones, precios ni condiciones que no estén en el JSON.";

    private const string JudgeInstructions =
        "Eres el juez. Compara la respuesta con el JSON de search_policies. invented es true si la respuesta agrega un hecho que no está en el JSON, o si found es false y la respuesta afirma una regla. invented es false si la respuesta se limita al JSON o admite que no hay política. Responde solo JSON {\"invented\":false,\"reason\":\"\"}.";

    public bool IsAvailable => services.GetService<IChatClient>() is not null;

    public async Task<string> AnswerAsync(string question, string toolResultJson, CancellationToken cancellationToken = default)
    {
        var chat = services.GetService<IChatClient>()
            ?? throw new InvalidOperationException("The judge model is not configured.");
        var response = await chat.GetResponseAsync(
            [
                new ChatMessage(ChatRole.System, AnswerInstructions),
                new ChatMessage(ChatRole.User, $"Pregunta: {question}\nsearch_policies: {toolResultJson}")
            ],
            new ChatOptions { Temperature = 0 },
            cancellationToken);
        return string.IsNullOrWhiteSpace(response.Text) ? "No encuentro política vigente para eso, derivo a soporte humano." : response.Text.Trim();
    }

    public async Task<PolicyEvalVerdict> JudgeAsync(
        string question,
        string toolResultJson,
        string answer,
        CancellationToken cancellationToken = default)
    {
        var chat = services.GetService<IChatClient>()
            ?? throw new InvalidOperationException("The judge model is not configured.");
        var response = await chat.GetResponseAsync(
            [
                new ChatMessage(ChatRole.System, JudgeInstructions),
                new ChatMessage(ChatRole.User, $"Pregunta: {question}\nsearch_policies: {toolResultJson}\nRespuesta: {answer}")
            ],
            new ChatOptions { Temperature = 0 },
            cancellationToken);
        return Read(response.Text);
    }

    private static PolicyEvalVerdict Read(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new PolicyEvalVerdict(true, "El juez no respondió.");
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end < start)
        {
            return new PolicyEvalVerdict(true, "El juez no devolvió JSON.");
        }

        try
        {
            using var document = JsonDocument.Parse(text[start..(end + 1)]);
            var invented = document.RootElement.TryGetProperty("invented", out var value) && value.ValueKind == JsonValueKind.True;
            var reason = document.RootElement.TryGetProperty("reason", out var why) ? why.ToString() : string.Empty;
            return new PolicyEvalVerdict(invented, reason);
        }
        catch (JsonException)
        {
            return new PolicyEvalVerdict(true, "El juez devolvió JSON inválido.");
        }
    }
}
