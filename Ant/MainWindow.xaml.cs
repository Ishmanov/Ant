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

        public int antIndex;
        public int antDirection; // 0 = Вверх, 1 = Вправо, 2 = Вниз, 3 = Влево
        public string[] arrows = { "▲", "▶", "▼", "◀" };

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

            int totalCells = rows * columns;

            antIndex = r.Next(totalCells); // случайная клетка
            antDirection = r.Next(4); // направление

            for (int i = 0; i < totalCells; i++)
            {
                bool isBlack = r.NextDouble() < blackChance;

                var cell = new Border
                {
                    Background = isBlack ? Brushes.Black : Brushes.White,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0.5)
                };

                if (i == antIndex) // клетка с муравьем
                {
                    cell.Child = new TextBlock
                    {
                        Text = arrows[antDirection],
                        Foreground = Brushes.Red,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 20
                    };
                }

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
