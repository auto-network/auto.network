using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using AutoWeb.Client;

namespace AutoWeb.Pages;

public partial class Settings : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IAutoHostClient AutoHostClient { get; set; } = default!;

    private enum SettingsSection
    {
        Authentication,
        ApiKeys
    }

    private SettingsSection currentSection = SettingsSection.Authentication;
    private string? authToken = null;

    protected override async Task OnInitializedAsync()
    {
        // Check if user is authenticated
        authToken = await JS.InvokeAsync<string?>("sessionStorage.getItem", "authToken");

        if (string.IsNullOrEmpty(authToken))
        {
            Navigation.NavigateTo("/auth");
            return;
        }
    }

    private void SelectSection(SettingsSection section)
    {
        currentSection = section;
    }

    private string GetTabClass(SettingsSection section)
    {
        var baseClass = "px-4 py-2 font-medium transition-colors";
        return section == currentSection
            ? $"{baseClass} text-green-400 border-b-2 border-green-400"
            : $"{baseClass} text-gray-400 hover:text-gray-200";
    }

    private void GoBack()
    {
        Navigation.NavigateTo("/");
    }
}