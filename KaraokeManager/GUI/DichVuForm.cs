using System;
using System.Drawing;
using System.Windows.Forms;
using KaraokeManager.BUS;
using KaraokeManager.DTO;

namespace KaraokeManager.GUI;

public class DichVuForm : Form
{
    private readonly DichVuBUS bus = new DichVuBUS();
    private readonly TextBox ten = new TextBox { Width = 210, MaxLength = 100 };
    private readonly NumericUpDown gia = new NumericUpDown { Maximum = 9999999999999999.99m, DecimalPlaces = 2, Width = 155, ThousandsSeparator = true };
    private readonly NumericUpDown ton = new NumericUpDown { Maximum = int.MaxValue, Width = 100 };
    private readonly DataGridView grid = new DataGridView
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect
    };

    public DichVuForm()
    {
        Text = "Danh mục dịch vụ karaoke";
        Size = new Size(860, 510);
        StartPosition = FormStartPosition.CenterParent;
        Controls.Add(grid);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(6) };
        void Button(string title, Action action)
        {
            var button = new Button { Text = title, AutoSize = true };
            button.Click += (s, e) => Run(action);
            actions.Controls.Add(button);
        }
        Button("Thêm", () => { bus.Luu(Doc(), null); Tai(); });
        Button("Sửa", () => { bus.Luu(Doc(), Chon()); Tai(); });
        Button("Xóa", () =>
        {
            var row = Chon();
            if (MessageBox.Show(this, "Xóa dịch vụ " + row.TenDV + "?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
            { bus.Xoa(row); Tai(); }
        });
        Button("Tải lại", Tai);
        Controls.Add(actions);
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Padding = new Padding(6), AutoSize = true };
        foreach (var (label, editor) in new (string, Control)[] { ("Tên dịch vụ", ten), ("Đơn giá (đ)", gia), ("Tồn kho", ton) })
        {
            bar.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(6, 7, 2, 0) });
            bar.Controls.Add(editor);
        }
        Controls.Add(bar);
        grid.SelectionChanged += (s, e) =>
        {
            if (grid.CurrentRow?.DataBoundItem is DichVuDTO item)
            { ten.Text = item.TenDV; gia.Value = item.DonGia; ton.Value = item.TonKho; }
        };
        Load += (s,e) => Run(Tai);
    }
    private void Run(Action action)
    {
        try { action(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Danh mục dịch vụ"); }
    }
    private DichVuDTO Chon() => grid.CurrentRow?.DataBoundItem as DichVuDTO ?? throw new InvalidOperationException("Hãy chọn dịch vụ.");
    private DichVuDTO Doc() => new DichVuDTO { TenDV = ten.Text, DonGia = gia.Value, TonKho = (int)ton.Value };
    private void Tai()
    {
        grid.DataSource = bus.GetAll();
        grid.Columns["MaDV"].HeaderText = "Mã";
        grid.Columns["TenDV"].HeaderText = "Tên dịch vụ";
        grid.Columns["DonGia"].HeaderText = "Đơn giá (đ)";
        grid.Columns["TonKho"].HeaderText = "Tồn kho";
    }
}
