using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EducationCenterDesk
{
    public partial class Form1 : Form
    {
        private readonly string _url = "http://localhost:5257/api/oplatas";
        private readonly HttpClient _client = new HttpClient();
        private CancellationTokenSource _cts;

        // Поддержка серых подсказок-примеров (Placeholder) для .NET Framework
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        private void SetPlaceholder(TextBox box, string text)
        {
            SendMessage(box.Handle, EM_SETCUEBANNER, 0, text);
        }

        public Form1()
        {
            InitializeComponent();
            dataGridView1.AutoGenerateColumns = true;
            SetupHelp();
        }

        // ЗАДАНИЯ 2 и 3: Настройка подсказок, примеров в полях и справки F1
        private void SetupHelp()
        {
            // 1. Всплывающие подсказки при наведении мыши (ToolTip)
            toolTip.SetToolTip(btnLoad, "Загрузить актуальный список платежей с сервера");
            toolTip.SetToolTip(btnCancel, "Прервать текущую загрузку списка платежей");
            toolTip.SetToolTip(btnAdd, "Отправить данные и добавить новый платёж");
            toolTip.SetToolTip(dataGridView1, "Таблица со списком всех платежей учебного центра");

            toolTip.SetToolTip(txtRoditelId, "Введите числовой идентификатор родителя");
            toolTip.SetToolTip(txtRebenokId, "Введите числовой идентификатор ребёнка");
            toolTip.SetToolTip(txtZanyatiya, "Введите количество оплачиваемых занятий (от 1 до 12)");
            toolTip.SetToolTip(txtSumma, "Введите сумму платежа в рублях (от 500 до 100000)");

            // 2. Серые подсказки-примеры внутри пустых полей
            SetPlaceholder(txtRoditelId, "Например: 1");
            SetPlaceholder(txtRebenokId, "Например: 2");
            SetPlaceholder(txtZanyatiya, "Например: 8");
            SetPlaceholder(txtSumma, "Например: 4800");

            // 3. Развёрнутая справка по клавише F1 (HelpProvider)
            helpProvider.SetHelpString(txtRoditelId,
                "Идентификатор родителя (плательщика). Укажите целое положительное число, под которым родитель зарегистрирован в базе. Например: 1.");
            helpProvider.SetHelpString(txtRebenokId,
                "Идентификатор ребёнка (ученика). Укажите целое положительное число из карточки ученика. Например: 2.");
            helpProvider.SetHelpString(txtZanyatiya,
                "Количество оплачиваемых занятий по абонементу. Допустимое значение — целое число от 1 до 12. Например: 8.");
            helpProvider.SetHelpString(txtSumma,
                "Сумма оплаты в рублях. Допустимое значение — от 500 до 100000 рублей. Например: 4800.");
            helpProvider.SetHelpString(dataGridView1,
                "Таблица всех платежей. Для обновления списка нажмите кнопку «Загрузить».");
        }

        // Асинхронная загрузка
        private async void btnLoad_Click(object sender, EventArgs e)
        {
            btnLoad.Enabled = false;
            btnCancel.Enabled = true;
            progressBar1.Style = ProgressBarStyle.Marquee;
            lblStatus.Text = "Загрузка...";

            _cts = new CancellationTokenSource();

            try
            {
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

        // Отмена загрузки
        private void btnCancel_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
        }

        // Добавление платежа с проверкой через ErrorProvider
        private async void btnAdd_Click(object sender, EventArgs e)
        {
            errorProvider.Clear();
            bool hasError = false;

            if (!int.TryParse(txtRoditelId.Text, out int roditelId) || roditelId <= 0)
            {
                errorProvider.SetError(txtRoditelId, "Укажите корректный числовой ID родителя");
                hasError = true;
            }

            if (!int.TryParse(txtRebenokId.Text, out int rebenokId) || rebenokId <= 0)
            {
                errorProvider.SetError(txtRebenokId, "Укажите корректный числовой ID ребёнка");
                hasError = true;
            }

            if (!int.TryParse(txtZanyatiya.Text, out int zanyatiya))
            {
                errorProvider.SetError(txtZanyatiya, "Введите целое число занятий");
                hasError = true;
            }

            if (!decimal.TryParse(txtSumma.Text, out decimal summa))
            {
                errorProvider.SetError(txtSumma, "Введите числовое значение суммы");
                hasError = true;
            }

            if (hasError)
            {
                lblStatus.Text = "Исправьте ошибки в полях ввода";
                return;
            }

            btnAdd.Enabled = false;
            try
            {
                var newOplata = new CreateOplataRequest
                {
                    RoditelId = roditelId,
                    RebenokId = rebenokId,
                    KolichestvoZanyatiy = zanyatiya,
                    Summa = summa
                };

                var response = await _client.PostAsJsonAsync(_url, newOplata);

                if (response.IsSuccessStatusCode)
                {
                    lblStatus.Text = "Платёж успешно добавлен";
                    txtRoditelId.Clear();
                    txtRebenokId.Clear();
                    txtZanyatiya.Clear();
                    txtSumma.Clear();
                    await LoadDataAsync();
                }
                else if ((int)response.StatusCode == 400)
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    ShowServerValidationErrors(errorJson);
                    MessageBox.Show($"Ошибка проверки данных:\n{errorJson}", "Ошибка 400", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Ошибка сервера:\n{error}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Нет связи с сервером. {ex.Message}", "Ошибка соединения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnAdd.Enabled = true;
            }
        }

        private void ShowServerValidationErrors(string json)
        {
            try
            {
                using (var doc = JsonDocument.Parse(json))
                {
                    if (doc.RootElement.TryGetProperty("errors", out var errors))
                    {
                        if (errors.TryGetProperty("RoditelId", out var e1) && e1.GetArrayLength() > 0)
                            errorProvider.SetError(txtRoditelId, e1[0].GetString());
                        if (errors.TryGetProperty("RebenokId", out var e2) && e2.GetArrayLength() > 0)
                            errorProvider.SetError(txtRebenokId, e2[0].GetString());
                        if (errors.TryGetProperty("KolichestvoZanyatiy", out var e3) && e3.GetArrayLength() > 0)
                            errorProvider.SetError(txtZanyatiya, e3[0].GetString());
                        if (errors.TryGetProperty("Summa", out var e4) && e4.GetArrayLength() > 0)
                            errorProvider.SetError(txtSumma, e4[0].GetString());
                    }
                }
            }
            catch { }
        }


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

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
    "EducationCenterDesk — приложение для учёта оплат занятий\n" +
    "Версия: 1.0\n" +
    "Автор: Худалей Наталья, группа 24-ИСП-2",
    "О программе",
    MessageBoxButtons.OK,
    MessageBoxIcon.Information);

        }

        private void руководствоПользователяToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help", "index.html");
            if (System.IO.File.Exists(path))
                System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
            else
                MessageBox.Show("Файл справки не найден: " + path);

        }
    }

    // Классы данных
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
}
