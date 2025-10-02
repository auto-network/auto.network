using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace AutoWeb.Client;

public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;

    public AuthorizationMessageHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            // Always add the auth token if we have one
            // Let the server decide what requires authentication
            var authToken = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", "authToken");
            if (!string.IsNullOrEmpty(authToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }
        }
        catch
        {
            // Ignore errors getting token
        }

        return await base.SendAsync(request, cancellationToken);
    }
}