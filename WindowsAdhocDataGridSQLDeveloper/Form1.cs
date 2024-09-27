using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace WindowsAdhocDataGridSQLDeveloper
{
    public partial class Form1 : Form
    {
        public static string connectionString = "Server=localhost;Port=3306;Database=tcs_dpdc_mw_prod;Uid=deepak;Pwd=821216;charset=utf8;";

        private List<object[]> data = new List<object[]>();
        private int totalRows = 0;
        private int loadedRows = 0;
        private const int batchSize = 50; // Number of rows to load at a time

        private bool isLoading = false; // Flag to prevent multiple loads
        private System.Timers.Timer scrollTimer; // Timer for debouncing scroll

        public Form1()
        {
            InitializeComponent();

            this.dataGridView1.VirtualMode = true;
            this.dataGridView1.CellValueNeeded += DataGridView1_CellValueNeeded;
            this.dataGridView1.Scroll += DataGridView1_Scroll; // Handle Scroll event



            //this.dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToOrderColumns = false;
            this.dataGridView1.SelectionMode = DataGridViewSelectionMode.RowHeaderSelect;

           // this.dataGridView1.SetRedraw(false);
            this.dataGridView1.SetDoubleBuffering(true);



            // Initialize the scroll timer
            scrollTimer = new System.Timers.Timer(100); // Set debounce time (in milliseconds)
            scrollTimer.Elapsed += OnScrollTimerElapsed;
            scrollTimer.AutoReset = false; // Run only once

            // LoadDataAsync(); // Initial load
        }

        private async void LoadDataAsync()
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    await conn.OpenAsync();

                    // Query to retrieve data
                    string query = textBox1.Text.Trim();
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        totalRows = 0;
                        data.Clear();
                        dataGridView1.Columns.Clear();

                        // Create columns in the DataGridView
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            dataGridView1.Columns.Add(reader.GetName(i), reader.GetName(i));
                        }

                        // Read data and store in the list
                        while (await reader.ReadAsync())
                        {
                            object[] rowData = new object[reader.FieldCount];
                            reader.GetValues(rowData);
                            data.Add(rowData);
                            totalRows++;
                        }

                        // Set the initial row count for the DataGridView
                        loadedRows = Math.Min(batchSize, totalRows);
                        dataGridView1.RowCount = loadedRows+1;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private void DataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < data.Count)
            {
                e.Value = data[e.RowIndex][e.ColumnIndex];
            }
        }
      
        /*
        private async void DataGridView1_Scroll(object sender, ScrollEventArgs e)
        {
            // Check if we are close to the bottom of the currently loaded rows
            if (dataGridView1.FirstDisplayedScrollingRowIndex + dataGridView1.DisplayedRowCount(false) >= loadedRows - 5)
            {
                if (loadedRows < totalRows)
                {
                    int nextBatchSize = Math.Min(batchSize, totalRows - loadedRows);
                    await LoadMoreDataAsync(loadedRows, nextBatchSize);
                }
            }
        }
        */

        private void DataGridView1_Scroll(object sender, ScrollEventArgs e)
        {
            // Restart the timer on scroll event
            if (!isLoading && loadedRows < totalRows)
            {
                scrollTimer.Stop(); // Stop any previous timer
                scrollTimer.Start(); // Start the timer for debouncing
            }
        }

        private async void OnScrollTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Check if we are close to the bottom of the currently loaded rows
            if (dataGridView1.FirstDisplayedScrollingRowIndex + dataGridView1.DisplayedRowCount(false) >= loadedRows - 5)
            {
                isLoading = true; // Set loading flag

                int nextBatchSize = Math.Min(batchSize, totalRows - loadedRows);
                await LoadMoreDataAsync(loadedRows, nextBatchSize);

                isLoading = false; // Reset loading flag
            }
        }


        private async Task LoadMoreDataAsync(int startRow, int numberOfRows)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    await conn.OpenAsync();

                    string query = textBox1.Text.Trim(); // Reuse the same query
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        // Skip the rows that have already been loaded
                        for (int i = 0; i < startRow; i++)
                        {
                            await reader.ReadAsync();
                        }

                        // Read next batch of data
                        for (int i = 0; i < numberOfRows; i++)
                        {
                            if (await reader.ReadAsync())
                            {
                                object[] rowData = new object[reader.FieldCount];
                                reader.GetValues(rowData);
                                data.Add(rowData);
                            }
                        }

                        loadedRows += numberOfRows;

                        // Update DataGridView row count
                        //dataGridView1.RowCount = loadedRows;

                        // Update DataGridView row count on the UI thread
                        this.Invoke((MethodInvoker)delegate {
                            dataGridView1.RowCount = loadedRows;
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            LoadDataAsync(); // Reload data on button click
        }
    }
}
