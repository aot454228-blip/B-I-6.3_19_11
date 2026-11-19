using System;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Project = ProjectTaskManager.GUI;
using Garage = GarageManager.GUI;
using Karaoke = KaraokeManager.GUI;

internal static class Program
{
    // Chạy trong thư mục gốc repo. LocalDB riêng được tạo và xóa bởi ChupAnh.ps1.
    private static readonly string[] Files = { "", "01_main_screen", "02_crud_screen", "03_transaction_screen", "04_report_screen", "05_error_handling" };
    private static string folder;
    private static readonly string output = Path.Combine(Path.GetTempPath(), "WinFormsPhotos-" + Guid.NewGuid().ToString("N"));

    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
    private delegate bool Visit(IntPtr w, IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint id, Visit visit, IntPtr data);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr h, StringBuilder text, int size);
    [DllImport("user32.dll")] private static extern IntPtr GetDlgItem(IntPtr h, int id);
    private static string expectedDialog;

    private static IntPtr DialogWindow(uint thread)
    {
        IntPtr result = IntPtr.Zero;
        EnumThreadWindows(thread, (w, unused) => {
            var cls = new StringBuilder(256);
            GetClassName(w, cls, cls.Capacity);
            if (cls.ToString() != "#32770") return true;
            result = w;
            return false;
        }, IntPtr.Zero);
        return result;
    }

    private static string Caption(IntPtr w)
    {
        var text = new StringBuilder(2048);
        GetWindowText(w, text, text.Capacity);
        return text.ToString();
    }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RectangleEdges rect);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int size);
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)]
    private struct RectangleEdges { public int Left, Top, Right, Bottom; }

    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length != 1 || !Regex.IsMatch(args[0], "^BaiTH_[0-9a-f]{32}$")) throw new ArgumentException("Chạy ChupAnh.ps1 từ thư mục gốc.");
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        using var timeout = new System.Threading.Timer(_ => {
            Console.Error.WriteLine("Screenshot run timed out after 90 seconds.");
            Environment.Exit(2);
        }, null, 90000, System.Threading.Timeout.Infinite);
        uint thread = GetCurrentThreadId();
        using var watchdog = new System.Windows.Forms.Timer { Interval = 2000 };
        watchdog.Tick += (s,e) => {
            var window = DialogWindow(thread);
            if (window != IntPtr.Zero && Caption(window) != expectedDialog)
                Environment.FailFast("Unexpected dialog: " + Caption(window) + " — " + Caption(GetDlgItem(window, 65535)));
        };

        Console.WriteLine("Initializing screenshot databases.");
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        watchdog.Start();
        string Connection(string db) => new SqlConnectionStringBuilder
        {
            DataSource = @"(localdb)\" + args[0], InitialCatalog = db == "master" ? db : args[0] + "_" + db, IntegratedSecurity = true, TrustServerCertificate = true
        }.ConnectionString;
        using (var cn = new SqlConnection(Connection("master")))
        {
            cn.Open();
            string script = File.ReadAllText("Database/ScriptDB.sql");
            foreach (var name in new[] { "ProjectTaskDB", "GarageDB", "KaraokeDB" })
                script = script.Replace(name, args[0] + "_" + name);
            foreach (var batch in Regex.Split(script, @"(?im)^\s*GO\s*$"))
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;
                using var command = new SqlCommand(batch, cn);
                Console.WriteLine("SQL batch " + batch.TrimStart().Split('\n')[0]);
                command.ExecuteNonQuery();
            }
        }
        ProjectTaskManager.DAL.DbHelper.ConnectionString = Connection("ProjectTaskDB");
        GarageManager.DAL.DbHelper.ConnectionString = Connection("GarageDB");
        KaraokeManager.DAL.DbHelper.ConnectionString = Connection("KaraokeDB");
        try
        {
            Projects();
            Repairs();
            Rooms();
            foreach (string file in Directory.GetFiles(output, "*.png", SearchOption.AllDirectories))
            {
                var dest = Path.Combine("images", Path.GetRelativePath(output, file));
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                File.Copy(file, dest, true);
            }
        }
        finally
        {
            if (Directory.Exists(output)) Directory.Delete(output, true);
            SqlConnection.ClearAllPools();
            using var master = new SqlConnection(Connection("master"));
            master.Open();
            foreach (string db in new[] { "ProjectTaskDB", "GarageDB", "KaraokeDB" })
            {
                string name = args[0] + "_" + db;
                using var cleanup = new SqlCommand($"DROP DATABASE IF EXISTS [{name}]", master);
                cleanup.ExecuteNonQuery();
            }
        }
    }

    private static void Start(Form form, string project)
    {
        folder = Path.Combine(output, project);
        Directory.CreateDirectory(folder);
        Console.WriteLine("Opening " + project);
        form.Show();
        form.Activate();
        Application.DoEvents();
    }

    private static T Field<T>(Form f, string name) => (T)f.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(f);
    private static void Tab(Form f, int index)
    {
        Field<TabControl>(f, "tabs").SelectedIndex = index;
        Application.DoEvents();
    }
    private static void Press(Control root, string title)
    {
        Button Find(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Button b && b.Text == title) return b;
                var nested = Find(c);
                if (nested != null) return nested;
            }
            return null;
        }
        var button = Find(root) ?? throw new InvalidOperationException("Thiếu nút " + title);
        if (!button.Enabled) throw new InvalidOperationException("Nút chưa sẵn sàng: " + title);
        Console.WriteLine("Click: " + title);
        button.PerformClick();
    }

    private static void Save(IntPtr window, int n)
    {
        if (!GetWindowRect(window, out var r)) throw new InvalidOperationException("Không đọc được cửa sổ.");
        Console.WriteLine("Drawing " + Files[n]);
        using var bitmap = new Bitmap(r.Right - r.Left, r.Bottom - r.Top);
        using (var g = Graphics.FromImage(bitmap))
        {
            var dc = g.GetHdc();
            try { if (!PrintWindow(window, dc, 2)) throw new InvalidOperationException("Windows chưa vẽ được cửa sổ."); }
            finally { g.ReleaseHdc(dc); }
        }
        var colors = new System.Collections.Generic.HashSet<int>();
        for (int y = 0; y < bitmap.Height; y += 4)
            for (int x = 0; x < bitmap.Width; x += 4)
                colors.Add(bitmap.GetPixel(x, y).ToArgb());
        if (colors.Count < 8) throw new InvalidOperationException("Ảnh cửa sổ trống: " + Files[n]);
        Console.WriteLine("Saving " + folder + "/" + Files[n]);
        bitmap.Save(Path.Combine(folder, Files[n] + ".png"), ImageFormat.Png);
    }

    private static void Save(Form f, int n)
    {
        f.Update();
        Application.DoEvents();
        Save(f.Handle, n);
    }

    private static void RefreshView(Form form, string method) =>
        form.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, null);

    private static void SelectName(DataGridView grid, string column, string name)
    {
        var row = grid.Rows.Cast<DataGridViewRow>().Single(r => Equals(r.Cells[column].Value, name));
        grid.CurrentCell = row.Cells[column];
    }

    private static void Projects()
    {
        using var f = new Project.FormMain();
        Start(f, "ProjectTaskManager");
        Tab(f, 2); Save(f, 1);
        Tab(f, 0); Save(f, 2);
        Tab(f, 1);
        var members = Field<CheckedListBox>(f, "clbThanhVien");
        members.SetItemChecked(members.Items.Count - 1, true);
        var task = (ProjectTaskManager.DTO.CongViecDTO)Field<DataGridView>(f,"dgvTask").CurrentRow.DataBoundItem;
        var member = (ProjectTaskManager.DTO.ThanhVienDTO)members.Items[members.Items.Count-1];
        new ProjectTaskManager.BUS.PhanCongBUS().GiaoViec(task.MaTask, new[] { member.MaThanhVien });
        RefreshView(f, "TickThanhVienCuaTask");
        Save(f, 3);
        Tab(f, 3); Save(f, 4);
        Tab(f, 0);
        const string name = "Dự án O'Brien";
        Field<TextBox>(f, "txtTenDuAn").Text = name;
        Press(f, "Thêm");
        var grid = Field<DataGridView>(f, "dgvDuAn");
        var row = grid.Rows.Cast<DataGridViewRow>().Single(r => ((ProjectTaskManager.DTO.DuAnDTO)r.DataBoundItem).TenDuAn == name);
        grid.CurrentCell = row.Cells["TenDuAn"];
        Save(f, 5);
    }

    private static void Repairs()
    {
        using var f = new Garage.FormMain();
        Start(f, "GarageManager");
        Save(f, 1);
        Tab(f, 1); Save(f, 2);
        Tab(f, 2);
        Field<TextBox>(f, "txtBSPhieu").Text = "30A-123.45";
        Field<TextBox>(f, "txtNoiDung").Text = "Kiểm tra và thay phụ tùng";
        Press(f, "Kiểm tra xe");
        Press(f, "Thêm vào phiếu");
        Save(f, 3);
        const string part = "Lọc gió O'Brien";
        new GarageManager.BUS.PhuTungBUS().Them(new GarageManager.DTO.PhuTungDTO { TenPhuTung = part, DonGia = 120000, SoLuongTon = 8 });
        Tab(f, 1);
        RefreshView(f, "LoadPhuTung");
        SelectName(Field<DataGridView>(f, "dgvPT"), "TenPhuTung", part);
        Save(f, 5);
        new GarageManager.BUS.PhieuSuaChuaBUS().ThanhToan(1);
        using var report = new Garage.BaoCaoForm();
        report.Show(f);
        Save(report, 4);
    }

    private static void Rooms()
    {
        using var f = new Karaoke.FormMain();
        Start(f, "KaraokeManager");
        Save(f, 1);
        using (var catalog = new Karaoke.DichVuForm())
        {
            catalog.Show(f);
            Save(catalog, 2);
        }
        var rooms = Field<FlowLayoutPanel>(f, "pnlPhong");
        rooms.Controls.OfType<Button>().Single(b => ((KaraokeManager.DTO.PhongDTO)b.Tag).MaPhong == 2).PerformClick();
        const string service = "Trà O'Brien";
        new KaraokeManager.BUS.DichVuBUS().Luu(new KaraokeManager.DTO.DichVuDTO { TenDV = service, DonGia = 30000, TonKho = 10 }, null);
        using (var safe = new Karaoke.DichVuForm())
        {
            safe.Show(f);
            SelectName(Field<DataGridView>(safe, "grid"), "TenDV", service);
            Save(safe, 5);
        }
        Field<NumericUpDown>(f, "nudSL").Value = 1;
        Press(f, "+ Thêm");
        Save(f, 3);
        new KaraokeManager.BUS.HoaDonBUS().ThanhToan(1);
        using var report = new Karaoke.BaoCaoForm();
        report.Show(f);
        Save(report, 4);
    }
}
