using AutoHost.Models;

namespace AutoHost.Services;

/// <summary>
/// Metadata about a service provider that can be connected to the system.
/// </summary>
public class ServiceDefinition
{
    public ServiceType Type { get; init; }
    public string DisplayName { get; init; } = "";
    public string Description { get; init; } = "";
    public ProtocolType[] SupportedProtocols { get; init; } = Array.Empty<ProtocolType>();
    public ProtocolType DefaultProtocol { get; init; }
}

/// <summary>
/// Metadata about a communication protocol used to interact with services.
/// </summary>
public class ProtocolDefinition
{
    public ProtocolType Type { get; init; }
    public string DisplayName { get; init; } = "";
    public string Description { get; init; } = "";
}

/// <summary>
/// Central registry of all supported services and protocols.
/// Defines valid ServiceType → ProtocolType mappings and provides metadata for UI.
/// </summary>
public static class ServiceRegistry
{
    /// <summary>
    /// All supported service providers with their metadata and protocol mappings.
    /// </summary>
    public static readonly IReadOnlyDictionary<ServiceType, ServiceDefinition> Services =
        new Dictionary<ServiceType, ServiceDefinition>
        {
            [ServiceType.OpenRouter] = new()
            {
                Type = ServiceType.OpenRouter,
                DisplayName = "OpenRouter",
                Description = "Multi-model aggregator service with access to multiple LLM providers",
                SupportedProtocols = new[] { ProtocolType.OpenAICompatible },
                DefaultProtocol = ProtocolType.OpenAICompatible
            },
            [ServiceType.OpenAI] = new()
            {
                Type = ServiceType.OpenAI,
                DisplayName = "OpenAI",
                Description = "ChatGPT, GPT-4, and other OpenAI models",
                SupportedProtocols = new[] { ProtocolType.OpenAICompatible },
                DefaultProtocol = ProtocolType.OpenAICompatible
            },
            [ServiceType.Anthropic] = new()
            {
                Type = ServiceType.Anthropic,
                DisplayName = "Anthropic",
                Description = "Claude models via Anthropic's native API",
                SupportedProtocols = new[] { ProtocolType.AnthropicAPI },
                DefaultProtocol = ProtocolType.AnthropicAPI
            },
            [ServiceType.Grok] = new()
            {
                Type = ServiceType.Grok,
                DisplayName = "Grok (xAI)",
                Description = "Grok models via xAI using OpenAI-compatible API",
                SupportedProtocols = new[] { ProtocolType.OpenAICompatible },
                DefaultProtocol = ProtocolType.OpenAICompatible
            }
        };

    /// <summary>
    /// All supported communication protocols with their metadata.
    /// </summary>
    public static readonly IReadOnlyDictionary<ProtocolType, ProtocolDefinition> Protocols =
        new Dictionary<ProtocolType, ProtocolDefinition>
        {
            [ProtocolType.OpenAICompatible] = new()
            {
                Type = ProtocolType.OpenAICompatible,
                DisplayName = "OpenAI Compatible",
                Description = "Standard OpenAI API format (used by OpenRouter, Grok, and others)"
            },
            [ProtocolType.AnthropicAPI] = new()
            {
                Type = ProtocolType.AnthropicAPI,
                DisplayName = "Anthropic API",
                Description = "Anthropic's native API format for Claude models"
            }
        };

    /// <summary>
    /// Get the list of protocols supported by a specific service.
    /// </summary>
    public static ProtocolType[] GetSupportedProtocols(ServiceType service)
    {
        if (!Services.TryGetValue(service, out var definition))
        {
            throw new ArgumentException($"Unknown service type: {service}", nameof(service));
        }
        return definition.SupportedProtocols;
    }

    /// <summary>
    /// Get the default (recommended) protocol for a specific service.
    /// </summary>
    public static ProtocolType GetDefaultProtocol(ServiceType service)
    {
        if (!Services.TryGetValue(service, out var definition))
        {
            throw new ArgumentException($"Unknown service type: {service}", nameof(service));
        }
        return definition.DefaultProtocol;
    }

    /// <summary>
    /// Validate that a service supports a specific protocol.
    /// </summary>
    public static bool IsValidMapping(ServiceType service, ProtocolType protocol)
    {
        if (!Services.TryGetValue(service, out var definition))
        {
            return false;
        }
        return definition.SupportedProtocols.Contains(protocol);
    }

    /// <summary>
    /// Get all services that support a specific protocol.
    /// </summary>
    public static IEnumerable<ServiceType> GetServicesForProtocol(ProtocolType protocol)
    {
        return Services.Values
            .Where(s => s.SupportedProtocols.Contains(protocol))
            .Select(s => s.Type);
    }
}
