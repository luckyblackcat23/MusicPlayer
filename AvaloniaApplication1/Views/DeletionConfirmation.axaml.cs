using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace AvaloniaApplication1;

public partial class DeletionConfirmation : Window
{
    private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

    public DeletionConfirmation()
    {
        InitializeComponent();
        YesButton.Click += (_, __) => Close(true);
        NoButton.Click += (_, __) => Close(false);
    }

    public Task<bool> ShowDialog(Window owner)
    {
        return ShowDialog<bool>(owner);
    }
}