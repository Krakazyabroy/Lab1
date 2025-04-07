using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Mail_LIB;

namespace Lab.pages
{
    /// <summary>
    /// Логика взаимодействия для NewSotryd.xaml
    /// </summary>
    public partial class NewSotryd : Window
    {
        LabEntities Lab = new LabEntities();
        users_ user = new users_();
        Validator validator = new Validator(); // Библиотека класс
        /// <summary>
        /// Инициализация
        /// </summary>
        public NewSotryd()
        {
            InitializeComponent();
            LoadComboBoxData(); // Заполнение ComboBox
        }

        // Заполнение ComboBox
        private void LoadComboBoxData()
        {
            // Заполнение ComboBox для типа полиса, исключая роль с ID 4
            var Rol = Lab.role_.Where(x => x.id != 4).Select(x => x.role).ToList();
            RolComboBox.ItemsSource = Rol;
        }

        // Далее 1 часть
        private void Reg1Button_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateReg1Fields()) // Проверка 1 части
                return;

            // Переключаем вкладку
            TabControl_Reg.SelectedIndex = 1;
        }

        //Кнопка очистить
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            NameTextBox.Clear();
            SurnameTextBox.Clear();
            PatronymicTextBox.Clear();
            DB_DatePicer.SelectedDate = null;
            TelephoneTextBox.Clear();
            EmailTextBox.Clear();
            RolComboBox.SelectedIndex = -1;
            LoginTextBox.Clear();
            PasswordBox.Clear();
            PasswordTextBox.Clear();
            ConfirmPasswordBox.Clear();
            ConfirmTextBox.Clear();
        }

        // Копка просмотр пароля
        private void LookButton_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordTextBox.Visibility == Visibility.Collapsed)  // Если пароль скрыт
            {
                // Копируем пароль из PasswordBox в TextBox
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;  // Скрываем PasswordBox
                PasswordTextBox.Visibility = Visibility.Visible;    // Показываем TextBox с паролем
                // Копируем пароль из PasswordBox в TextBox
                ConfirmTextBox.Text = ConfirmPasswordBox.Password;
                ConfirmPasswordBox.Visibility = Visibility.Collapsed;  // Скрываем PasswordBox
                ConfirmTextBox.Visibility = Visibility.Visible;    // Показываем TextBox с паролем
            }
            else
            {
                // Копируем текст из TextBox обратно в PasswordBox
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;  // Скрываем TextBox
                PasswordBox.Visibility = Visibility.Visible;     // Показываем PasswordBox
                // Копируем текст из TextBox обратно в PasswordBox
                ConfirmPasswordBox.Password = ConfirmTextBox.Text;
                ConfirmTextBox.Visibility = Visibility.Collapsed;  // Скрываем TextBox
                ConfirmPasswordBox.Visibility = Visibility.Visible;     // Показываем PasswordBox
            }

        }

        // Кнопка Отмена, переход
        private void Authorization_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Кнопка Регистрация
        private void Authorization1_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateRegistrationFields()) // Проверка всего
                return;

            // Получаем пароль из PasswordBox и хешируем его
            byte[] salt;
            byte[] buffer2;
            using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(PasswordBox.Password, 16, 1000))
            {
                salt = bytes.Salt;
                buffer2 = bytes.GetBytes(32);
            }

            byte[] dst = new byte[49]; // 1 + 16 + 32 = 49
            Buffer.BlockCopy(salt, 0, dst, 1, 16);
            Buffer.BlockCopy(buffer2, 0, dst, 17, 32);
            string pas = Convert.ToBase64String(dst);

            // Заполнение данных пользователя
            var user = new users_
            {
                login = LoginTextBox.Text,
                password = pas,
                role = Lab.role_.FirstOrDefault(r => r.role == RolComboBox.SelectedItem.ToString()).id,

                name = NameTextBox.Text,
                surname = SurnameTextBox.Text,
                patronymic = PatronymicTextBox.Text,
                date_birth = DateTime.Parse(DB_DatePicer.Text),
                telephone = TelephoneTextBox.Text,
                e_mail = EmailTextBox.Text,                
            };

            Lab.users_.Add(user);
            Lab.SaveChanges();
            MessageBox.Show("Регистрация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        // Проверка 1 части
        private bool ValidateReg1Fields()
        {

            if (string.IsNullOrEmpty(NameTextBox.Text) ||
                string.IsNullOrEmpty(SurnameTextBox.Text) ||
                DB_DatePicer.SelectedDate == null ||
                string.IsNullOrEmpty(TelephoneTextBox.Text) ||
                string.IsNullOrEmpty(EmailTextBox.Text))
            {
                MessageBox.Show("Все поля должны быть заполнены.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!validator.CheckMail(EmailTextBox.Text))
            {
                MessageBox.Show("Некорректный email.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!Regex.IsMatch(TelephoneTextBox.Text, @"^\d{11}$"))
            {
                MessageBox.Show("Номер телефона должен содержать 11 цифр.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            DateTime selectedDate = DB_DatePicer.SelectedDate.Value; // Получаем выбранную дату
            DateTime eighteenYearsAgo = DateTime.Today.AddYears(-18); // Дата 18 лет назад

            // Проверка, меньше ли выбранная дата 18 лет от текущей даты
            if (selectedDate > eighteenYearsAgo)
            {
                MessageBox.Show("Регистрация доступна только совершеннолетним пользователям", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            return true;
        }

        // Проверка всего
        private bool ValidateRegistrationFields()
        {
            ValidateReg1Fields(); // Проверка 1 часть
            // Валидация на заполненность полей 3 часть
            if (string.IsNullOrEmpty(LoginTextBox.Text) ||
                string.IsNullOrEmpty(PasswordBox.Password) ||
                string.IsNullOrEmpty(ConfirmPasswordBox.Password) ||
                RolComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("Все поля должны быть заполнены.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка валидности логина
            if (!validator.CheckLogin(LoginTextBox.Text))
            {
                MessageBox.Show("Логин должен содержать только латинские буквы и цифры и быть не менее 6 символов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка на уникальность логина в базе данных
            if (Lab.users_.Any(u => u.login.ToLower() == LoginTextBox.Text.ToLower()))
            {
                MessageBox.Show("Этот логин уже существует. Пожалуйста, выберите другой логин.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }


            // Проверка совпадения паролей
            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка валидности пароля
            if (!validator.CheckPassword(PasswordBox.Password))
            {
                MessageBox.Show("Пароль должен содержать хотя бы 8 символов, одну букву, одну цифру и один специальный символ (!#$%&'*+/=?^_{|}~).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }
}
