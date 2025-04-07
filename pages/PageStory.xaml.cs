using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Lab.pages
    
{
    /// <summary>
    /// 
    /// </summary>

    public partial class PageStory : Page
    {
        LabEntities Lab = new LabEntities();
        List<story_> allStories; // Хранение всех записей истории
        /// <summary>
        /// Инициализация
        /// </summary>
        public PageStory()
        {
            InitializeComponent();
            // Заполнение ComboBox для причины
            var Cous = Lab.cause_.Select(x => x.cause).ToList();
            CouseFilter_ComboBox.ItemsSource = Cous;
        }

        // При открытии страницы загружаем данные
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            allStories = Lab.story_.ToList(); // Загружаем все записи
            ListStory.ItemsSource = allStories; // Устанавливаем источник данных
        }

        // При изменении Фильтр логина
        private void LoginFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        // При изменении фильтра даты
        private void DateFilterPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        // Фильтр по причине
        private void CouseFilter_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }


        // Выборка по фильтрам
        private void ApplyFilters()
        {
            try
            {
                string filterLogin = LoginFilterTextBox.Text.ToLower().Trim(); // Получаем текст фильтра по логину и убираем пробелы
                DateTime? selectedDate = DateFilterPicker.SelectedDate; // Получаем выбранную дату
                string selectedCause = CouseFilter_ComboBox.SelectedItem?.ToString(); // Получаем выбранную причину

                // Фильтруем записи по логину, дате и причине
                var filteredStories = allStories
                    .Where(s =>
                        (string.IsNullOrEmpty(filterLogin) ||
                         (s.users_ != null && (s.users_.login == null || s.users_.login.ToLower().Contains(filterLogin))) || // Учитываем пустые значения логина
                        (string.IsNullOrWhiteSpace(filterLogin) && s.users_.login == null)) && // Выводим записи с пустым логином при вводе пробела
                        (!selectedDate.HasValue || (s.startenter.HasValue && s.startenter.Value.Date == selectedDate.Value.Date)) && // Проверка на дату
                        (string.IsNullOrEmpty(selectedCause) || (s.cause_ != null && s.cause_.cause == selectedCause)) // Фильтрация по причине
            )
            .ToList();

                ListStory.ItemsSource = filteredStories; // Обновляем источник данных
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при фильтрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}