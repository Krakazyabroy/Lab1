using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Font;
//using iTextSharp.text.pdf;
//using iTextSharp.text;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
    public partial class PageBio : Page
    {
        LabEntities Lab = new LabEntities();
        List<order_> allOrder; // Хранение всех записей истории

        /// <summary>
        /// Инициализация
        /// </summary>
        public PageBio()
        {
            InitializeComponent();
            // Заполнение ComboBox для статуса
            var Stat = Lab.status_.Select(x => x.status).ToList();
            StatusFilter_ComboBox.ItemsSource = Stat;
        }

        // Загрузка данных в табл
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            allOrder = Lab.order_.ToList();
            ListOrder.ItemsSource = allOrder; // Устанавливаем источник данных
        }

        // Фильтр по имени пользователя
        private void FullNameFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        // Фильтр по дате
        private void DateFilterPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        // Фильтр по статусу
        private void StatusFilter_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        // Метод фильтрация
        private void ApplyFilters()
        {
            try
            {
                string filterFullName = FullNameFilterTextBox.Text.ToLower().Trim(); // Получаем текст фильтра по полному имени и убираем пробелы
                DateTime? selectedDate = DateFilterPicker.SelectedDate; // Получаем выбранную дату
                string selectedStatus = StatusFilter_ComboBox.SelectedItem?.ToString(); // Получаем выбранный статус

                // Фильтруем записи по логину, дате и причине
                var filteredOrder = allOrder
                    .Where(s =>
                        (string.IsNullOrEmpty(filterFullName) ||
                        (s.users_ != null &&
                        (s.users_.FullName == null || s.users_.FullName.ToLower().Contains(filterFullName))) // Учитываем пустые значения логина
                        ) &&
                        (!selectedDate.HasValue ||
                        (s.creation_date.HasValue && s.creation_date.Value.Date == selectedDate.Value.Date)) && // Проверка на дату
                        (string.IsNullOrEmpty(selectedStatus) ||
                        (s.status_ != null && s.status_.status == selectedStatus)) // Фильтрация по статусу
                    )
                    .ToList();

                ListOrder.ItemsSource = filteredOrder; // Обновляем источник данных
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при фильтрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Действие Редактировать заказ
        private void Red_Click(object sender, RoutedEventArgs e)
        {
            if (ListOrder.SelectedItem is order_ selectedOrder)
            {

                // Создаем новое модальное окно и передаем выбранный заказ
                OrderNew orderWindow = new OrderNew(selectedOrder);
                bool? result = orderWindow.ShowDialog();
                if(result == false)
                {
                    UpdateListOrder(); // Метод для обновления списка заказов
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите заказ для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        // Действие добавить услуги в заказ
        private void Dob_Click(object sender, RoutedEventArgs e)
        {
            if (ListOrder.SelectedItem is order_ selectedOrder)
            {
                DobServices win = new DobServices(selectedOrder);
                bool? result = win.ShowDialog();
                if (result == false)
                {
                    UpdateListOrder(); // Метод для обновления списка заказов
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите заказ для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        // Действие распечатать
        private void Rasp_Click(object sender, RoutedEventArgs e)
        {
            if (ListOrder.SelectedItem is order_ selectedOrder)
            {
                CreatePdfDocument(selectedOrder);
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите заказ для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        // Обновить список заказов
        private void UpdateListOrder()
        {
            // Загружаем обновленные данные из базы данных
            allOrder = Lab.order_.ToList();
            ListOrder.ItemsSource = allOrder; // Обновляем источник данных

            // Сбрасываем значения фильтров
            FullNameFilterTextBox.Text = string.Empty; // Сбрасываем текстовое поле
            DateFilterPicker.SelectedDate = null; // Сбрасываем выбранную дату
            StatusFilter_ComboBox.SelectedItem = null; // Сбрасываем выбранный статус
        }

        //Создаем PDF-документ
        private void CreatePdfDocument(order_ currentOrder)
        {
            // Получаем полное имя пациента
            var user = Lab.users_
                .Where(z => z.id == currentOrder.id_user)
                .Select(z => new { z.name, z.surname }) // Получаем имя и фамилию как анонимный объект
                .FirstOrDefault();
            string fullName = user != null ? $"{user.name} {user.surname}" : "Неизвестный пользователь"; // Интерполяция строк

            // Создаем имя файла с использованием идентификатора заказа
            string fileName = $"Заказ_{currentOrder.id}.pdf"; // Используем интерполяцию строк
            string filePath = System.IO.Path.Combine(Environment.CurrentDirectory, fileName); // Путь к файлу

            try
            {
                // Создаем PDF-документ
                using (PdfWriter writer = new PdfWriter(filePath))
                {
                    using (PdfDocument pdf = new PdfDocument(writer))
                    {
                        // Инициализируем документ
                        Document document = new Document(pdf);

                        // Загружаем шрифт, поддерживающий кириллицу
                        //PdfFont font = PdfFontFactory.CreateFont("C:\\Users\\Juli\\OneDrive\\Документы\\C#\\УП01.01_2семестр\\Lab\\bin\\Debug\\arial.ttf", PdfEncodings.UTF8);
                        //document.SetFont(font);

                        // Добавляем многострочный заголовок
                        Paragraph header = new Paragraph()
                            .Add($"OOO \"Lab Med\"\n")
                            .Add($"Order № {currentOrder.id}\n")
                            .Add($"Date: {currentOrder.creation_date.Value.ToString("dd-MM-yyyy")}\n")
                            .Add($"Patient: {fullName}") // Используем fullName
                                                         //.SetFont(font) // Устанавливаем шрифт
                            .SetFontSize(20)
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                        document.Add(header);

                        // Создание таблицы
                        Table table = new Table(UnitValue.CreatePercentArray(new float[] { 1, 2 })).UseAllAvailableWidth();

                        // Добавление заголовков
                        table.AddHeaderCell(new Cell().Add(new Paragraph("Service code"))); // Код услуги
                        table.AddHeaderCell(new Cell().Add(new Paragraph("Service Name"))); // Название услуги

                        // Получаем услуги для текущего заказа
                        var selectedServices = Lab.s_in_o_.Where(s => s.id_order == currentOrder.id).ToList();

                        // Заполнение таблицы данными из коллекции
                        foreach (var service in selectedServices)
                        {
                            // Получаем информацию о услуге по ее ID
                            var serviceDetails = Lab.services_.FirstOrDefault(s => s.code == service.code);
                            if (serviceDetails != null)
                            {
                                table.AddCell(new Cell().Add(new Paragraph(serviceDetails.code.ToString()))); // Код услуги
                                table.AddCell(new Cell().Add(new Paragraph(serviceDetails.service))); // Название услуги
                            }
                        }

                        // Добавление таблицы в документ
                        document.Add(table);

                        // Закрываем документ
                        document.Close();
                    }
                }

                // Открываем созданный PDF-документ
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true // Используем оболочку для открытия файла
                });

                MessageBox.Show("PDF-документ создан и открыт!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

