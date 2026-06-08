using PhotoEditor.Maui.Sample.Pages;

namespace PhotoEditor.Maui.Sample;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(BuiltInEditorPage), typeof(BuiltInEditorPage));
        Routing.RegisterRoute(nameof(CustomEditorPage), typeof(CustomEditorPage));
        Routing.RegisterRoute(nameof(ThemedDefaultEditorPage), typeof(ThemedDefaultEditorPage));
    }
}
