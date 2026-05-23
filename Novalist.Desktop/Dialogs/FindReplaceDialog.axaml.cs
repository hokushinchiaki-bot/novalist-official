using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Novalist.Core.Models;
using Novalist.Desktop.ViewModels;

namespace Novalist.Desktop.Dialogs;

public partial class FindReplaceDialog : UserControl
{
    public TaskCompletionSource DialogClosed { get; } = new();

    public FindReplaceDialog()
    {
        InitializeComponent();
    }

    public FindReplaceDialog(FindReplaceViewModel vm) : this()
    {
        DataContext = vm;
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            FindBox.Focus();
            FindBox.SelectAll();
        }, Avalonia.Threading.DispatcherPriority.Input);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
            DialogClosed.TrySetResult();
    }

    // Enter on either search field runs Find. Enter inside Replace also runs
    // Find rather than ReplaceAll because Replace-all is destructive; we want
    // the user to click the explicit Replace All button.
    private void OnFindBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is FindReplaceViewModel vm && vm.FindCommand.CanExecute(null))
        {
            vm.FindCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        DialogClosed.TrySetResult();
    }

    private void OnResultDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is FindReplaceViewModel vm
            && ResultsList.SelectedItem is FindMatch match)
        {
            vm.JumpToCommand.Execute(match);
        }
    }
}
