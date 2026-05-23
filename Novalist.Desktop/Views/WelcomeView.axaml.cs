using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Novalist.Desktop.ViewModels;

namespace Novalist.Desktop.Views;

public partial class WelcomeView : UserControl
{
    private WelcomeViewModel? _vm;

    public WelcomeView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (_vm != null) _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm = DataContext as WelcomeViewModel;
            if (_vm != null) _vm.PropertyChanged += OnVmPropertyChanged;
        };
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WelcomeViewModel.IsCreateFormOpen)) return;
        if (_vm?.IsCreateFormOpen != true) return;
        Dispatcher.UIThread.Post(() =>
        {
            NewProjectNameBox.Focus();
            NewProjectNameBox.SelectAll();
        }, DispatcherPriority.Input);
    }

    private void OnCreateFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is not WelcomeViewModel vm) return;
        if (!vm.CreateProjectCommand.CanExecute(null)) return;
        vm.CreateProjectCommand.Execute(null);
        e.Handled = true;
    }

    private void OnRemoveRecentClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem item) return;
        if (item.Tag is not RecentProjectCard card) return;
        if (DataContext is not WelcomeViewModel vm) return;
        vm.RemoveRecentProjectCommand.Execute(card);
    }
}
