using System;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Lab.pages
{
    public partial class WinUser : Window
    {
        LabEntities Lab = new LabEntities();
        /// <summary>
        /// Id пользователя
        /// </summary>
        public static int IdUser;
        /// <summary>
        /// IP адресс пользователя
        /// </summary>
        public string ipAddress;
        public DispatcherTimer timer;
        public TimeSpan sessionDuration = new TimeSpan(2, 30, 0); //Длительность сеанса
        private DateTime sessionStartTime; // Время начала сеанса
        public static int LaborantID;
        private static DateTime? blockEndTime; // Время окончания блокировки
        private static bool isBlocked = false; // Флаг блокировки

        /// <summary>
        /// Инициализация элементов по роли
        /// </summary>
        /// <param name="user"></param>
        public WinUser(users_ user)
        {
            InitializeComponent();

            // Устанавливаем время начала сеанса
            sessionStartTime = DateTime.Now;
            LaborantID = user.role_.id;
            // Устанавливаем данные для элементов управления
            RoleLabel.Text = user.role_.role;
            NameLabel.Text = user.name;
            SurnameLabel.Text = user.surname;
            PatronymicLabel.Text = user.patronymic;

            IdUser = user.id;

            // Устанавливаем изображение пользователя
            if (!string.IsNullOrEmpty(user.avatar))
            {
                string imagePath = $"/pages/Image/{user.avatar}.png";
                Avatar.Source = new BitmapImage(new Uri(imagePath, UriKind.Relative));
            }

            SetButtonVisibility(user.role_.role); // Управляем видимостью кнопок в зависимости от роли
            SetTimerVisibility(user.role_.role);// Проверяем роль и запускаем таймер                                   
            CheckUserBlock(user); // Проверка блокировки при входе
        }

        // Проверка блокировки при входе
        private void CheckUserBlock(users_ user)
        {
            // Проверяем роль пользователя
            if (user.role_.role == "Лаборант" || user.role_.role == "Лаборант-исследователь")
            {
                // Получаем время последней блокировки из истории
                var lastBlock = Lab.story_.Where(s => s.id_user == user.id && s.id_cause == 3)
                                           .OrderByDescending(s => s.lastenter)
                                           .FirstOrDefault();

                if (lastBlock != null)
                {
                    blockEndTime = lastBlock.lastenter.Value.AddMinutes(30); // Время окончания блокировки
                    if (DateTime.Now < blockEndTime)
                    {
                        MessageBox.Show($"Вы не можете войти, пока выполняется кварцевание помещения. Пожалуйста, подождите до {blockEndTime.Value.ToString("HH:mm:ss")}.", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Warning);

                        // Используем Dispatcher для отложенного закрытия окна
                        Dispatcher.BeginInvoke(new Action(() => this.Close()), DispatcherPriority.Background);
                        return;
                    }
                }
            }
        }

        // Блокировка, если время лаборантов истекло
        private async void BlockUserFor30Minutes()
        {
            isBlocked = true; // Устанавливаем флаг блокировки
            blockEndTime = DateTime.Now.AddMinutes(30); // Устанавливаем время окончания блокировки
            UpdateStory(3); // Запись в историю (вышло время)

            // Закрываем текущее окно
            this.Hide(); // Скрываем текущее окно вместо закрытия

            // Открываем главное окно (форму авторизации)
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            // Ждем 30 минут асинхронно
            await Task.Delay(TimeSpan.FromMinutes(30));

            MessageBox.Show("Вы снова можете войти.", "Разблокировка", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Работа таймера для лаборантов
        private bool isFifteenMinutesWarningShown = false; // Флаг для отслеживания, было ли показано сообщение о 15 минутах

        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeSpan elapsedTime = DateTime.Now - sessionStartTime; // Вычисляем прошедшее время
            TimeSpan remainingTime = sessionDuration - elapsedTime; // Вычисляем оставшееся время

            if (remainingTime.TotalSeconds <= 0)
            {
                timer.Stop();
                MessageBox.Show("Время сеанса истекло! Вы будете заблокированы на 30 минут.", "Время вышло", MessageBoxButton.OK, MessageBoxImage.Warning);
                BlockUserFor30Minutes();
                return;
            }
            else if (remainingTime.TotalMinutes <= 15 && !isFifteenMinutesWarningShown)
            {
                MessageBox.Show("Осталось 15 минут!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                isFifteenMinutesWarningShown = true; // Устанавливаем флаг, чтобы сообщение не показывалось повторно
            }

            Timer.Content = remainingTime.ToString(@"hh\:mm\:ss"); // Обновляем отображение таймера
        }

        // Проверяем роль и запускаем таймер 
        private void SetTimerVisibility(string role)
        {
            if (role == "Лаборант" || role == "Лаборант-исследователь")
            {
                StartTimer(); // Запускаем таймер
            }
            else
            {
                Timer.Visibility = Visibility.Collapsed; // Скрываем таймер для других ролей
            }
        }
        // Запускаем таймер
        private void StartTimer()
        {
            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 1); // Обновление каждую секунду
            timer.Start();
        }

       
        // Управляем видимостью кнопок в зависимости от роли
        private void SetButtonVisibility(string role)
        {
            switch (role)
            {
                case "Лаборант":
                    AcceptBiomaterialButton.Visibility = Visibility.Visible; // Лаборант может создать заказ
                    GenerateReportButton.Visibility = Visibility.Visible; // Лаборант может сформировать отчеты
                    break;

                case "Лаборант-исследователь":
                    WorkWithAnalyzerButton.Visibility = Visibility.Visible; // Лаборант-исследователь может работать с анализатором
                    break;

                case "Бухгалтер":
                    ViewReportsButton.Visibility = Visibility.Visible; // Бухгалтер может просмотреть отчеты
                    break;

                case "Администратор":
                    StoryUsersButton.Visibility = Visibility.Visible; // Администратор может проконтролировать всех пользователей
                    NewSotrydButton.Visibility = Visibility.Visible; // Администратор может добавлять сотрудников
                    break;
                case "Пользователь":
                    TextForUser.Visibility = Visibility.Visible; // Пользователь видит надпись
                    break;
                default:
                    break;
            }
        }

        // Кнопка выйти
        private void Authorization_Click(object sender, RoutedEventArgs e)
        {
            UpdateStory(1);
            MainWindow window = new MainWindow();
            window.Show();
            this.Close();
        }

        // Запись в историю
        private void UpdateStory(int cause)
        {
            // Вычисляем ip
            if (Dns.GetHostAddresses(Dns.GetHostName()).Length > 0)
            {
                ipAddress = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString();
            }

            story_ story = new story_();
            story.ip = ipAddress;
            story.lastenter = DateTime.Now;
            story.startenter = sessionStartTime;
            story.id_user = IdUser;
            story.id_cause = cause;
            Lab.story_.Add(story);
            Lab.SaveChanges();
        }

        // Админ
        // Кнопка История входа
        private void StoryUsersButton_Click(object sender, RoutedEventArgs e)
        {
            MyFrame.Navigate(new pages.PageStory());
        }

        //Кнопка добавить сотрудника
        private void NewSotrydButton_Click(object sender, RoutedEventArgs e)
        {
            NewSotryd win = new NewSotryd();
            // Устанавливаем текущее окно как владельца модального окна
            win.Owner = this;
            // Открываем модальное окно
            win.ShowDialog();
        }

        // Лаборант
        // Кнопка сформировать отчет
        private void AcceptBiomaterialButton_Click(object sender, RoutedEventArgs e)
        {
            MyFrame.Navigate(new pages.PageBio());
        }

        // Кнопка создать заказ
        private void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            OrderNew win = new OrderNew();
            win.Owner = this;
            win.ShowDialog();
        }

        //Лаборант-исследователь
        //Кнопка работа с анализатором
        private void WorkWithAnalyzerButton_Click(object sender, RoutedEventArgs e)
        {
            MyFrame.Navigate(new pages.PageAnaliz());
        }

        //Бухгалтер
        private void ViewReportsButton_Click(object sender, RoutedEventArgs e)
        {
            MyFrame.Navigate(new pages.PageBux());

        }


    }
}