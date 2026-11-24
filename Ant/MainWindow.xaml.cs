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

namespace Ant
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Random r = new Random();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Generate(object sender, RoutedEventArgs e)
        {
            MyGrid.Children.Clear();
            int rows = Int32.Parse(RowsBlock.Text);
            int columns = Int32.Parse(ColumnsBlock.Text);
            double blackChance = Int32.Parse(ChanceBlock.Text) / 100.0;
            MyGrid.Rows = rows;
            MyGrid.Columns = columns;

            for (int i = 0; i < rows * columns; i++)
            {
                bool isBlack = r.NextDouble() < blackChance;

                var cell = new Border
                {
                    Background = isBlack ? Brushes.Black : Brushes.White,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0.5)
                };

                MyGrid.Children.Add(cell);
            }
        }

        private void Start(object sender, RoutedEventArgs e)
        {

        }

        private void Stop(object sender, RoutedEventArgs e)
        {

        }
    }
}
