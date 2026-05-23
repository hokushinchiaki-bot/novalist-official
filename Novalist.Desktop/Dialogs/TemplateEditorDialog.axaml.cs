using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Novalist.Desktop.ViewModels;

namespace Novalist.Desktop.Dialogs;

public partial class TemplateEditorDialog : UserControl
{
    public bool Saved { get; private set; }
    public TaskCompletionSource DialogClosed { get; } = new();

    public TemplateEditorDialog()
    {
        InitializeComponent();
    }

    public TemplateEditorDialog(TemplateEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            TemplateNameBox.Focus();
            TemplateNameBox.SelectAll();
        }, Avalonia.Threading.DispatcherPriority.Input);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            Saved = false;
            DialogClosed.TrySetResult();
        }
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        Saved = true;
        DialogClosed.TrySetResult();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Saved = false;
        DialogClosed.TrySetResult();
    }
}
