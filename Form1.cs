using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace UserManagementApp
{
    public partial class Form1 : Form
    {
        private readonly HttpClient httpClient;
        private List<Person> people;
        private SimpleHttpServer server;

        public Form1()
        {
            InitializeComponent();

            // Инициализация HttpClient
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:5000/");

            // Инициализация списка пользователей
            people = new List<Person>();

            // Запуск сервера
            server = new SimpleHttpServer();
            server.Start("http://localhost:5000/");
        }

        // Метод для загрузки списка пользователей (Read)
        private async void btnLoad_Click(object sender, EventArgs e)
        {
            await LoadPeople();
        }

        private async Task LoadPeople()
        {
            try
            {
                var response = await httpClient.GetAsync("persons");
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                people = JsonConvert.DeserializeObject<List<Person>>(responseString);

                dataGridView1.DataSource = null;
                dataGridView1.DataSource = people;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке списка пользователей: " + ex.Message);
            }
        }

        // Метод для добавления нового пользователя (Create)
        private async void btnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtSalary.Text))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            if (!decimal.TryParse(txtSalary.Text, out decimal salary))
            {
                MessageBox.Show("Некорректное значение зарплаты.");
                return;
            }

            var person = new Person
            {
                Name = txtName.Text,
                BirthDate = dateTimePicker1.Value,
                Salary = salary
            };

            try
            {
                var json = JsonConvert.SerializeObject(person);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("persons", content);
                response.EnsureSuccessStatusCode();

                MessageBox.Show("Пользователь успешно добавлен.");

                await LoadPeople();
                ClearInputFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении пользователя: " + ex.Message);
            }
        }

        // Метод для обновления выбранного пользователя (Update)
        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выберите пользователя для обновления.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtSalary.Text))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            if (!decimal.TryParse(txtSalary.Text, out decimal salary))
            {
                MessageBox.Show("Некорректное значение зарплаты.");
                return;
            }

            var person = dataGridView1.CurrentRow.DataBoundItem as Person;
            if (person == null)
            {
                MessageBox.Show("Неверный выбор.");
                return;
            }

            person.Name = txtName.Text;
            person.BirthDate = dateTimePicker1.Value;
            person.Salary = salary;

            try
            {
                var json = JsonConvert.SerializeObject(person);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync($"persons/{person.Id}", content);
                response.EnsureSuccessStatusCode();

                MessageBox.Show("Пользователь успешно обновлен.");

                await LoadPeople();
                ClearInputFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении пользователя: " + ex.Message);
            }
        }

        // Метод для удаления выбранного пользователя (Delete)
        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выберите пользователя для удаления.");
                return;
            }

            var person = dataGridView1.CurrentRow.DataBoundItem as Person;
            if (person == null)
            {
                MessageBox.Show("Неверный выбор.");
                return;
            }

            var confirmResult = MessageBox.Show("Вы уверены, что хотите удалить выбранного пользователя?",
                                     "Подтверждение удаления",
                                     MessageBoxButtons.YesNo);

            if (confirmResult == DialogResult.Yes)
            {
                try
                {
                    var response = await httpClient.DeleteAsync($"persons/{person.Id}");
                    response.EnsureSuccessStatusCode();

                    MessageBox.Show("Пользователь успешно удален.");

                    await LoadPeople();
                    ClearInputFields();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении пользователя: " + ex.Message);
                }
            }
        }

        // Метод для обработки изменения выбора в DataGridView
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                return;
            }

            var person = dataGridView1.CurrentRow.DataBoundItem as Person;
            if (person != null)
            {
                txtName.Text = person.Name;
                dateTimePicker1.Value = person.BirthDate;
                txtSalary.Text = person.Salary.ToString();
            }
        }

        // Метод для очистки полей ввода
        private void ClearInputFields()
        {
            txtName.Clear();
            txtSalary.Clear();
            dateTimePicker1.Value = DateTime.Now;
        }

        // Остановка сервера при закрытии формы
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            server.Stop();
            httpClient.Dispose();
        }
    }
}
