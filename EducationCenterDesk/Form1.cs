using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EducationCenterDesk
{
    public partial class Form1 : Form
    {
        // Адрес твоего сервера из ПР №4 (проверь порт, если у тебя другой!)
        private readonly string _url = "http://localhost:5257/api/oplatas";
        private readonly HttpClient _client = new HttpClient();
        private CancellationTokenSource _cts;

        public Form1()
        {
            InitializeComponent();
            // Русские заголовки для таблицы
            dataGridView1.AutoGenerateColumns = true;
        }

        // ЗАДАНИЕ 2: Асинхронная загрузка
        private async void btnLoad_Click(object sender, EventArgs e)
        {
            btnLoad.Enabled = false;
            btnCancel.Enabled = true;
            progressBar1.Style = ProgressBarStyle.Marquee;
            lblStatus.Text = "Загрузка...";

            _cts = new CancellationTokenSource();

            try
            {
                // Запрашиваем данные у сервера
                var data = await _client.GetFromJsonAsync<List<Oplata>>(_url, _cts.Token);
                dataGridView1.DataSource = data;
                lblStatus.Text = $"Загружено записей: {data.Count}";
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Загрузка отменена пользователем";
            }
            catch (HttpRequestException)
            {
                lblStatus.Text = "Нет связи с сервером. Проверьте, запущен ли сервис.";
            }
            finally
            {
                btnLoad.Enabled = true;
                btnCancel.Enabled = false;
                progressBar1.Style = ProgressBarStyle.Blocks;
            }
        }

        // ЗАДАНИЕ 5: Отмена загрузки
        private void btnCancel_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
        }

        // ЗАДАНИЕ 4: Добавление
        private async void btnAdd_Click(object sender, EventArgs e)
        {
            btnAdd.Enabled = false;
            try
            {
                // Создаем запрос
                var newOplata = new CreateOplataRequest
                {
                    RoditelId = 10,
                    RebenokId = 20,
                    KolichestvoZanyatiy = 4,
                    Summa = 4000
                };

                var response = await _client.PostAsJsonAsync(_url, newOplata);

                if (response.IsSuccessStatusCode)
                {
                    lblStatus.Text = "Платёж добавлен";
                    await LoadDataAsync(); // Перезагружаем таблицу
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Ошибка сервера:\n{error}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Нет связи с сервером. {ex.Message}");
            }
            finally
            {
                btnAdd.Enabled = true;
            }
        }

        // Вспомогательный метод для обновления таблицы
        private async Task LoadDataAsync()
        {
            var data = await _client.GetFromJsonAsync<List<Oplata>>(_url);
            dataGridView1.DataSource = data;
        }

      
            private void btnExperiment_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Эксперимент: окно сейчас зависнет...";

            try
            {
                // ВНИМАНИЕ: Здесь мы используем .Result, что вызывает блокировку!
                var data = _client.GetFromJsonAsync<List<Oplata>>(_url).Result;

                lblStatus.Text = "Если вы это видите, значит окно не зависло";
                dataGridView1.DataSource = data;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
    }

    // Классы данных (должны совпадать с сервером!)
    public class Oplata
    {
        public int Id { get; set; }
        public int RoditelId { get; set; }
        public int RebenokId { get; set; }
        public int KolichestvoZanyatiy { get; set; }
        public decimal Summa { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateOplataRequest
    {
        public int RoditelId { get; set; }
        public int RebenokId { get; set; }
        public int KolichestvoZanyatiy { get; set; }
        public decimal Summa { get; set; }
    }