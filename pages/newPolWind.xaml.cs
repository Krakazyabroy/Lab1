using Mail_LIB;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace Lab.pages
{
    /// <summary>
    /// Логика взаимодействия для newPolWind.xaml
    /// </summary>
    public partial class newPolWind : Window
    {
        LabEntities Lab = new LabEntities();
        users_ user = new users_();
        Validator validator = new Validator();

        /// <summary>
        /// Окно добавления нового пользователя
        /// </summary>
        public newPolWind()
        {
            InitializeComponent();
            LoadComboBoxData();

        }

        // Заполнение ComboBox
        private void LoadComboBoxData()
        {
            // Заполнение ComboBox для типа полиса
            var policyTypes = Lab.policy_.Select(x => x.type_policy).ToList();
            TypePolicyTextBox.ItemsSource = policyTypes;

            // Заполнение ComboBox для страховых компаний
            var insuranceCompanies = Lab.insurance_company_.Select(c => c.name_company).ToList();
            InsuranceCompanyTextBox.ItemsSource = insuranceCompanies;
        }

        // Далее 1 часть
        private void Reg1Button_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateReg1Fields())
                return;

            // Переключаем вкладку
            TabControl_NewPol.SelectedIndex = 1;
        }

        // Завершить
        private void Reg2Button_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateReg1Fields() && !ValidateReg2Fields()) // Проверки 1 и 2 части
                return;

            // Заполнение данных пользователя
            var user = new users_
            {
                role = 4,

                name = NameTextBox.Text,
                surname = SurnameTextBox.Text,
                patronymic = PatronymicTextBox.Text,
                date_birth = DateTime.Parse(DB_DatePicer.Text),
                telephone = TelephoneTextBox.Text,
                e_mail = EmailTextBox.Text,
                passport_series = PassportSeriesTextBox.Text,
                passport_number = PassportNumberTextBox.Text,
                policy_number = PolicyNumberTextBox.Text,
                type_policy = Lab.policy_.FirstOrDefault(p => p.type_policy == TypePolicyTextBox.SelectedItem.ToString())?.id,
                insurance_company = Lab.insurance_company_.FirstOrDefault(c => c.name_company == InsuranceCompanyTextBox.SelectedItem.ToString())?.id

            };

            Lab.users_.Add(user);
            Lab.SaveChanges();
            MessageBox.Show("Пользователь добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();

        }

        //Кнопка очистить
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            NameTextBox.Clear();
            SurnameTextBox.Clear();
            PatronymicTextBox.Clear();
            TelephoneTextBox.Clear();
            EmailTextBox.Clear();
            DB_DatePicer.SelectedDate = null;
            PassportSeriesTextBox.Clear();
            PassportNumberTextBox.Clear();
            PolicyNumberTextBox.Clear();
            TypePolicyTextBox.SelectedIndex = -1;
            InsuranceCompanyTextBox.SelectedIndex = -1;

        }

        // Кнопка отмена
        private void Authorization_Click(object sender, RoutedEventArgs e)
        {
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

            if (validator.CheckMail(EmailTextBox.Text))
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

        // Проверка 2 части
        private bool ValidateReg2Fields()
        {
            if (string.IsNullOrEmpty(PassportSeriesTextBox.Text) ||
                string.IsNullOrEmpty(PassportNumberTextBox.Text) ||
                string.IsNullOrEmpty(PolicyNumberTextBox.Text) ||
                TypePolicyTextBox.SelectedIndex == -1 ||
                InsuranceCompanyTextBox.SelectedIndex == -1)
            {
                MessageBox.Show("Все поля должны быть заполнены.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!Regex.IsMatch(PassportSeriesTextBox.Text, @"^\d{4}$"))
            {
                MessageBox.Show("Серия паспорта должна содержать 4 цифр.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!Regex.IsMatch(PassportNumberTextBox.Text, @"^\d{6}$"))
            {
                MessageBox.Show("Номер паспорта должен содержать 6 цифр.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!Regex.IsMatch(PolicyNumberTextBox.Text, @"^\d{16}$"))
            {
                MessageBox.Show("Номер полиса должен содержать 16 цифр.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }
}
