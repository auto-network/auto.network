using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using AutoWeb.Client;

namespace AutoWeb.Pages;

public partial class Home : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IAutoHostClient AutoHostClient { get; set; } = default!;

    private string apiKey = string.Empty;
    private string userInput = string.Empty;
    private List<Message> messages = new List<Message>();
    private string errorMessage = string.Empty;
    private bool isLoadingKey = true;
    private string? authToken = null;
    private string? userEmail = null;

    private class Message
    {
        public bool IsUser { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    private class OpenRouterRequest
    {
        public string model { get; set; } = "x-ai/grok-4-fast:free";
        public List<Dictionary<string, string>> messages { get; set; } = new List<Dictionary<string, string>>();
    }

    private class OpenRouterResponse
    {
        public List<Choice> choices { get; set; } = new List<Choice>();
    }

    private class Choice
    {
        public MessageContent message { get; set; } = new MessageContent();
    }

    private class MessageContent
    {
        public string content { get; set; } = string.Empty;
    }

    private class ApiKeyResponse
    {
        public string ApiKey { get; set; } = string.Empty;
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            errorMessage = "Please enter your OpenRouter API Key.";
            return;
        }

        if (string.IsNullOrEmpty(userInput))
        {
            return;
        }

        // Add user message to history
        messages.Add(new Message { IsUser = true, Content = userInput });

        // Prepare request with full history
        var request = new OpenRouterRequest
        {
            messages = messages.Select(m => new Dictionary<string, string>
            {
                { "role", m.IsUser ? "user" : "assistant" },
                { "content", m.Content }
            }).ToList()
        };

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
            Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await Http.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<OpenRouterResponse>();
                if (result != null && result.choices.Count > 0)
                {
                    var botMessage = result.choices[0].message.content;
                    messages.Add(new Message { IsUser = false, Content = botMessage });
                    errorMessage = string.Empty;
                }
            }
            else
            {
                errorMessage = $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Exception: {ex.Message}";
        }

        userInput = string.Empty;
        await ScrollToBottom();
    }

    private async Task HandleKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await SendMessage();
        }
    }

    private async Task ScrollToBottom()
    {
        await JS.InvokeVoidAsync("scrollToBottom", ".h-96");
    }

    protected override async Task OnInitializedAsync()
    {
        // Check if user is authenticated
        authToken = await JS.InvokeAsync<string?>("sessionStorage.getItem", "authToken");
        userEmail = await JS.InvokeAsync<string?>("sessionStorage.getItem", "userEmail");

        if (string.IsNullOrEmpty(authToken))
        {
            Navigation.NavigateTo("/auth");
            return;
        }

        await LoadApiKey();
    }

    private async Task LoadApiKey()
    {
        try
        {
            var result = await AutoHostClient.AuthGetApiKeyAsync();
            if (!string.IsNullOrEmpty(result.ApiKey))
            {
                apiKey = result.ApiKey;
            }
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            // Token expired or invalid, redirect to auth
            await JS.InvokeVoidAsync("sessionStorage.clear");
            Navigation.NavigateTo("/auth");
        }
        catch
        {
            // AutoHost not running or not accessible - that's ok, user can enter manually
        }
        finally
        {
            isLoadingKey = false;
        }
    }

    private async Task SaveApiKey()
    {
        try
        {
            await AutoHostClient.AuthSaveApiKeyAsync(new SaveApiKeyRequest { ApiKey = apiKey });
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            // Token expired or invalid, redirect to auth
            await JS.InvokeVoidAsync("sessionStorage.clear");
            Navigation.NavigateTo("/auth");
        }
        catch (ApiException ex)
        {
            errorMessage = "Failed to save API key to local server";
        }
        catch
        {
            // AutoHost not running - that's ok, will work without persistence
        }
    }


    private async Task Logout()
    {
        await JS.InvokeVoidAsync("sessionStorage.clear");
        Navigation.NavigateTo("/auth");
    }

    private void OpenSettings()
    {
        Navigation.NavigateTo("/settings");
    }
}