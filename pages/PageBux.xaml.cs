using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.IO.Font;
using Org.BouncyCastle.Math;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Timers;
using System.Data.Entity;
using iText.IO.Image;
using Microsoft.Win32;
using Paragraph = iText.Layout.Element.Paragraph;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Draw;
using Table = iText.Layout.Element.Table;
using TextAlignment = iText.Layout.Properties.TextAlignment;
using iText.Layout.Borders;
using iText.Kernel.Colors;

namespace Lab.pages
{
    /// <summary>
    /// Логика взаимодействия для PageBux.xaml
    /// </summary>
    public partial class PageBux : Page
    {
        LabEntities Lab = new LabEntities();
        List<order_> allOrder; // Хранение всех записей истории
        List<order_> filteredOrder; // Хранение отфильтрованных заказов
       
        /// <summary>
        /// Инициализация
        /// </summary>
        public PageBux()
        {
            InitializeComponent();
            // Заполнение ComboBox для статуса
            var Comp = Lab.insurance_company_.Select(x => x.name_company).ToList();
            CompanFilter_ComboBox.ItemsSource = Comp;
        }

        // Загрузка данных в табл
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            allOrder = Lab.order_.Where(z => z.id_status == 3).ToList();
            ListOrder.ItemsSource = allOrder; // Устанавливаем источник данных
        }

        private void DateFilterPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }
        private void CompanFilter_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }


        private void ApplyFilters()
        {
            try
            {
                DateTime? selectedOtDate = OtDateFilterPicker.SelectedDate; // Получаем выбранную дату от
                DateTime? selectedDoDate = DoDateFilterPicker.SelectedDate; // Получаем выбранную дату до
                string selectedCompan = CompanFilter_ComboBox.SelectedItem?.ToString(); // Получаем выбранную компанию

                // Получаем ID компании, если выбрана
                int? companyId = null;
                if (!string.IsNullOrEmpty(selectedCompan))
                {
                    var company = Lab.insurance_company_.FirstOrDefault(c => c.name_company == selectedCompan);
                    companyId = company?.id;
                }

                // Фильтруем записи по дате и компании
                filteredOrder = allOrder
                    .Where(s =>
                        (!selectedOtDate.HasValue || (s.creation_date.HasValue && s.creation_date.Value.Date >= selectedOtDate.Value.Date)) && // Проверка на дату от
                        (!selectedDoDate.HasValue || (s.creation_date.HasValue && s.creation_date.Value.Date <= selectedDoDate.Value.Date)) && // Проверка на дату до
                        (companyId == null || s.id_user.HasValue && Lab.users_.Any(u => u.id == s.id_user && u.insurance_company == companyId)) && // Фильтрация по компании
                        (s.id_status == 3) // Статус заказа должен быть 3
                    )
                    .ToList();

                ListOrder.ItemsSource = filteredOrder; // Обновляем источник данных
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при фильтрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateInvoiceButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, выбрана ли компания
            if (CompanFilter_ComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите компанию.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Завершаем выполнение метода, если компания не выбрана
            }

            // Получаем выбранную компанию
            string selectedCompan = CompanFilter_ComboBox.SelectedItem.ToString();
            int? companyId = null;

            var company = Lab.insurance_company_.FirstOrDefault(c => c.name_company == selectedCompan);
            companyId = company?.id;

            // Получаем выбранные даты
            DateTime? dateFrom = OtDateFilterPicker.SelectedDate;
            DateTime? dateTo = DoDateFilterPicker.SelectedDate;

            // Суммируем общую сумму для выбранной компании и периода
            double totalSum = 0;

            // Получаем всех пользователей, которые принадлежат к выбранной компании
            var usersInCompany = Lab.users_.Where(u => u.insurance_company == companyId).ToList();

            // Проходим по всем пользователям и суммируем стоимость услуг в заказах
            foreach (var user in usersInCompany)
            {
                var userOrders = Lab.order_.Where(o => o.id_user == user.id && o.creation_date >= dateFrom && o.creation_date <= dateTo && o.id_status == 3).ToList();

                foreach (var order in userOrders)
                {
                    var servicesInOrder = Lab.s_in_o_.Where(s => s.id_order == order.id).ToList();

                    foreach (var service in servicesInOrder)
                    {
                        var serviceDetails = Lab.services_.FirstOrDefault(s => s.code == service.code);
                        if (serviceDetails != null && serviceDetails.price.HasValue)
                        {
                            totalSum += serviceDetails.price.Value; // Суммируем стоимость услуги
                        }
                    }
                }
            }

            // Сохранение общей суммы в БД
            try
            {
                var newCheck = new check_
                {
                    sum = totalSum,
                    date_ot = dateFrom,
                    date_do = dateTo,
                    id_comp = companyId,
                    id_bux = WinUser.IdUser  // Предполагается, что у вас есть доступ к текущему пользователю
                };

                // Добавляем чек в базу данных
                Lab.check_.Add(newCheck);
                Lab.SaveChanges(); // Сохраняем изменения

                MessageBox.Show($"Чек сохранен! Общая сумма: {totalSum:C2}", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении чека: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}