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
using System.Windows.Media.Animation;
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

        private Border[] cells;   // клетки поля
        private bool[] isBlack;   // состояние клеток (true = черная)
        private double antAngle;  // накопленный угол поворота

        private StackPanel currentRuleList; // Какой список сейчас выполняется
        private int currentActionIndex = 0; // Индекс текущего действия в этом списке

        // Easing для поворота муравья
        private static readonly IEasingFunction TurnEase =
            new CubicEase { EasingMode = EasingMode.EaseInOut };

        // Easing для смены цвета клетки
        private static readonly IEasingFunction ColorEase =
            new QuadraticEase { EasingMode = EasingMode.EaseOut };

        private TimeSpan StepDuration =>
            TimeSpan.FromMilliseconds(Math.Max(antTimer.Interval.TotalMilliseconds * 0.9, 1));

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
            antDirection = r.Next(4);      // направление

            cells = new Border[totalCells];
            isBlack = new bool[totalCells];

            for (int i = 0; i < totalCells; i++)
            {
                isBlack[i] = r.NextDouble() < blackChance;

                // Своя незамороженная кисть у каждой клетки — иначе цвет нельзя анимировать
                cells[i] = new Border
                {
                    Background = new SolidColorBrush(isBlack[i] ? Colors.Black : Colors.White),
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0.5)
                };

                MyGrid.Children.Add(cells[i]);
            }

            // Сбрасываем муравья: убираем возможные анимации и ставим без плавности
            antAngle = antDirection * 90;
            AntRotate.BeginAnimation(RotateTransform.AngleProperty, null);
            AntRotate.Angle = antAngle;
            AntShape.Visibility = Visibility.Visible;

            MyGrid.UpdateLayout();
            PlaceAnt(animate: false);
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

        private void Field_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // При изменении размера окна пересчитываем размер и позицию муравья
            PlaceAnt(animate: false);
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

        // Анимации 

        private void Animate(Animatable target, DependencyProperty prop, double to,
                             bool animate, IEasingFunction easing = null)
        {
            if (!animate)
            {
                target.BeginAnimation(prop, null); // снимаем удерживаемую анимацию
                target.SetValue(prop, to);
                return;
            }

            target.BeginAnimation(prop, new DoubleAnimation(to, StepDuration)
            {
                EasingFunction = easing
            });
        }


        // Ставит муравья в клетку antIndex и подгоняет его размер под размер клетки
        private void PlaceAnt(bool animate)
        {
            if (cells == null || MyGrid.Columns == 0 || MyGrid.Rows == 0) return;

            int cols = MyGrid.Columns;
            int rows = MyGrid.Rows;
            double cellW = MyGrid.ActualWidth / cols;
            double cellH = MyGrid.ActualHeight / rows;

            AntShape.Width = cellW * 0.7;
            AntShape.Height = cellH * 0.7;

            double x = (antIndex % cols) * cellW + cellW * 0.15;
            double y = (antIndex / cols) * cellH + cellH * 0.15;

            // Движение линейное
            Animate(AntMove, TranslateTransform.XProperty, x, animate);
            Animate(AntMove, TranslateTransform.YProperty, y, animate);
        }

        // Плавно перекрашивает клетку
        private void SetCell(int index, bool black)
        {
            isBlack[index] = black;

            var brush = (SolidColorBrush)cells[index].Background;
            brush.BeginAnimation(SolidColorBrush.ColorProperty,
                new ColorAnimation(black ? Colors.Black : Colors.White, StepDuration)
                {
                    EasingFunction = ColorEase
                });
        }

        // Логика муравья 

        private void StepAnt()
        {
            if (currentRuleList == null || currentActionIndex >= currentRuleList.Children.Count)
            {
                currentRuleList = isBlack[antIndex] ? BlackRulesPanel : WhiteRulesPanel;
                currentActionIndex = 0;

                if (currentRuleList.Children.Count == 0) return;
            }

            var ruleBlock = (Border)currentRuleList.Children[currentActionIndex];
            string action = ruleBlock.Tag.ToString();

            switch (action)
            {
                case "Flip":
                    SetCell(antIndex, !isBlack[antIndex]);
                    break;

                case "Right":
                    antDirection = (antDirection + 1) % 4;
                    antAngle += 90;
                    Animate(AntRotate, RotateTransform.AngleProperty, antAngle, true, TurnEase);
                    break;

                case "Left":
                    antDirection = (antDirection + 3) % 4;
                    antAngle -= 90;
                    Animate(AntRotate, RotateTransform.AngleProperty, antAngle, true, TurnEase);
                    break;

                case "Go":
                    bool wrapped = MoveAntForward();
                    // При переходе через край поля муравей телепортируется
                    PlaceAnt(animate: !wrapped);
                    break;
            }

            currentActionIndex++;
        }

        // Сдвигает муравья на клетку вперед. Возвращает true, если он перешел через край поля
        private bool MoveAntForward()
        {
            int rows = MyGrid.Rows;
            int cols = MyGrid.Columns;
            int row = antIndex / cols;
            int col = antIndex % cols;
            bool wrapped = false;

            switch (antDirection)
            {
                case 0: wrapped = row == 0; row = (row - 1 + rows) % rows; break;
                case 1: wrapped = col == cols - 1; col = (col + 1) % cols; break;
                case 2: wrapped = row == rows - 1; row = (row + 1) % rows; break;
                case 3: wrapped = col == 0; col = (col - 1 + cols) % cols; break;
            }

            antIndex = row * cols + col;
            return wrapped;
        }
    }
}
