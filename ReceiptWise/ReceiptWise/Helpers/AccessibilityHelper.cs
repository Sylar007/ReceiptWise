namespace ReceiptWise.App.Helpers;

/// <summary>
/// Helper for setting accessibility properties
/// </summary>
public static class AccessibilityHelper
{
    public static void SetAccessibility(
        VisualElement element,
        string name,
        string? hint = null,
        bool isHeading = false)
    {
        SemanticProperties.SetDescription(element, name);

        if (!string.IsNullOrEmpty(hint))
        {
            SemanticProperties.SetHint(element, hint);
        }

        if (isHeading)
        {
            SemanticProperties.SetHeadingLevel(element, SemanticHeadingLevel.Level1);
        }
    }

    public static void AnnounceToScreenReader(string message)
    {
        SemanticScreenReader.Announce(message);
    }
}