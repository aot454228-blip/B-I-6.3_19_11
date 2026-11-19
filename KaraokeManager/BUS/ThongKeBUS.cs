using System.Data;
using KaraokeManager.DAL;

namespace KaraokeManager.BUS;

public static class ThongKeBUS
{
    public static DataTable TongHop() => ThongKeDAL.TongHop();
}
