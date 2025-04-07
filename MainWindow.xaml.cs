using Lab.pages;
using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Lab
{
    public partial class MainWindow : Window
    {
        LabEntities Lab = new LabEntities();
        private string currentCaptchaText; // Переменная для хранения текущего текста CAPTCHA
        private Random random = new Random();
        private DateTime captchaBlockEndTime; // Время окончания блокировки ввода
        public string ipAddress;
        public static DateTime T; // Время входа
        public static int idUser; // Id вошедшего пользователя
        private int failedLoginAttempts = 0; // Переменная для отслеживания количества неверных попыток входа


        public MainWindow()
        {
            InitializeComponent();
            HideCaptcha(); // Скрываем CAPTCHA и кнопку проверки до первой неудачной попытки входа
        }

        //Скрыть капчу
        private void HideCaptcha()
        {
            CaptchaText.Visibility = Visibility.Collapsed;
            UserInputTextBox.Visibility = Visibility.Collapsed;
            RegenerateCaptchaButton.Visibility = Visibility.Collapsed;
        }

        //Показать капчу
        private void ShowCaptcha()
        {
            CaptchaText.Visibility = Visibility.Visible;
            UserInputTextBox.Visibility = Visibility.Visible;
            RegenerateCaptchaButton.Visibility = Visibility.Visible;
            RegenerateCaptcha(); // Генерируем CAPTCHA
        }

        //Показать/скрыть пароль
        private void LookButton_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordTextBox.Visibility == Visibility.Collapsed)  // Если пароль скрыт
            {
                // Копируем пароль из PasswordBox в TextBox
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;  // Скрываем PasswordBox
                PasswordTextBox.Visibility = Visibility.Visible;    // Показываем TextBox с паролем
            }
            else
            {
                // Копируем текст из TextBox обратно в PasswordBox
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;  // Скрываем TextBox
                PasswordBox.Visibility = Visibility.Visible;     // Показываем PasswordBox
            }
        }

        //Войти
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Вход заблокирован на 10 сек после неверной капчи?
            if (DateTime.Now < captchaBlockEndTime)
            {
                MessageBox.Show("Попробуйте войти через 10 секунд после неудачной попытки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Логин и пароль не пустые?
            if (string.IsNullOrEmpty(LoginTextBox.Text) || string.IsNullOrEmpty(PasswordBox.Password))
            {
                MessageBox.Show("Введите логин и пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                LogFailedLoginAttempt(2);
                return;
            }

            var user = Lab.users_.FirstOrDefault(u => u.login == LoginTextBox.Text);

            // Логин не найден?
            if (user == null)
            {
                HandleFailedLogin(); //показ капчи и тд
                return;
            }

            // Логин найден, проверки пароля
            if (ValidatePassword(user))
            {
                // Проверяем, была ли показана CAPTCHA
                if (failedLoginAttempts >= 1)
                {
                    // Проверка CAPTCHA
                    if (!UserInputTextBox.Text.Equals(currentCaptchaText, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleCaptchaError(); // Неверная капча
                        return;
                    }
                }

                HandleSuccessfulLogin(user); //Пароль верный, вход
            }
            else
            {
                HandleFailedLogin();  // пользователь не найден
            }
        }

        //Пользователь не найден
        private void HandleFailedLogin()
        {
            failedLoginAttempts++; // Увеличиваем количество неверных попыток входа

            if (failedLoginAttempts == 1) // Если это первая неверная попытка
            {
                ShowCaptcha(); // Показываем CAPTCHA
            }
            ShowCaptchaWithError("Неверный логин или пароль, повторите с CAPTCHA"); // Показ CAPTCHA с сообщением об ошибке
        }

        // Показ CAPTCHA с сообщением об ошибке
        private void ShowCaptchaWithError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            captchaBlockEndTime = DateTime.Now.AddSeconds(10); // Устанавливаем время блокировки 10 сек
            LogFailedLoginAttempt(4); //Запись в историю (ошибка входа)
        }

        //Логин найден, проверка пароля
        private bool ValidatePassword(users_ user)
        {
            string hashedPassword = user.password;
            if (hashedPassword.Length > 20) // если пароль больше 20, то он хеширован
            {
                return ValidateHashedPassword(hashedPassword); // Проверка хешированного пароля
            }
            else
            {
                return PasswordBox.Password == user.password;
            }
        }

        // Проверка хешированного пароля
        private bool ValidateHashedPassword(string hashedPassword)
        {
            try
            {
                byte[] src = Convert.FromBase64String(hashedPassword);
                if (src.Length != 49 || src[0] != 0)
                {
                    MessageBox.Show("Неверный формат хешированного пароля.");
                    LogFailedLoginAttempt(2); //Запись в историю (ошибка системы)
                    return false;
                }

                byte[] salt = new byte[16];
                Buffer.BlockCopy(src, 1, salt, 0, 16);
                byte[] storedHash = new byte[32];
                Buffer.BlockCopy(src, 17, storedHash, 0, 32);

                using (var pbkdf2 = new Rfc2898DeriveBytes(PasswordBox.Password, salt, 1000))
                {
                    byte[] computedHash = pbkdf2.GetBytes(32);
                    return Convert.ToBase64String(computedHash) == Convert.ToBase64String(storedHash);
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Ошибка при декодировании хешированного пароля. Проверьте данные в базе.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                LogFailedLoginAttempt(2); //Запись в историю (ошибка системы)
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                LogFailedLoginAttempt(2); //Запись в историю (ошибка системы)
                return false;
            }
        }

        //Запись в историю (ошибки входа)
        private void LogFailedLoginAttempt(int cause)
        {
            //Получаем ip
            if (Dns.GetHostAddresses(Dns.GetHostName()).Length > 0)
            {
                ipAddress = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString();
            }

            var story = new story_()
            {
                ip = ipAddress,
                startenter = DateTime.Now,
                lastenter = DateTime.Now,
                id_cause = cause
            };
            Lab.story_.Add(story);
            Lab.SaveChanges();
        }

        //Пароль верный, вход
        private void HandleSuccessfulLogin(users_ user)
        {
            HideCaptcha(); // Скрываем CAPTCHA и кнопку проверки
            ShowUserRoleMessage(user); // открытие окна пользователя
            // Запись данных пользователя
            T = DateTime.Now;
            idUser = user.id;
        }

        // открытие окна пользователя
        private void ShowUserRoleMessage(users_ user)
        {
            MessageBox.Show($"Вы вошли в учетную запись {user.role_.role}");
            WinUser newWindow = new WinUser(user);
            newWindow.Show();
            this.Close();
        }

        // Генерируем новую CAPTCHA
        private void RegenerateCaptcha()
        {
            currentCaptchaText = GenerateRandomCaptcha(6); // Генерация рандомной капча из 6 символов
            CaptchaText.Text = currentCaptchaText;
            ApplyCaptchaTransformations(); // Трансформация капчи
        }

        // Генерация рандомной капча из 6 символов
        private string GenerateRandomCaptcha(int length)
        {
            var charactersAvailable = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
            return new string(Enumerable.Repeat(charactersAvailable, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Трансформация капчи
        private void ApplyCaptchaTransformations()
        {
            var rotateTransform = new RotateTransform { Angle = random.Next(-10, 10) };
            CaptchaText.LayoutTransform = rotateTransform;

            var textDecoration = new TextDecoration
            {
                Location = TextDecorationLocation.Strikethrough,
                Pen = new Pen(Brushes.Black, 3)
            };

            CaptchaText.TextDecorations = new TextDecorationCollection { textDecoration };
        }

        // Неверная капча
        private void HandleCaptchaError()
        {
            if (string.IsNullOrEmpty(UserInputTextBox.Text))
            {
                MessageBox.Show("Введите CAPTCHA!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Неверная CAPTCHA. Попробуйте снова.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                UserInputTextBox.Clear();
                RegenerateCaptcha(); // Генерируем новую CAPTCHA при ошибке
            }
        }

        // кнопка поменять капчу
        private void RegenerateCaptchaButton_Click(object sender, RoutedEventArgs e)
        {
            RegenerateCaptcha(); // Генерируем новую CAPTCHA
        }

        // кнопка регистрация, переход
        private void RegTextBox_Click(object sender, RoutedEventArgs e)
        {
            registration win = new registration();
            win.Show();
            this.Close();
        }
    }
}