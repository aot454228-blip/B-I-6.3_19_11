using System.Data;
using GarageManager.DAL;

namespace GarageManager.BUS;

public static class ThongKeBUS
{
    public static DataTable TongHop() => ThongKeDAL.TongHop();
}
