using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using RevitElementBipChecker.Model;
using RevitElementBipChecker.Viewmodel;

namespace RevitElementBipChecker.View
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class MainWindows : Window
    {
        public BipCheckerViewmodel Viewmodel;

        public MainWindows(BipCheckerViewmodel vm)
        {
            DataContext = vm;
            Viewmodel = vm;
            Viewmodel.frmmain = this;
            InitializeComponent();
            PreviewKeyDown += HandleEsc;
        }

        private void AboutOnClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://chuongmep.com/");
        }

        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    var header = (string) headerClicked.Column.Header;
                    Sort(header.RemoveInvalid(), direction);

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            var dataView =
                CollectionViewSource.GetDefaultView(lsBipChecker.ItemsSource);

            dataView.SortDescriptions.Clear();
            var sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

    }
}
