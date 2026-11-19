-- =====================================================
-- BÀI 6.3 - ProjectTaskDB (SQL Server)
-- =====================================================
IF DB_ID(N'ProjectTaskDB') IS NULL CREATE DATABASE ProjectTaskDB;
GO
USE ProjectTaskDB;
GO

CREATE TABLE DuAn (
    MaDuAn      INT IDENTITY(1,1) PRIMARY KEY,
    TenDuAn     NVARCHAR(150) NOT NULL,
    NgayBatDau  DATETIME NULL,
    NgayKetThuc DATETIME NULL,
    TrangThai   NVARCHAR(50) NULL,
    CONSTRAINT CK_DuAn_TrangThai CHECK (TrangThai IN (N'Đang chạy', N'Hoàn thành', N'Tạm dừng'))
);

CREATE TABLE ThanhVien (
    MaThanhVien INT IDENTITY(1,1) PRIMARY KEY,
    HoTen       NVARCHAR(100) NOT NULL,
    Email       VARCHAR(100) NULL UNIQUE,
    ChuyenMon   NVARCHAR(100) NULL
);

CREATE TABLE CongViec (
    MaTask    INT IDENTITY(1,1) PRIMARY KEY,
    MaDuAn    INT NOT NULL,
    TenTask   NVARCHAR(200) NOT NULL,
    MoTa      NVARCHAR(MAX) NULL,
    HanNop    DATETIME NULL,
    DoUuTien  NVARCHAR(20) NULL,
    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'To Do',
    CONSTRAINT FK_CongViec_DuAn FOREIGN KEY (MaDuAn) REFERENCES DuAn(MaDuAn) ON DELETE CASCADE,
    CONSTRAINT CK_CongViec_UuTien CHECK (DoUuTien IN (N'Cao', N'Trung bình', N'Thấp')),
    CONSTRAINT CK_CongViec_TrangThai CHECK (TrangThai IN (N'To Do', N'In Progress', N'Review', N'Done'))
);

CREATE TABLE PhanCong (
    MaTask      INT NOT NULL,
    MaThanhVien INT NOT NULL,
    NgayGiao    DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_PhanCong PRIMARY KEY (MaTask, MaThanhVien),
    CONSTRAINT FK_PhanCong_Task FOREIGN KEY (MaTask) REFERENCES CongViec(MaTask) ON DELETE CASCADE,
    CONSTRAINT FK_PhanCong_TV   FOREIGN KEY (MaThanhVien) REFERENCES ThanhVien(MaThanhVien)
);
GO

-- ---------------- Dữ liệu mẫu ----------------
INSERT DuAn (TenDuAn, NgayBatDau, NgayKetThuc, TrangThai) VALUES
 (N'Website bán hàng', '2026-09-01', '2026-12-31', N'Đang chạy'),
 (N'App quản lý kho',  '2026-06-01', '2026-10-30', N'Tạm dừng');

INSERT ThanhVien (HoTen, Email, ChuyenMon) VALUES
 (N'Nguyễn Văn An', 'an@example.com',   N'Backend'),
 (N'Trần Thị Bình', 'binh@example.com', N'Frontend'),
 (N'Lê Minh Châu',  'chau@example.com', N'Tester');

INSERT CongViec (MaDuAn, TenTask, MoTa, HanNop, DoUuTien, TrangThai) VALUES
 (1, N'Thiết kế CSDL',      N'Vẽ ERD, tạo bảng',     '2026-09-15', N'Cao',        N'Done'),
 (1, N'Làm trang chủ',      N'HTML/CSS responsive',  '2026-11-10', N'Trung bình', N'In Progress'),
 (1, N'API đăng nhập',      N'JWT',                  '2026-11-01', N'Cao',        N'In Progress'),
 (1, N'Kiểm thử thanh toán',N'Test cổng thanh toán', '2026-12-20', N'Thấp',       N'To Do'),
 (2, N'Phân tích yêu cầu',  N'Họp khách hàng',       '2026-07-01', N'Cao',        N'Review');

INSERT PhanCong (MaTask, MaThanhVien) VALUES (1,1),(2,2),(3,1),(3,2),(4,3),(5,3);
GO

-- =====================================================
-- BÀI 6.4 - GarageDB (SQL Server)
-- =====================================================
IF DB_ID(N'GarageDB') IS NULL CREATE DATABASE GarageDB;
GO
USE GarageDB;
GO

CREATE TABLE KhachHang (
    MaKH      INT IDENTITY(1,1) PRIMARY KEY,
    TenKH     NVARCHAR(100) NOT NULL,
    DienThoai VARCHAR(15) NULL,
    DiaChi    NVARCHAR(200) NULL
);
-- UNIQUE cho điện thoại nhưng cho phép nhiều dòng NULL (UNIQUE thường chỉ cho 1 NULL)
CREATE UNIQUE INDEX UX_KhachHang_DienThoai ON KhachHang(DienThoai) WHERE DienThoai IS NOT NULL;

