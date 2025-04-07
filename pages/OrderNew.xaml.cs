using System;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Lab.pages
{
    /// <summary>
    /// Логика взаимодействия для OrderNew.xaml
    /// </summary>
    public partial class OrderNew : Window
    {
        LabEntities Lab = new LabEntities();
        private order_ currentOrder; // Хранение текущего заказа

        /// <summary>
        /// Конструктор для создания нового заказа
        /// </summary>
        public OrderNew()
        {
            InitializeComponent();
            SetBioCodeHint();
        }

        /// <summary>
        /// Конструктор для редактирования существующего заказа
        /// </summary>
        /// <param name="order"></param>
        public OrderNew(order_ order) : this() // Вызов конструктора по умолчанию
        {
            currentOrder = Lab.order_.Find(order.id); // Загрузка заказа из базы данных;
            LoadOrderData();
        }

        // Заполнение данных переданного заказа
        private void LoadOrderData()
        {
            if (currentOrder != null)
            {
                BioTextBox.Text = currentOrder.id.ToString(); // Заполнение кода биоматериала
                BioTextBox.IsReadOnly = true;
                PatComboBox.SelectedValue = currentOrder.id_user; // Установка выбранного пользователя
                Date_DatePicer.SelectedDate = currentOrder.creation_date; // Установка даты
            }
        }

        // Заполняем подсказки и тд
        private void SetBioCodeHint()
        {
            // Получаем последний номер заказа из базы данных
            var lastOrder = Lab.order_.OrderByDescending(o => o.id).FirstOrDefault();
            int nextOrderNumber = (lastOrder != null ? lastOrder.id : 0) + 1;
            BioTextBox.Text = nextOrderNumber.ToString(); // Устанавливаем подсказку номера заказа
            BioTextBox.SelectionStart = BioTextBox.Text.Length; // Устанавливаем курсор в конец

            // Заполнение ComboBox для имени пациента
            var patients = Lab.users_.ToList();
            PatComboBox.ItemsSource = patients; // Заполняем ComboBox объектами пользователей
            PatComboBox.DisplayMemberPath = "FullName"; // Указываем, какое свойство отображать
            PatComboBox.SelectedValuePath = "id"; // Указываем, какое свойство использовать как значение


            Date_DatePicer.Text = DateTime.Now.ToString(); // Устанавливаем подсказку даты
        }

        // Добавление пациента
        private void NewPatButton_Click(object sender, RoutedEventArgs e)
        {
            newPolWind win = new newPolWind();
            win.Owner = this;
            win.ShowDialog();
        }

        //Биоматериал при нажатии на Enter
        private void BioTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BioCodeCheck();
            }
        }

        // Проверка кода биоматериала (заказа)
        private bool BioCodeCheck()
        {
            string bioCode = BioTextBox.Text;

            // Проверка на пустое значение
            if (string.IsNullOrWhiteSpace(bioCode))
            {
                MessageBox.Show("Введите код биоматериала.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Попытка преобразовать код биоматериала в число
            if (int.TryParse(bioCode, out int bioCodeInt))
            {
                // Проверка на дублирование только если это новый заказ
                if (currentOrder == null || (currentOrder != null && currentOrder.id != bioCodeInt))
                {
                    // Проверка на дублирование
                    if (Lab.order_.Any(o => o.id == bioCodeInt))
                    {
                        MessageBox.Show("Этот код биоматериала уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }
            else
            {
                MessageBox.Show("Неверный формат кода биоматериала. Введите только цифры.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return true;
        }

        // Кнопка Сохранить (проверки + запись в бд)
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Если поля пустые
            if (string.IsNullOrEmpty(BioTextBox.Text) ||
               PatComboBox.SelectedIndex == -1 ||
               !Date_DatePicer.SelectedDate.HasValue)
            {
                MessageBox.Show("Все поля должны быть заполнены.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            BioCodeCheck(); // Проверка кода биоматериала

            // Если дата больше настоящей
            if (Date_DatePicer.SelectedDate.Value.Date > DateTime.Today)
            {
                MessageBox.Show("Дата не может быть больше настоящей", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CreateOrder(1); // Создаем заказ (статус = в обработке)
            this.Close();
        }

        // Создание/редактирование заказа
        private void CreateOrder(int status)
        {
            var selectedUser = PatComboBox.SelectedItem as users_; // Получаем выбранного пользователя
            try
            {
                if (currentOrder != null) // Если редактируем существующий заказ
                {
                    // Обновляем свойства существующего заказа
                    currentOrder.creation_date = Date_DatePicer.SelectedDate.Value; // Используем SelectedDate
                    currentOrder.id_user = selectedUser.id;
                    currentOrder.id_status = status;

                    Lab.order_.AddOrUpdate(currentOrder);
                }
                else // Если создаем новый заказ
                {
                    var order = new order_()
                    {
                        creation_date = Date_DatePicer.SelectedDate.Value, // Используем SelectedDate
                        id_status = status,
                        id_user = selectedUser.id,
                        lead_time = 0
                    };

                    Lab.order_.Add(order);
                }

                Lab.SaveChanges(); // Сохраняем изменения в базе данных
                MessageBox.Show($"Заказ сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка отмена
        private void EndButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


    }
}