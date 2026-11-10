-- DNTULAB PostgreSQL - kiểm tra dữ liệu Demo
SELECT 'Phong' AS "Bang", count(*) AS "SoDong" FROM public."Phong"
UNION ALL SELECT 'ThietBi', count(*) FROM public."ThietBi"
UNION ALL SELECT 'NguoiDung', count(*) FROM public."NguoiDung"
UNION ALL SELECT 'LichDay', count(*) FROM public."LichDay"
UNION ALL SELECT 'TicketSuCo', count(*) FROM public."TicketSuCo"
UNION ALL SELECT 'YeuCauDoiPhong', count(*) FROM public."YeuCauDoiPhong"
ORDER BY 1;

SELECT "MaPhong", "LoaiPhong", "Cum", "SucChua"
FROM public."Phong"
ORDER BY CASE "Cum" WHEN 'F' THEN 1 WHEN 'G' THEN 2 WHEN 'AIBOT' THEN 3 END, "MaPhong";

SELECT "MaThietBi", "MaPhong", "Loai", "Ten", "TrangThai"
FROM public."ThietBi"
WHERE "MaPhong" = 'AIBOT'
ORDER BY "MaThietBi";

SELECT "TenDangNhap", "HoTen", "VaiTro", "HoatDong"
FROM public."NguoiDung"
WHERE "TenDangNhap" IN ('gv01', 'gv02', 'ktv01', 'ql01')
ORDER BY "TenDangNhap";

SELECT nd."TenDangNhap", count(ld."Id") AS "TongCa"
FROM public."NguoiDung" nd
LEFT JOIN public."LichDay" ld ON ld."GiangVienId" = nd."Id"
    AND ld."Ngay" BETWEEN DATE '2026-10-20' AND DATE '2026-11-30'
WHERE nd."TenDangNhap" IN ('gv01', 'gv02')
GROUP BY nd."TenDangNhap"
ORDER BY nd."TenDangNhap";