CREATE TABLE Xe (
    BienSo     VARCHAR(20) PRIMARY KEY,
    MaKH       INT NOT NULL,
    HieuXe     NVARCHAR(50) NULL,
    NamSanXuat INT NULL,
    CONSTRAINT FK_Xe_KhachHang FOREIGN KEY (MaKH) REFERENCES KhachHang(MaKH)
);

CREATE TABLE PhuTung (
    MaPhuTung  INT IDENTITY(1,1) PRIMARY KEY,
    TenPhuTung NVARCHAR(150) NOT NULL,
    DonGia     DECIMAL(18,2) NOT NULL DEFAULT 0,
    SoLuongTon INT NOT NULL DEFAULT 0,
    CONSTRAINT CK_PhuTung_Ton CHECK (SoLuongTon >= 0),
    CONSTRAINT CK_PhuTung_Gia CHECK (DonGia >= 0)
);

CREATE TABLE PhieuSuaChua (
    MaPhieu    INT IDENTITY(1,1) PRIMARY KEY,
    BienSo     VARCHAR(20) NOT NULL,
    NgayNhan   DATETIME NOT NULL DEFAULT GETDATE(),
    NoiDungSua NVARCHAR(MAX) NULL,
    TongTien   DECIMAL(18,2) NOT NULL DEFAULT 0,
    TrangThai  NVARCHAR(30) NOT NULL DEFAULT N'Đang sửa',
    CONSTRAINT FK_Phieu_Xe FOREIGN KEY (BienSo) REFERENCES Xe(BienSo),
    CONSTRAINT CK_Phieu_TrangThai CHECK (TrangThai IN (N'Đang sửa', N'Đã hoàn thành', N'Đã thanh toán'))
);

CREATE TABLE ChiTietSuaChua (
    MaPhieu    INT NOT NULL,
    MaPhuTung  INT NOT NULL,
    SoLuong    INT NOT NULL,
    TienCong   DECIMAL(18,2) NOT NULL DEFAULT 0,
    CONSTRAINT PK_ChiTiet PRIMARY KEY (MaPhieu, MaPhuTung),
    CONSTRAINT FK_CT_Phieu FOREIGN KEY (MaPhieu) REFERENCES PhieuSuaChua(MaPhieu) ON DELETE CASCADE,
    CONSTRAINT FK_CT_PhuTung FOREIGN KEY (MaPhuTung) REFERENCES PhuTung(MaPhuTung),
    CONSTRAINT CK_CT_SoLuong CHECK (SoLuong > 0),
    CONSTRAINT CK_CT_TienCong CHECK (TienCong >= 0)
);
GO

-- ---------------- Dữ liệu mẫu ----------------
INSERT KhachHang (TenKH, DienThoai, DiaChi) VALUES
 (N'Nguyễn Văn Hùng', '0901234567', N'12 Lê Lợi, Hà Nội'),
 (N'Trần Thị Mai',    '0912345678', N'45 Trần Phú, Đà Nẵng');

INSERT Xe (BienSo, MaKH, HieuXe, NamSanXuat) VALUES
 ('30A-123.45', 1, N'Toyota',  2019),
 ('30G-678.90', 1, N'Honda',   2021),
 ('43A-555.66', 2, N'VinFast', 2023);

-- Tồn kho dưới đây đã trừ sẵn phiếu mẫu số 1 (2 dầu nhớt, 1 lọc gió)
INSERT PhuTung (TenPhuTung, DonGia, SoLuongTon) VALUES
 (N'Dầu nhớt 5W-30 (lít)',  150000, 48),
 (N'Lọc gió động cơ',       200000, 19),
 (N'Má phanh trước',        650000, 10),
 (N'Bugi',                   90000, 40),
 (N'Ắc quy 12V',           1800000,  3);

INSERT PhieuSuaChua (BienSo, NoiDungSua, TongTien, TrangThai)
VALUES ('30A-123.45', N'Bảo dưỡng định kỳ 20.000 km', 650000, N'Đã hoàn thành');
INSERT ChiTietSuaChua (MaPhieu, MaPhuTung, SoLuong, TienCong) VALUES (1, 1, 2, 100000), (1, 2, 1, 50000);
-- 2*150.000 + 1*200.000 + 100.000 + 50.000 = 650.000
GO

-- =====================================================
-- BÀI 6.5 - KaraokeDB (SQL Server)
-- =====================================================
IF DB_ID(N'KaraokeDB') IS NULL CREATE DATABASE KaraokeDB;
GO
USE KaraokeDB;
GO

