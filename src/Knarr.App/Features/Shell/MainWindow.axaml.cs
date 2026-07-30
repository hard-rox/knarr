using Avalonia.Controls;
using Avalonia.Rendering;

namespace Knarr.App.Features.Shell;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        RendererDiagnostics.DebugOverlays = RendererDebugOverlays.Fps | RendererDebugOverlays.LayoutTimeGraph;
    }
}
