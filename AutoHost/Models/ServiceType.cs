using System.Text.Json.Serialization;

namespace AutoHost.Models;

/// <summary>
/// External service providers that can be connected to the system.
/// Phase 1 & 2: LLM services only. Future: Storage, Compute, etc.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServiceType
{
    OpenRouter,
    OpenAI,
    Anthropic,
    Grok
}

/// <summary>
/// Communication protocols used to interact with external services.
/// Multiple services may share the same protocol (e.g., OpenAI-Compatible).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProtocolType
{
    OpenAICompatible,
    AnthropicAPI
}
