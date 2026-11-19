using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using KaraokeManager.DTO;
using static KaraokeManager.DAL.DbHelper;

namespace KaraokeManager.DAL
{
    public class DichVuDAL
    {
        public void Ghi(DichVuDTO moi, DichVuDTO cu)
        {
            using var cn = CreateConnection();
            string compare = " WHERE MaDV=@id AND TenDV=@oldName AND DonGia=@oldPrice AND TonKho=@oldStock";
            string sql = cu == null ? "INSERT DichVu(TenDV,DonGia,TonKho) VALUES(@name,@price,@stock)"
                : moi == null ? "DELETE FROM DichVu" + compare
                : "UPDATE DichVu SET TenDV=@name, DonGia=@price, TonKho=@stock" + compare;
            using var cmd = new SqlCommand(sql, cn);
            if (moi != null)
            {
                cmd.Parameters.Add(P("@name", moi.TenDV));
                cmd.Parameters.Add(P("@price", moi.DonGia));
                cmd.Parameters.Add(P("@stock", moi.TonKho));
            }
            if (cu != null)
            {
                cmd.Parameters.Add(P("@id", cu.MaDV));
                cmd.Parameters.Add(P("@oldName", cu.TenDV));
                cmd.Parameters.Add(P("@oldPrice", cu.DonGia));
                cmd.Parameters.Add(P("@oldStock", cu.TonKho));
            }
            cn.Open();
            try
            {
                if (cmd.ExecuteNonQuery() != 1)
                    throw new System.InvalidOperationException("Dữ liệu dịch vụ đã thay đổi. Hãy tải lại danh mục.");
            }
            catch (SqlException ex) when (moi == null && ex.Number == 547)
            {
                throw new System.InvalidOperationException("Dịch vụ đã có trong hóa đơn nên không thể xóa.", ex);
            }
        }

        public List<DichVuDTO> GetAll()
        {
            var list = new List<DichVuDTO>();
            using var cn = CreateConnection();
            using var cmd = new SqlCommand("SELECT MaDV, TenDV, DonGia, TonKho FROM DichVu ORDER BY TenDV", cn);
            cn.Open();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                list.Add(new DichVuDTO
                {
                    MaDV = rd.GetInt32(0), TenDV = rd.GetString(1),
                    DonGia = rd.GetDecimal(2), TonKho = rd.GetInt32(3)
                });
            return list;
        }
    }
}
