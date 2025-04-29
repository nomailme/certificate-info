using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using Avalonia;
using Avalonia.Controls;

namespace CertificateViewer.Components.CertificateChain;

public partial class CertificateChainPanel : UserControl
{
    public static readonly DirectProperty<CertificateChainPanel, ObservableCollection<X509Certificate2>>
        ItemsSourceProperty =
            AvaloniaProperty.RegisterDirect<CertificateChainPanel, ObservableCollection<X509Certificate2>>(
                nameof(ItemsSource),
                o => o.ItemsSource,
                (o, v) => o.ItemsSource = v);


    public static readonly DirectProperty<CertificateChainPanel, X509Certificate2?> SelectedItemProperty =
        AvaloniaProperty.RegisterDirect<CertificateChainPanel, X509Certificate2?>(
            nameof(SelectedItem),
            o => o.SelectedItem,
            (o, v) => o.SelectedItem = v,
            enableDataValidation: false);

    private ObservableCollection<X509Certificate2> _items = new();
    private X509Certificate2? _selectedItem;

    public CertificateChainPanel()
    {
        InitializeComponent();
        AffectsArrange<CertificateChainPanel>(ItemsSourceProperty);
        DataContext = this;
    }


    public ObservableCollection<X509Certificate2> ItemsSource
    {
        get => _items;
        set
        {
            SetAndRaise(ItemsSourceProperty, ref _items, value);
        }
    }




    public bool IsEmpty => ItemsSource.Count == 0;


    public X509Certificate2? SelectedItem
    {
        get => _selectedItem;
        set => SetAndRaise(SelectedItemProperty, ref _selectedItem, value);
    }
}
