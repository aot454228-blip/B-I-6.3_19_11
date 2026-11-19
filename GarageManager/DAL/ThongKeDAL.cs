using System.Data;
using Microsoft.Data.SqlClient;

namespace GarageManager.DAL;

public static class ThongKeDAL
{
    public static DataTable TongHop()
    {
        using var cn = DbHelper.CreateConnection();
        using var adapter = new SqlDataAdapter(@"SELECT TrangThai AS [Trạng thái], COUNT(*) AS [Số phiếu],
       SUM(TongTien) AS [Giá trị phiếu (đ)],
       SUM(CASE WHEN TrangThai = N'Đã thanh toán' THEN TongTien ELSE 0 END) AS [Đã thu (đ)]
FROM PhieuSuaChua GROUP BY TrangThai ORDER BY TrangThai", cn);
        var ketQua = new DataTable();
        adapter.Fill(ketQua);
        return ketQua;
    }
}
