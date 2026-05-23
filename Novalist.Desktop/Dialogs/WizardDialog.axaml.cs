using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Novalist.Desktop.ViewModels;

namespace Novalist.Desktop.Dialogs;

public partial class WizardDialog : UserControl
{
    public TaskCompletionSource DialogClosed { get; } = new();
    private WizardDialogViewModel? _vm;

    public WizardDialog()
    {
        InitializeComponent();
    }

    public WizardDialog(WizardDialogViewModel vm) : this()
    {
        DataContext = vm;
        _vm = vm;
        vm.CloseRequested += () => DialogClosed.TrySetResult();
        vm.PropertyChanged += OnVmPropertyChanged;
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        FocusCurrentStepInput();
    }

    // StepTitle is rewritten on every step transition (including review),
    // so use it as the signal to re-focus the new primary input.
    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WizardDialogViewModel.StepTitle))
            FocusCurrentStepInput();
    }

    private void FocusCurrentStepInput()
    {
        if (_vm == null) return;
        // Post on Input priority so visibility bindings have applied before
        // we read IsVisible.
        Dispatcher.UIThread.Post(() =>
        {
            if (_vm.IsMultilineText && WizardMultilineBox.IsVisible)
            {
                WizardMultilineBox.Focus();
                WizardMultilineBox.SelectAll();
            }
            else if (_vm.IsTextStep && WizardTextBox.IsVisible)
            {
                WizardTextBox.Focus();
                WizardTextBox.SelectAll();
            }
            else if (_vm.IsNumberStep && WizardNumberBox.IsVisible)
            {
                WizardNumberBox.Focus();
            }
            // Choice / date / entity-list steps host their primary controls
            // inside ItemsControls or pickers without a stable x:Name; default
            // tab order picks the right control when the user starts typing.
        }, DispatcherPriority.Input);
    }
}
