using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WindowsInput;
using WindowsInput.Native;


namespace LoLAccountManagerC
{
    public partial class MainWindow : Window
    {
        private Account[] accounts;
        private DataTable data;

        // private string filePath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName, "accounts.json");
        // non compiled version
        private string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "accounts.json");

        private bool isDataGridButtonFocused;
        private int tempIndex;
        private bool isLeagueClientOpen;

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr WindowHandle);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        public MainWindow()
        {
            InitializeComponent();
            checkForLoggedIn();
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            data = LoadJson(filePath);
            if (data != null)
            {
                DataGrid.ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star);
                DataGrid.ItemsSource = data.DefaultView;
            }
            else
            {
                data = new DataTable();
                data.Columns.Add("AccountName", typeof(string));
                data.Columns.Add("Password", typeof(string));
                data.Columns.Add("RiotID", typeof(string));
                DataGrid.ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star);
                DataGrid.ItemsSource = data.DefaultView;
            }

        }

        private DataTable LoadJson(string filePath)
        {
            if (File.Exists(filePath))
            {
                string jsonString = File.ReadAllText(filePath);

                accounts = JsonConvert.DeserializeObject<Account[]>(jsonString);
                DataTable data = new DataTable();

                data.Columns.Add("AccountName", typeof(string));
                data.Columns.Add("Password", typeof(string));
                data.Columns.Add("RiotID", typeof(string));

                foreach (var account in accounts)
                {
                    DataRow row = data.NewRow();
                    row["AccountName"] = account.AccountName;
                    row["Password"] = new string('*', account.Password.Length);
                    row["RiotID"] = account.RiotID;
                    data.Rows.Add(row);
                }

                return data;
            }
            else
            {
                File.WriteAllText(filePath, "[{}]");
                DataTable data = new DataTable();

                data.Columns.Add("AccountName", typeof(string));
                data.Columns.Add("Password", typeof(string));
                data.Columns.Add("RiotID", typeof(string));

                return data;
            }

        }

        private void TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            bool checkFilled = !String.IsNullOrWhiteSpace(AccountNameInput.Text) &&
                               !String.IsNullOrWhiteSpace(PasswordInput.Text) &&
                               !String.IsNullOrWhiteSpace(RiotIDInput.Text);
            EnterBtn.IsEnabled = checkFilled;
        }
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }



        private void EnterBtn_Click(object sender, RoutedEventArgs e)
        {
            string tempAccName = AccountNameInput.Text;
            string tempPassword = PasswordInput.Text;
            string tempRiotID = RiotIDInput.Text;
            Account tempAccount = new Account(tempAccName, tempPassword, tempRiotID);

            //Write to visual array
            if (accounts != null)
            {
                Array.Resize(ref accounts, accounts.Length + 1);
                accounts[accounts.Length - 1] = tempAccount;
                data.Rows.Clear();
                foreach (var account in accounts)
                {
                    DataRow row = data.NewRow();
                    row["AccountName"] = account.AccountName;
                    row["Password"] = new string('*', account.Password.Length);
                    row["RiotID"] = account.RiotID;
                    data.Rows.Add(row);
                }



                DataGrid.ItemsSource = data.DefaultView;

                string updateJson = JsonConvert.SerializeObject(accounts, Formatting.Indented);
                File.WriteAllText(filePath, updateJson);
            }
            else
            {
                accounts = new Account[] { tempAccount };

                foreach (var account in accounts)
                {
                    DataRow row = data.NewRow();
                    row["AccountName"] = account.AccountName;
                    row["Password"] = new string('*', account.Password.Length);
                    row["RiotID"] = account.RiotID;
                    data.Rows.Add(row);
                }


                DataGrid.ItemsSource = data.DefaultView;

                string updateJson = JsonConvert.SerializeObject(accounts, Formatting.Indented);
                File.WriteAllText(filePath, updateJson);
            }

            AccountNameInput.Text = "";
            PasswordInput.Text = "";
            RiotIDInput.Text = "";

        }
        private void focusLeagueClient()
        {

            string processName = "LeagueClientUx";
            Process[] processes = Process.GetProcessesByName(processName);

            IntPtr mainWindowHandle = processes[0].MainWindowHandle;
            SetForegroundWindow(mainWindowHandle);


        }

        private void focusRiotClient()
        {

            string processName = "Riot Client";
            Process[] processes = Process.GetProcessesByName(processName);

            IntPtr mainWindowHandle = IntPtr.Zero;
            if (processes.Length > 0)
            {
                foreach (var process in processes)
                {
                    IntPtr tempHandle = process.MainWindowHandle;
                    if (tempHandle != IntPtr.Zero)
                    {
                        mainWindowHandle = tempHandle;
                    }
                }

            }
            SetForegroundWindow(mainWindowHandle);


        }
        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            DataGrid.SelectedIndex = tempIndex;
            DataRowView rowView = (DataRowView)DataGrid.SelectedItem;
            DataRow deleteRow = rowView.Row;
            string targetAccount = deleteRow["AccountName"].ToString();
            Account targetAccountObj = accounts.FirstOrDefault(a => a.AccountName == targetAccount);
            //Create list juggle to easily remove account
            List<Account> updatedAccounts = accounts.ToList();
            updatedAccounts.Remove(targetAccountObj);
            accounts = updatedAccounts.ToArray();
            string updateJson = JsonConvert.SerializeObject(accounts, Formatting.Indented);
            File.WriteAllText(filePath, updateJson);

            data.Rows.Remove(deleteRow);
            DataGrid.ItemsSource = data.DefaultView;

        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGrid.SelectedItem != null)
            {
                DeleteBtn.IsEnabled = true;
                LoginBtn.IsEnabled = true;
            }
            else
            {
                DeleteBtn.IsEnabled = false;
                LoginBtn.IsEnabled = false;
            }

        }

        private void DataGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            tempIndex = DataGrid.SelectedIndex; //Maintains genuine selection, while stopping tainting and visual confusion with cells from leaving
            DataGrid.SelectedIndex = -1;
            DeleteBtn.IsEnabled = false;
            LoginBtn.IsEnabled = false;
            if (isDataGridButtonFocused && tempIndex != -1)
            {
                DeleteBtn.IsEnabled = true;
                LoginBtn.IsEnabled = true;
            }
            else
            {
                tempIndex = -1;
            }
        }

        private void DeleteBtn_MouseEnter(object sender, MouseEventArgs e)
        {
            isDataGridButtonFocused = true;
        }

        private void DeleteBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            isDataGridButtonFocused = false;
        }

        private void LoginBtn_MouseEnter(object sender, MouseEventArgs e)
        {
            isDataGridButtonFocused = true;
        }

        private void LoginBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            isDataGridButtonFocused = false;
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            InputSimulator inputSimulator = new InputSimulator();

            Account selectedAccount = null;
            DataGrid.SelectedIndex = tempIndex;
            DataRowView rowView = (DataRowView)DataGrid.SelectedItem;
            DataRow selectRow = rowView.Row;
            foreach (var account in accounts)
            {
                if (account.AccountName == selectRow["AccountName"].ToString())
                {
                    selectedAccount = account;
                }
            }
            string accountNameLogin = selectedAccount.AccountName;
            string passwordLogin = selectedAccount.Password;

            focusRiotClient();
            Thread.Sleep(10);
            IntPtr handle = GetForegroundWindow();
            GetWindowRect(handle, out RECT rect);

            ClickAt(rect.Right - 60, rect.Top + 200); //Defocus any text controls
            for (int i = 0; i < 10; i++)
            {
                inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);

            }
            ClickAt(rect.Right - 60, rect.Top + 200);

            inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            for (int i = 0; i < accountNameLogin.Length; i++)
            {
                inputSimulator.Keyboard.TextEntry(accountNameLogin[i]);

            }
            inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            for (int i = 0; i < passwordLogin.Length; i++)
            {
                inputSimulator.Keyboard.TextEntry(passwordLogin[i]);
            }

            inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
        }

        static void ClickAt(int x, int y)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            focusLeagueClient();
            Thread.Sleep(100);
            IntPtr handle = GetForegroundWindow();
            int width = 0;
            if (GetWindowRect(handle, out RECT rect))
            {
                width = rect.Right - rect.Left;
            }

            Console.WriteLine(rect.Right);
            Console.WriteLine(rect.Left);
            Console.WriteLine(rect.Top);
            Console.WriteLine(rect.Bottom);
            switch (width)
            {
                case 1600:
                    ClickAt(rect.Right - 25, rect.Top + 17);
                    Thread.Sleep(50);
                    ClickAt(rect.Right - 752, rect.Top + 501);
                    break;
                case 1280:
                    ClickAt(rect.Right - 25, rect.Top + 17);
                    Thread.Sleep(50);
                    ClickAt(rect.Right - 617, rect.Top + 398);
                    break;
                case 1024:
                    ClickAt(rect.Right - 17, rect.Top + 12);
                    Thread.Sleep(50);
                    ClickAt(rect.Right - 487, rect.Top + 326);
                    break;
                default:
                    break;
            }
        }

        static async Task<bool> IsProcessRunningAsync(string processName)
        {
            return await Task.Run(() =>
            {
                Process[] processes = Process.GetProcessesByName(processName);
                return processes.Length > 0;
            });
        }

        private async void checkForLoggedIn()
        {
            string processName = "LeagueClientUx";
            TimeSpan interval = TimeSpan.FromSeconds(3);
            while (true)
            {
                bool isRunning = await IsProcessRunningAsync(processName);
                Dispatcher.Invoke(() =>
                {
                    LogoutBtn.IsEnabled = isRunning;
                    if (isDataGridButtonFocused) { LoginBtn.IsEnabled = !isRunning; }
                    isLeagueClientOpen = isRunning;
                });
                await Task.Delay(interval);
            }
        }


    }
}