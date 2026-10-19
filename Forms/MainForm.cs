using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using PostServiceFormsApp.Models;
using PostServiceFormsApp.Services;

namespace PostServiceFormsApp.Forms
{
    public partial class MainForm : Form
    {
        private readonly PostServiceClient _apiClient = new();
        private List<Posting> _currentPostings = new();

        public MainForm()
        {
            InitializeComponent();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            txtServerUrl.Text = _apiClient.BaseUrl;
            await LoadPostingsAsync();
        }

        // Завантаження списку відправлень із сервера
        private async Task LoadPostingsAsync()
        {
            try
            {
                lblStatus.Text = "Отримання даних із сервера...";
                lblStatus.ForeColor = Color.Black;
                btnRefresh.Enabled = false;

                _apiClient.BaseUrl = txtServerUrl.Text.Trim();

                var postings = await _apiClient.GetPostingsAsync();
                _currentPostings = postings.ToList();

                dgvPostings.DataSource = null;
                dgvPostings.DataSource = _currentPostings;

                lblCount.Text = $"Всього записів: {_currentPostings.Count}";
                lblStatus.Text = "Дані успішно завантажено";
                lblStatus.ForeColor = Color.DarkGreen;
            }
            // Перехоплюємо ТІЛЬКИ ті помилки, на які є зрозуміла реакція:
            // 1. Сервер недоступний або тайм-аут (StatusCode == null)
            catch (HttpRequestException ex) when (ex.StatusCode == null)
            {
                lblStatus.Text = "Сервер недоступний";
                lblStatus.ForeColor = Color.Red;

                var result = MessageBox.Show(
                    "Сервер поштового сервісу недоступний. Спробувати підключитися знову?",
                    "Сервер недоступний",
                    MessageBoxButtons.RetryCancel,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Retry)
                {
                    await LoadPostingsAsync();
                }
            }
            // Решту непередбачених помилок (наприклад, 500, десеріалізація) НЕ перехоплюємо тут — 
            // вони піднімаються до глобального обробника UnhandledException
            finally
            {
                btnRefresh.Enabled = true;
            }
        }

        private Posting? GetSelectedPosting()
        {
            if (dgvPostings.SelectedRows.Count > 0)
            {
                return dgvPostings.SelectedRows[0].DataBoundItem as Posting;
            }
            return null;
        }

        private async void btnAdd_Click(object sender, EventArgs e)
        {
            using var dlg = new PostingEditForm(_apiClient);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await LoadPostingsAsync();
            }
        }

        private async void btnEdit_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedPosting();
            if (selected == null)
            {
                MessageBox.Show(
                    "Будь ласка, виберіть відправлення зі списку для редагування!",
                    "Увага",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            using var dlg = new PostingEditForm(_apiClient, selected);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await LoadPostingsAsync();
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedPosting();
            if (selected == null)
            {
                MessageBox.Show(
                    "Будь ласка, виберіть відправлення для видалення!",
                    "Увага",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            var confirmResult = MessageBox.Show(
                $"Ви дійсно бажаєте видалити відправлення #{selected.Id} ({selected.Content}) від {selected.From} до {selected.To}?",
                "Підтвердження видалення",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirmResult != DialogResult.Yes)
                return;

            try
            {
                btnDelete.Enabled = false;
                lblStatus.Text = $"Видалення відправлення #{selected.Id}...";

                await _apiClient.DeletePostingAsync(selected.Id);

                MessageBox.Show(
                    "Відправлення успішно видалено!",
                    "Успіх",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                await LoadPostingsAsync();
            }
            // 1. Помилка застарілих даних: запис уже видалив інший користувач (404)
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                MessageBox.Show(
                    "Вибране відправлення вже відсутнє на сервері. Список буде автоматично оновлено.",
                    "Запис відсутній (404)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                await LoadPostingsAsync();
            }
            // 2. Сервер став недоступним у момент запиту (StatusCode == null)
            catch (HttpRequestException ex) when (ex.StatusCode == null)
            {
                var retry = MessageBox.Show(
                    "Сервер недоступний. Спробувати повторити видалення?",
                    "Помилка зв'язку",
                    MessageBoxButtons.RetryCancel,
                    MessageBoxIcon.Warning
                );

                if (retry == DialogResult.Retry)
                {
                    btnDelete_Click(sender, e);
                }
            }
            // Код 500 та інші непередбачені помилки підуть у глобальний обробник
            finally
            {
                btnDelete.Enabled = true;
            }
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            await LoadPostingsAsync();
        }

        private void dgvPostings_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                btnEdit_Click(sender, e);
            }
        }
    }
}