CREATE TABLE PhongHat (
    MaPhong     INT IDENTITY(1,1) PRIMARY KEY,
    TenPhong    NVARCHAR(50) NOT NULL,
    LoaiPhong   NVARCHAR(30) NULL,
    GiaTheoGio  DECIMAL(18,2) NOT NULL DEFAULT 0,
    TrangThai   NVARCHAR(20) NOT NULL DEFAULT N'Trống',
    CONSTRAINT CK_Phong_TrangThai CHECK (TrangThai IN (N'Trống', N'Đang hát')),
    CONSTRAINT CK_Phong_Gia CHECK (GiaTheoGio >= 0)
);

CREATE TABLE DichVu (
    MaDV   INT IDENTITY(1,1) PRIMARY KEY,
    TenDV  NVARCHAR(100) NOT NULL,
    DonGia DECIMAL(18,2) NOT NULL DEFAULT 0,
    TonKho INT NOT NULL DEFAULT 0,
    CONSTRAINT CK_DV_TonKho CHECK (TonKho >= 0),
    CONSTRAINT CK_DV_Gia CHECK (DonGia >= 0)
);

CREATE TABLE HoaDon (
    MaHD           INT IDENTITY(1,1) PRIMARY KEY,
    MaPhong        INT NOT NULL,
    GioVao         DATETIME NOT NULL DEFAULT GETDATE(),
    GioRa          DATETIME NULL,
    TongTienPhong  DECIMAL(18,2) NOT NULL DEFAULT 0,
    TongTienDichVu DECIMAL(18,2) NOT NULL DEFAULT 0,
    TrangThai      BIT NOT NULL DEFAULT 0,        -- 0: chưa thanh toán, 1: đã thanh toán
    CONSTRAINT FK_HoaDon_Phong FOREIGN KEY (MaPhong) REFERENCES PhongHat(MaPhong)
);
-- Mỗi phòng chỉ có tối đa 1 hóa đơn chưa thanh toán (chặn mở phòng 2 lần)
CREATE UNIQUE INDEX UX_HoaDon_PhongDangMo ON HoaDon(MaPhong) WHERE TrangThai = 0;

CREATE TABLE ChiTietDichVu (
    MaHD    INT NOT NULL,
    MaDV    INT NOT NULL,
    SoLuong INT NOT NULL,
    DonGia  DECIMAL(18,2) NOT NULL,              -- đơn giá tại thời điểm gọi
    CONSTRAINT PK_ChiTietDV PRIMARY KEY (MaHD, MaDV),
    CONSTRAINT FK_CTDV_HoaDon FOREIGN KEY (MaHD) REFERENCES HoaDon(MaHD) ON DELETE CASCADE,
    CONSTRAINT FK_CTDV_DichVu FOREIGN KEY (MaDV) REFERENCES DichVu(MaDV),
    CONSTRAINT CK_CTDV_SoLuong CHECK (SoLuong > 0)
);
GO

-- ---------------- Dữ liệu mẫu ----------------
INSERT PhongHat (TenPhong, LoaiPhong, GiaTheoGio) VALUES
 (N'Phòng VIP 01',     N'VIP',    250000),
 (N'Phòng VIP 02',     N'VIP',    250000),
 (N'Phòng VIP 03',     N'VIP',    250000),
 (N'Phòng Thường 01',  N'Thường', 120000),
 (N'Phòng Thường 02',  N'Thường', 120000),
 (N'Phòng Thường 03',  N'Thường', 120000),
 (N'Phòng Thường 04',  N'Thường', 120000),
 (N'Phòng Thường 05',  N'Thường', 120000);

INSERT DichVu (TenDV, DonGia, TonKho) VALUES
 (N'Bia Heineken',   30000, 120),
 (N'Bia Tiger',      25000, 100),
 (N'Nước suối',      10000, 200),
 (N'Coca-Cola',      15000, 100),
 (N'Trái cây dĩa',  120000,  30),
 (N'Khô bò',         90000,  25),
 (N'Snack',          20000,  80);

-- Phòng VIP 02 đang hát từ 95 phút trước, đã gọi 6 bia Heineken + 1 trái cây dĩa
INSERT HoaDon (MaPhong, GioVao, TrangThai) VALUES (2, DATEADD(MINUTE, -95, GETDATE()), 0);
UPDATE PhongHat SET TrangThai = N'Đang hát' WHERE MaPhong = 2;
INSERT ChiTietDichVu (MaHD, MaDV, SoLuong, DonGia) VALUES (1, 1, 6, 30000), (1, 5, 1, 120000);
GO
