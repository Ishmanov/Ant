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
using System.Windows.Threading;

namespace Ant
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Random r = new Random();
        public DispatcherTimer antTimer;

        public int antIndex;
        public int antDirection; // 0 = Вверх, 1 = Вправо, 2 = Вниз, 3 = Влево.
        public string[] arrows = { "▲", "▶", "▼", "◀" };

        private StackPanel currentRuleList; // Какой список сейчас выполняется
        private int currentActionIndex = 0; // Индекс текущего действия в этом списке

        public MainWindow()
        {
            InitializeComponent();

            antTimer = new DispatcherTimer();
            antTimer.Interval = TimeSpan.FromMilliseconds(SpeedSlider.Value);
            antTimer.Tick += (s, e) => StepAnt(); // каждый тик вызывает шаг
            PauseButton.IsEnabled = false;
        }

        private void Generate(object sender, RoutedEventArgs e)
        {
            if (antTimer != null)
            {
                antTimer.Stop();
                StartButton.IsEnabled = true;
                PauseButton.IsEnabled = false;
            }
            currentRuleList = null;
            currentActionIndex = 0;

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
            if (MyGrid.Children.Count == 0) return;

            antTimer.Start();
            StartButton.IsEnabled = false;
            PauseButton.IsEnabled = true;
        }

        private void Stop(object sender, RoutedEventArgs e)
        {
            antTimer.Stop();
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
        }
        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (antTimer == null || SpeedLabel == null) return;

            int delay = (int)SpeedSlider.Value;
            antTimer.Interval = TimeSpan.FromMilliseconds(delay);
            SpeedLabel.Text = $"{delay} мс";
        }

        private void ActionBlock_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is Border sourceBlock)
            {
                // Передаем сам Border как данные для перетаскивания
                DragDrop.DoDragDrop(sourceBlock, sourceBlock, DragDropEffects.Copy);
            }
        }

        private void RulesPanel_Drop(object sender, DragEventArgs e)
        {
            if (sender is StackPanel targetPanel && e.Data.GetData(typeof(Border)) is Border sourceBlock)
            {
                string actionCode = sourceBlock.Tag.ToString();
                string actionText = ((TextBlock)sourceBlock.Child).Text;

                // Создаем визуальную копию блока для списка
                var newBlock = new Border
                {
                    Tag = actionCode,
                    Background = sourceBlock.Background,
                    Padding = new Thickness(10),
                    Margin = new Thickness(5),
                    Cursor = Cursors.Hand,
                    Child = new TextBlock
                    {
                        Text = $"{targetPanel.Children.Count + 1}. {actionText}"
                    }
                };

                // Удаление по правому клику и пересчет нумерации
                newBlock.MouseRightButtonDown += (s, args) =>
                {
                    targetPanel.Children.Remove(newBlock);
                    UpdateStepNumbers(targetPanel);
                };

                targetPanel.Children.Add(newBlock);
            }
        }

        private void UpdateStepNumbers(StackPanel panel)
        {
            for (int i = 0; i < panel.Children.Count; i++)
            {
                if (panel.Children[i] is Border b && b.Child is TextBlock tb)
                {
                    // Отрезаем старый номер до точки и ставим актуальный
                    string cleanText = tb.Text.Substring(tb.Text.IndexOf('.') + 2);
                    tb.Text = $"{i + 1}. {cleanText}";
                }
            }
        }

        private void StepAnt()
        {
            var currentCell = (Border)MyGrid.Children[antIndex];

            if (currentRuleList == null || currentActionIndex >= currentRuleList.Children.Count)
            {
                bool isBlack = currentCell.Background == Brushes.Black;
                currentRuleList = isBlack ? BlackRulesPanel : WhiteRulesPanel;
                currentActionIndex = 0;

                if (currentRuleList.Children.Count == 0) return;
            }

            var ruleBlock = (Border)currentRuleList.Children[currentActionIndex];
            string action = ruleBlock.Tag.ToString();

            switch (action)
            {
                case "Flip":
                    currentCell.Background = (currentCell.Background == Brushes.Black) ? Brushes.White : Brushes.Black;
                    break;

                case "Right":
                    antDirection = (antDirection + 1) % 4;
                    if (currentCell.Child is TextBlock tbRight)
                        tbRight.Text = arrows[antDirection];
                    break;

                case "Left":
                    antDirection = (antDirection + 3) % 4;
                    if (currentCell.Child is TextBlock tbLeft)
                        tbLeft.Text = arrows[antDirection];
                    break;

                case "Go":
                    var antVisual = currentCell.Child;
                    currentCell.Child = null;
                    MoveAntForward();
                    ((Border)MyGrid.Children[antIndex]).Child = antVisual;
                    break;
            }

            currentActionIndex++;
        }

        private void MoveAntForward()
        {
            int rows = MyGrid.Rows;
            int cols = MyGrid.Columns;
            int r = antIndex / cols;
            int c = antIndex % cols;

            if (antDirection == 0) r = (r - 1 + rows) % rows;
            else if (antDirection == 1) c = (c + 1) % cols;
            else if (antDirection == 2) r = (r + 1) % rows;
            else if (antDirection == 3) c = (c - 1 + cols) % cols;

            antIndex = r * cols + c;
        }
    }
}
