using System.Data;
using Microsoft.Data.SqlClient;

namespace KaraokeManager.DAL;

public static class ThongKeDAL
{
    public static DataTable TongHop()
    {
        using var cn = DbHelper.CreateConnection();
        using var adapter = new SqlDataAdapter(@"SELECT p.TenPhong AS [Phòng], COUNT(h.MaHD) AS [Hóa đơn đã thu],
       COALESCE(SUM(h.TongTienPhong),0) AS [Tiền phòng (đ)],
       COALESCE(SUM(h.TongTienDichVu),0) AS [Dịch vụ (đ)],
       COALESCE(SUM(h.TongTienPhong + h.TongTienDichVu),0) AS [Doanh thu (đ)]
FROM PhongHat p LEFT JOIN HoaDon h ON h.MaPhong = p.MaPhong AND h.TrangThai = 1
GROUP BY p.MaPhong, p.TenPhong ORDER BY p.TenPhong", cn);
        var ketQua = new DataTable();
        adapter.Fill(ketQua);
        return ketQua;
    }
}
