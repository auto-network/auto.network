using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using AutoWeb.Client;

namespace AutoWeb.Components;

public partial class ConnectionHub : ComponentBase
{
    [Inject] private IAutoHostClient AutoHostClient { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private List<ConnectionInfo> connections = new();
    private List<ServiceDefinition> availableServices = new();
    private List<ProtocolDefinition> availableProtocols = new();

    private bool isAddingConnection = false;
    private ServiceType selectedService = ServiceType.OpenRouter;
    private ProtocolType selectedProtocol = ProtocolType.OpenAICompatible;
    private string newConnectionDescription = "";
    private string newConnectionApiKey = "";
    private bool isSaving = false;
    private int? showingKeyId = null;
    private int? deletingKeyId = null;
    private string errorMessage = "";
    private string successMessage = "";

    protected override async Task OnInitializedAsync()
    {
        await LoadRegistry();
        await LoadConnections();
    }

    private async Task LoadRegistry()
    {
        try
        {
            var registry = await AutoHostClient.ConnectionsGetRegistryAsync();
            availableServices = registry.Services.ToList();
            availableProtocols = registry.Protocols.ToList();

            // Set default protocol based on selected service
            if (availableServices.Count > 0)
            {
                var defaultService = availableServices[0];
                selectedService = defaultService.Type;
                selectedProtocol = defaultService.DefaultProtocol;
            }
        }
        catch
        {
            // Ignore errors loading registry
        }
    }

    private async Task LoadConnections()
    {
        try
        {
            var result = await AutoHostClient.ConnectionsGetAsync();
            connections = result.Connections.ToList();
        }
        catch
        {
            // Ignore errors loading connections
        }
    }

    private void ShowAddConnection()
    {
        isAddingConnection = true;

        // Set defaults
        if (availableServices.Count > 0)
        {
            var defaultService = availableServices[0];
            selectedService = defaultService.Type;
            selectedProtocol = defaultService.DefaultProtocol;
        }

        newConnectionDescription = "";
        newConnectionApiKey = "";
        errorMessage = "";
        successMessage = "";
    }

    private void CancelAddConnection()
    {
        isAddingConnection = false;
        newConnectionApiKey = "";
    }

    private void OnServiceChanged(ChangeEventArgs e)
    {
        if (Enum.TryParse<ServiceType>(e.Value?.ToString(), out var serviceType))
        {
            selectedService = serviceType;

            // Update protocol to match service's default
            var service = availableServices.FirstOrDefault(s => s.Type == serviceType);
            if (service != null)
            {
                selectedProtocol = service.DefaultProtocol;
            }
        }
    }

    private void OnProtocolChanged(ChangeEventArgs e)
    {
        if (Enum.TryParse<ProtocolType>(e.Value?.ToString(), out var protocolType))
        {
            selectedProtocol = protocolType;
        }
    }

    private async Task SaveNewConnection()
    {
        isSaving = true;
        errorMessage = "";
        successMessage = "";

        try
        {
            await AutoHostClient.ConnectionsCreateAsync(new CreateConnectionRequest
            {
                ApiKey = newConnectionApiKey,
                Description = string.IsNullOrWhiteSpace(newConnectionDescription)
                    ? $"{selectedService} Connection"
                    : newConnectionDescription,
                ServiceType = selectedService,
                Protocol = selectedProtocol
            });

            successMessage = "Connection saved successfully.";
            isAddingConnection = false;
            newConnectionApiKey = "";
            await LoadConnections();
        }
        catch (ApiException<ErrorResponse> ex)
        {
            errorMessage = ex.Result.Error ?? "Failed to save connection.";
        }
        catch (Exception)
        {
            errorMessage = "Failed to save connection.";
        }
        finally
        {
            isSaving = false;
        }
    }

    private void ToggleShowKey(int keyId)
    {
        showingKeyId = showingKeyId == keyId ? null : keyId;
    }

    private async Task DeleteConnection(int connectionId)
    {
        deletingKeyId = connectionId;
        errorMessage = "";
        successMessage = "";

        try
        {
            await AutoHostClient.ConnectionsDeleteAsync(connectionId);
            successMessage = "Connection deleted successfully.";
            await LoadConnections();
        }
        catch (ApiException<ErrorResponse> ex)
        {
            errorMessage = ex.Result.Error ?? "Failed to delete connection.";
        }
        catch (Exception)
        {
            errorMessage = "Failed to delete connection.";
        }
        finally
        {
            deletingKeyId = null;
        }
    }

    private string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.Now - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "just now";
        if (timeSpan.TotalHours < 1)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalDays < 1)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";

        return dateTime.ToString("MMM d");
    }

    private ProtocolType[] GetSupportedProtocols(ServiceType service)
    {
        var serviceInfo = availableServices.FirstOrDefault(s => s.Type == service);
        return serviceInfo?.SupportedProtocols.ToArray() ?? Array.Empty<ProtocolType>();
    }

    private string GetServiceDisplayName(ServiceType service)
    {
        var serviceInfo = availableServices.FirstOrDefault(s => s.Type == service);
        return serviceInfo?.DisplayName ?? service.ToString();
    }

    private string GetProtocolDisplayName(ProtocolType protocol)
    {
        var protocolInfo = availableProtocols.FirstOrDefault(p => p.Type == protocol);
        return protocolInfo?.DisplayName ?? protocol.ToString();
    }
}
