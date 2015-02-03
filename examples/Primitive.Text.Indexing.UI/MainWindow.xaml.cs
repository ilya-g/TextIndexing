using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Primitive.Text.Documents;

namespace Primitive.Text.Indexing.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(IndexerViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }


        public IndexerViewModel ViewModel
        {
            get { return (IndexerViewModel) DataContext; }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // delay indexing for 0.5 sec after window is loaded
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            ViewModel.StartIndexingAllSources();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[]) e.Data.GetData(DataFormats.FileDrop);

                ViewModel.AddDocumentSourcesFromPathList(files);
            }
        }



        private void ResultsListBox_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            ViewModel.ExploreToDocument(((ListBox)sender).SelectedItem as DocumentInfo);
        }


    }
}
