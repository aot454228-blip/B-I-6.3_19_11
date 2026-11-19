using System.Drawing;
using System.Windows.Forms;
using KaraokeManager.BUS;

namespace KaraokeManager.GUI;

public class BaoCaoForm : Form
{
    private readonly DataGridView bang = new DataGridView
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
        RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };

    public BaoCaoForm()
    {
        Text = "Báo cáo doanh thu theo phòng";
        Size = new Size(900, 460);
        StartPosition = FormStartPosition.CenterParent;
        Controls.Add(bang);
        var refresh = new Button { Text = "Làm mới", Dock = DockStyle.Top, Height = 36 };
        Controls.Add(refresh);
        refresh.Click += (s, e) => Tai();
        Load += (s, e) => Tai();
    }

    private void Tai()
    {
        try
        {
            bang.DataSource = ThongKeBUS.TongHop();
            foreach (DataGridViewColumn col in bang.Columns)
                if (col.ValueType == typeof(decimal)) col.DefaultCellStyle.Format = "N0";
        }
        catch (System.Exception ex) { MessageBox.Show(this, ex.Message, "Lỗi tải báo cáo"); }
    }
}
