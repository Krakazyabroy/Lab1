using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Data.Entity.Migrations;

namespace Lab.pages
{
    public partial class PageAnaliz : Page
    {
        private static LabEntities Lab = new LabEntities();
        /// <summary>
        /// Имя анализатора
        /// </summary>
        public string name;
        private DateTime dateTimeStartAnalyzer;

        /// <summary>
        /// Инициализация
        /// </summary>
        public PageAnaliz()
        {
            InitializeComponent();
            var analyzers = Lab.analyzer_.ToList();
            AnalizFilter_ComboBox.ItemsSource = analyzers;
            AnalizFilter_ComboBox.DisplayMemberPath = "name";
        }

        // Загрузка табл
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            List<s_in_o_> allSer = Lab.s_in_o_.Where(z => z.id_status != 3).ToList();
            ListAnaliz.ItemsSource = allSer;
        }

        // Фильтрация
        private void AnalizFilter_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AnalizFilter_ComboBox.SelectedItem is analyzer_ selectedAnalyzer)
            {
                int analyzerId = selectedAnalyzer.id;
                name = selectedAnalyzer.name;

                var filteredServices = Lab.s_in_o_.Where(a =>
                    (a.services_.metka == analyzerId || a.services_.metka == 3) &&
                    a.id_status != 3).ToList();

                ListAnaliz.ItemsSource = filteredServices.Any() ? filteredServices : null;
                if (!filteredServices.Any())
                {
                    MessageBox.Show("Нет доступных услуг для выбранного анализатора.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите анализатор.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        
        private void Get_Click(object sender, RoutedEventArgs e)
        {
            dateTimeStartAnalyzer = DateTime.Now;

            if (AnalizFilter_ComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите анализатор.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ListAnaliz.SelectedItem is s_in_o_ selectedSer)
            {
                SendServiceToAnalyzer(selectedSer);
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите услугу для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void SendServiceToAnalyzer(s_in_o_ selectedSer)
        {
            try
            {
                Services services1 = new Services
                {
                    serviceCode = selectedSer.code.GetValueOrDefault()
                };

                List<Services> services = new List<Services> { services1 };

                var order = Lab.order_.FirstOrDefault(z => z.id == selectedSer.id_order);
                string patient = order.id_user.ToString();

                var httpWebRequest = (HttpWebRequest)WebRequest.Create($"http://localhost:5000/api/analyzer/{name}");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = new JavaScriptSerializer().Serialize(new { patient, services });
                    streamWriter.Write(json);
                }

                using (var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        MessageBox.Show($"Услуга {selectedSer.services_.service} успешно отправлена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        selectedSer.id_status = 2; // Устанавливаем статус услуги на 2
                        Lab.Entry(selectedSer).State = System.Data.Entity.EntityState.Modified; // Указываем, что объект изменен
                        Lab.SaveChanges(); // Сохраняем изменения в базе данных

                        // Обновляем статус заказа, если он не равен 2
                        var order1 = Lab.order_.FirstOrDefault(x => x.id == selectedSer.id_order);
                        if (order1 != null && order1.id_status != 2)
                        {
                            order1.id_status = 2; // Устанавливаем статус заказа на 2
                            Lab.Entry(order1).State = System.Data.Entity.EntityState.Modified;
                            Lab.SaveChanges();
                        }

                        // Запускаем анимацию
                        StartAnimation(selectedSer);

                        // Ждем 15 секунд перед началом опроса
                        await Task.Delay(15000);

                        // Опрос результатов
                        PollAnalyzerResults(selectedSer);
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (var responseStream = ex.Response.GetResponseStream())
                    using (var reader = new StreamReader(responseStream))
                    {
                        string errorResponse = reader.ReadToEnd();
                        MessageBox.Show($"Ошибка отправки: {errorResponse}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Ошибка отправки: Не удалось получить ответ от сервера.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PollAnalyzerResults(s_in_o_ selectedSer)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create($"http://localhost:5000/api/analyzer/{name}");
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "GET";

            int elapsedTime = 0; // Время, прошедшее с начала опроса
            int pollingInterval = 2000; // Интервал опроса в миллисекундах

            while (elapsedTime < 15000) // 15 секунд
            {
                try
                {
                    using (var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                    {
                        if (httpResponse.StatusCode == HttpStatusCode.OK)
                        {
                            using (Stream stream = httpResponse.GetResponseStream())
                            {
                                StreamReader reader = new StreamReader(stream);
                                string json = reader.ReadToEnd();
                                GetAnalizator getAnalizator = new JavaScriptSerializer().Deserialize<GetAnalizator>(json);

                                selectedSer.progress = getAnalizator.progress;

                                if (getAnalizator.patient != null)
                                {
                                    ProcessAnalyzerResults(selectedSer, getAnalizator);
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка получения результатов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                }

                System.Threading.Thread.Sleep(pollingInterval); // Ждем перед следующим опросом
                elapsedTime += pollingInterval; // Увеличиваем прошедшее время
            }
        }

        private void ProcessAnalyzerResults(s_in_o_ selectedSer, GetAnalizator getAnalizator)
        {
            try
            {
                var resultService = getAnalizator.services.First();
                selectedSer.result = resultService.result;

                // Получаем услугу, к которой относится результат
                var service = Lab.services_.FirstOrDefault(s => s.code == selectedSer.code);

                if (service != null)
                {
                    // Проверяем, является ли результат числом
                    if (double.TryParse(resultService.result, out double resultValue))
                    {
                        // Проверяем, попадает ли результат в допустимый диапазон
                        if (resultValue < service.ot_deviation || resultValue > service.do_deviation)
                        {
                            MessageBox.Show($"Результаты для услуги {selectedSer.services_.service}: {selectedSer.result} находятся вне допустимого диапазона ({service.ot_deviation} - {service.do_deviation}).", "Проблема с результатами", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Результаты для услуги {selectedSer.services_.service}: {selectedSer.result} находятся вне допустимого диапазона ({service.ot_deviation} - {service.do_deviation}).", "Проблема с результатами", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                var result = MessageBox.Show($"Результаты для услуги {selectedSer.services_.service}: {selectedSer.result}.\n Сохранить результаты?", "Успех", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    SaveResult(selectedSer);
                }
                else
                {
                    MessageBox.Show("Запрос будет отправлен заново.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    SendServiceToAnalyzer(selectedSer);
                }

                MessageBox.Show($"Результаты для услуги {selectedSer.services_.service} успешно обработаны.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обработки результатов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveResult(s_in_o_ selectedSer)
        {
            try
            {
                selectedSer.id_status = 3; // Устанавливаем статус "Завершено"
                Lab.Entry(selectedSer).State = System.Data.Entity.EntityState.Modified;
                Lab.SaveChanges();

                // Создаем запись о данных анализатора
                data_analyzer_ newAnalyzerData = new data_analyzer_()
                {
                    id_analyzer = (AnalizFilter_ComboBox.SelectedItem as analyzer_).id,
                    id_lab = WinUser.LaborantID,
                    dateTime_receipt = dateTimeStartAnalyzer,
                    id_s_in_o = selectedSer.id,
                    due_time = (int)(DateTime.Now - dateTimeStartAnalyzer).TotalSeconds
                };

                Lab.data_analyzer_.Add(newAnalyzerData);
                Lab.SaveChanges();

                // Проверяем, завершены ли все услуги в заказе
                var listServicesInOrder = Lab.s_in_o_.Where(x => x.id_order == selectedSer.id_order).ToList();
                bool allServicesCompleted = listServicesInOrder.All(x => x.id_status == 3);

                if (allServicesCompleted)
                {
                    MessageBox.Show("Все услуги в заказе выполнены!", "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
                   
                    var order = Lab.order_.FirstOrDefault(x => x.id == selectedSer.id_order);
                    if (order != null)
                    {
                        order.id_status = 3; // Устанавливаем статус заказа "Завершен"
                        Lab.order_.AddOrUpdate(order);
                        Lab.SaveChanges();
                    }
                }

                ListAnaliz.ItemsSource = Lab.s_in_o_.Where(z => z.id_status != 3).ToList(); // Загружаем обновленные данные
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения результата: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }//
        }

        private async void StartAnimation(s_in_o_ selectedSer)
        {
            // Получаем элемент DataGrid
            var dataGridRow = ListAnaliz.ItemContainerGenerator.ContainerFromItem(selectedSer) as DataGridRow;
            if (dataGridRow != null)
            {
                // Получаем доступ к элементам в строке
                var progressBar = FindVisualChild<ProgressBar>(dataGridRow);
                var progressText = FindVisualChild<Label>(dataGridRow);

                // Показываем элементы
                progressBar.Visibility = Visibility.Visible;
                progressText.Visibility = Visibility.Visible;

                // Анимация
                for (int i = 0; i <= 100; i++)
                {
                    progressBar.Value = i;
                    progressText.Content = $"{i}%"; // Обновляем текст с процентом
                    await Task.Delay(150); // Задержка для анимации
                }

                // Ждем 15 секунд перед скрытием элементов
                await Task.Delay(15000);

                // Скрываем элементы
                progressBar.Visibility = Visibility.Collapsed;
                progressText.Visibility = Visibility.Collapsed;
            }
        }

        // Метод для поиска визуального дочернего элемента
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }
    }
    /// <summary>
    /// Класс для представления услуги
    /// </summary>
    public class Services
    {
        /// <summary>
        /// Код услуги
        /// </summary>
        public int serviceCode { get; set; }
        /// <summary>
        /// Результат услуги
        /// </summary>
        public string result { get; set; } 
    }
    /// <summary>
    /// Класс для представления ответа от анализатора
    /// </summary>
    public class GetAnalizator
    {
        /// <summary>
        /// Информация о пациенте
        /// </summary>
        public string patient { get; set; }
        /// <summary>
        /// Список услуг
        /// </summary>
        public List<Services> services { get; set; }
        /// <summary>
        /// Прогресс выполнения
        /// </summary>
        public int progress { get; set; } 
    }
}

