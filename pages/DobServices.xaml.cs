using Aspose.BarCode.Generation; // Убедитесь, что у вас установлен пакет Aspose.BarCode
using iText.IO.Image;
using iText.Kernel.Pdf; // Убедитесь, что у вас установлен пакет iText7
using iText.Layout; // Для работы с элементами PDF
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;


namespace Lab.pages
{
    /// <summary>
    /// Логика взаимодействия для DobServices.xaml
    /// </summary>
    public partial class DobServices : Window
    {
        LabEntities Lab = new LabEntities();
        private order_ currentOrder; // Хранение текущего заказа
        /// <summary>
        /// Переменная счетчик
        /// </summary>
        public static int ch = 0;
        private Random random = new Random();

        /// <summary>
        /// Сохраняем текущий заказ
        /// </summary>
        /// <param name="order"></param>
        public DobServices(order_ order)
        {
            InitializeComponent();
            currentOrder = order; // Сохраняем текущий заказ
            LoadServices();
        }

        // Загружаем список услуг
        private void LoadServices()
        {
            // Загружаем услуги из базы данных
            var services = Lab.services_.ToList();

            // Проверяем, что currentOrder не равен null
            if (currentOrder == null)
            {
                MessageBox.Show("Текущий заказ не установлен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверяем, что s_in_o_ инициализирован
            if (currentOrder.s_in_o_ == null)
            {
                currentOrder.s_in_o_ = new List<s_in_o_>(); // Инициализируем, если это необходимо
                MessageBox.Show("инициализация.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

            }

            var serviceViewModels = services.Select(s => new ServiceViewModel
            {
                Id = s.code,
                Name = s.service,
                IsSelected = currentOrder.s_in_o_.Any(o => o.code == s.code) // Устанавливаем галочку, если услуга уже в заказе
            }).ToList();

            ServicesItemsControl.ItemsSource = serviceViewModels; // Устанавливаем источник данных
        }

        // Выбираем услугу
        private void ServiceCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Получаем CheckBox, который вызвал событие
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                // Получаем выбранную услугу
                ServiceViewModel selectedService = checkBox.DataContext as ServiceViewModel;
                if (selectedService != null)
                {
                    string formattedDateTime = currentOrder.creation_date.Value.ToString("yyyyMMdd");

                    // Генерация случайных 6 цифр
                    string randomDigits = random.Next(100000, 999999).ToString(); // Генерируем случайное число от 100000 до 999999

                    // Формируем строку для штрихкода из id заказа, даты создания, 6 случайных цифр
                    string bc = $"{currentOrder.id.ToString()} {formattedDateTime} {randomDigits}";

                    // Проверяем, не добавлена ли услуга уже
                    if (!currentOrder.s_in_o_.Any(o => o.code == selectedService.Id))
                    {
                        // Добавляем услугу с штрихкодом
                        s_in_o_ orderService = new s_in_o_
                        {
                            id_order = currentOrder.id,
                            code = selectedService.Id,
                            id_status = 1,
                            BarCode = bc // Сохраняем штрих-код
                        };
                        Lab.s_in_o_.Add(orderService); // Добавляем новую услугу
                        currentOrder.s_in_o_.Add(orderService); // Добавляем в текущий заказ
                        BarcodeMake(bc, selectedService.Name); // Генерируем штрихкод
                    }
                    else
                    {
                        // Если услуга уже добавлена, обновляем только штрих-код
                        var existingService = currentOrder.s_in_o_.First(o => o.code == selectedService.Id);
                        existingService.BarCode = bc; // Обновляем штрих-код
                        Lab.s_in_o_.AddOrUpdate(existingService);
                    }

                    Lab.SaveChanges(); // Сохраняем изменения в базе данных
                }
            }
        }

        // Формируем штрихкод
        private void BarcodeMake(string bc, string nameSer)
        {
            var imageType = "Jpeg";
            var imageFormat = (BarCodeImageFormat)Enum.Parse(typeof(BarCodeImageFormat), imageType);
            var encodeType = EncodeTypes.Code128;
            string imagePath = "Штрихкод_" + nameSer + "_" + ch.ToString() + "." + imageType;

            BarcodeGenerator generator = new BarcodeGenerator(encodeType, bc);
            generator.Save(imagePath, imageFormat);

            MessageBox.Show($"Штрихкод по услуге готов.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

            // Создаем PDF-документ с сохраненным штрихкодом
            CreatePdfDocument(imagePath, nameSer);

            ch++;
        }

        // Создаем PDF-документ
        private void CreatePdfDocument(string barcodeImagePath, string nameSer)
        {
            // Создаем имя файла с использованием идентификатора заказа
            string fileName = $"Штрихкод_{nameSer}_{ch}.pdf"; // Используем интерполяцию строк
            string filePath = Path.Combine(Environment.CurrentDirectory, fileName); // Путь к файлу

            // Создаем PDF-документ
            using (PdfWriter writer = new PdfWriter(filePath))
            {
                using (PdfDocument pdf = new PdfDocument(writer))
                {
                    // Инициализируем документ
                    Document document = new Document(pdf);

                    // Создаем изображение для PDF
                    var pdfImage = iText.IO.Image.ImageDataFactory.Create(barcodeImagePath);
                    var image = new iText.Layout.Element.Image(pdfImage);

                    // Устанавливаем размеры и позицию изображения (при необходимости)
                    image.ScaleToFit(400, 200); // Масштабируем изображение
                    image.SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER); // Центрируем изображение

                    // Добавляем изображение в документ
                    document.Add(image);

                    // Закрываем документ
                    document.Close();
                }
            }
            // Открываем созданный PDF-документ
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = true // Используем оболочку для открытия файла
            });
            MessageBox.Show("PDF-документ с штрихкодом создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Кнопка сохранить
        private void AddServicesButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем выбранные услуги
            var selectedServices = ServicesItemsControl.Items
                .Cast<ServiceViewModel>()
                .Where(s => s.IsSelected)
                .ToList();

            // Добавляем выбранные услуги в заказ
            foreach (var service in selectedServices)
            {
                // Проверяем, не добавлена ли услуга уже
                if (!currentOrder.s_in_o_.Any(o => o.code == service.Id))
                {
                    s_in_o_ orderService = new s_in_o_
                    {
                        id_order = currentOrder.id,
                        code = service.Id,
                        id_status = 1
                    };
                    Lab.s_in_o_.Add(orderService); // Добавляем новую услугу
                }
            }
            Lab.SaveChanges();

            CalculateTotal(); // Рассчитываем общее время и сумму

            MessageBox.Show("Услуги успешно добавлены в заказ.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        // Рассчитываем общее время
        private void CalculateTotal()
        {
            // Получаем все услуги, связанные с текущим заказом
            var orderServices = Lab.s_in_o_.Where(o => o.id_order == currentOrder.id).ToList();

            // Рассчитываем общее время и сумму
            int totalLeadTime = 0;

            foreach (var orderService in orderServices)
            {
                var service = Lab.services_.FirstOrDefault(s => s.code == orderService.code);
                if (service != null)
                {
                    totalLeadTime += service.due_date.Value; // Суммируем время выполнения
                }
                
            }
            currentOrder.lead_time = totalLeadTime;
            Lab.order_.AddOrUpdate(currentOrder);
            Lab.SaveChanges(); // Сохраняем изменения
        }

        // Кнопка отмена
        private void EndButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
/// <summary>
/// Класс выбранных услуг в заказе
/// </summary>
public class ServiceViewModel
{
    /// <summary>
    /// Id 
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Имя
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Выбраны
    /// </summary>
    public bool IsSelected { get; set; } 
}


