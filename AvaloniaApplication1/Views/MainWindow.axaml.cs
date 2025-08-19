using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaApplication1.Views;

public partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;
    }

    public async void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new DeletionConfirmation();
        bool confirmed = await dialog.ShowDialog(this);

        if (confirmed)
        {
            // proceed with delete
        }
    }
}
