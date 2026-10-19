using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using PostServiceFormsApp.Models;
using PostServiceFormsApp.Services;

namespace PostServiceFormsApp.Forms
{
    public partial class PostingEditForm : Form
    {
        private readonly IPostServiceClient _apiClient;
        public Posting Model { get; }

        public PostingEditForm(IPostServiceClient apiClient, Posting? existing = null)
        {
            InitializeComponent();
            _apiClient = apiClient ?? new PostServiceClient();
            Model = existing ?? new Posting();

            Text = Model.Id > 0 ? $"Редагування відправлення #{Model.Id}" : "Нове поштове відправлення";
            lblTitle.Text = Model.Id > 0 ? $"Редагування запису #{Model.Id}" : "Нове поштове відправлення";
        }

        private void PostingEditForm_Load(object sender, EventArgs e)
        {
            cmbDeliveryType.Items.Clear();
            cmbDeliveryType.Items.Add("Стандартна (у відділення)");
            cmbDeliveryType.Items.Add("Кур'єрська доставка");
            cmbDeliveryType.SelectedIndex = Model.DeliveryType == 1 ? 1 : 0;

            txtFrom.Text = Model.From;
            txtTo.Text = Model.To;
            txtContent.Text = Model.Content;
            numWeight.Value = Model.Weight > 0 ? Model.Weight : 1.0m;
            numLength.Value = Model.Depth > 0 ? Model.Depth : 15.0m;
            numWidth.Value = Model.Width > 0 ? Model.Width : 20.0m;
            numHeight.Value = Model.Height > 0 ? Model.Height : 10.0m;
            numValue.Value = Model.Value >= 0 ? Model.Value : 100.0m;
            numPrice.Value = Model.Price >= 0 ? Model.Price : 65.0m;
        }

        private Posting BuildPostingFromInputs()
        {
            return new Posting
            {
                Id = Model.Id,
                From = txtFrom.Text.Trim(),
                To = txtTo.Text.Trim(),
                Content = txtContent.Text.Trim(),
                DeliveryType = cmbDeliveryType.SelectedIndex,
                Weight = numWeight.Value,
                Depth = numLength.Value,
                Width = numWidth.Value,
                Height = numHeight.Value,
                Value = numValue.Value,
                Price = numPrice.Value
            };
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            var posting = BuildPostingFromInputs();

            try
            {
                // Блокуємо кнопку збереження, щоб уникнути повторного кліку
                btnSave.Enabled = false;

                if (Model.Id == 0)
                {
                    await _apiClient.CreatePostingAsync(posting);
                }
                else
                {
                    await _apiClient.UpdatePostingAsync(Model.Id, posting);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            // 1. Помилка валідації на стороні сервера (код 400)
            catch (PostingValidationException ex)
            {
                var messages = new List<string>();
                foreach (var field in ex.Errors)
                {
                    messages.AddRange(field.Value);
                }

                // Показуємо зрозумілі підказки та залишаємо форму ВІДКРИТОЮ для виправлення
                MessageBox.Show(
                    string.Join(Environment.NewLine, messages),
                    "Перевірте введені дані (Код 400)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
            // 2. Помилка застарілих даних під час редагування (код 404)
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                MessageBox.Show(
                    "Відправлення, яке ви редагуєте, уже видалено на сервері іншим користувачем.",
                    "Запис не знайдено (Код 404)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                DialogResult = DialogResult.Cancel;
                Close();
            }
            // 3. Сервер недоступний або зник зв'язок (StatusCode == null)
            catch (HttpRequestException ex) when (ex.StatusCode == null)
            {
                var retry = MessageBox.Show(
                    "Сервер недоступний. Спробувати повторити збереження?",
                    "Сервер недоступний",
                    MessageBoxButtons.RetryCancel,
                    MessageBoxIcon.Warning
                );

                if (retry == DialogResult.Retry)
                {
                    btnSave_Click(sender, e);
                }
            }
            // Усі інші непередбачені винятки (код 500, JSON помилки) НЕ «ковтаємо» —
            // вони потрапляють у глобальний обробник UnhandledException
            finally
            {
                // Обов'язково розблоковуємо кнопку у блоці finally, щоб форма не зависала
                btnSave.Enabled = true;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
