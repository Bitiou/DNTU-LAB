-- DNTULAB PostgreSQL - 4 tài khoản demo
-- Hash được giữ nguyên từ 03_SeedAccounts.sql (ASP.NET Core Identity PasswordHasher V3).
-- Script đặt lại hash của 4 tài khoản demo để bảo đảm đúng mật khẩu bên dưới.
-- gv01 / Gv@123456; gv02 / Gv@123456; ktv01 / Ktv@123456; ql01 / Ql@123456
BEGIN;

INSERT INTO public."NguoiDung"
    ("TenDangNhap", "MatKhauHash", "HoTen", "VaiTro", "HoatDong")
VALUES
('gv01', 'AQAAAAIAAYagAAAAEGuSWbbqd6eD0jAwLW8Yiow7fiSjTB3l4aDNw6S1xHcnmkYtU61gdZspPD+QhTt5MQ==', 'Giảng viên 01', 'GV', TRUE),
('gv02', 'AQAAAAIAAYagAAAAEJwjzBDiRtCeBwifoHrRP0NtkIItcBjG6HSTZCcETG2NpTW3w6mr3s1not9P+FPzJg==', 'Giảng viên 02', 'GV', TRUE),
('ktv01', 'AQAAAAIAAYagAAAAECkq2k/MA5Cni9uET14dhnRaWQmCuhOeYh6wO7nViqttDLVca4EOsfbgWusz/or7sw==', 'Kỹ thuật viên 01', 'KTV', TRUE),
('ql01', 'AQAAAAIAAYagAAAAEGt0PEVLvx3J8+biII/pSLXtSLA7Qd+kEMEQF83PoIeX8kXc7LzGaejCEFCZ2Z3GxQ==', 'Quản lý LAB', 'QL', TRUE)
ON CONFLICT ("TenDangNhap") DO UPDATE SET
    "MatKhauHash" = EXCLUDED."MatKhauHash",
    "HoTen" = EXCLUDED."HoTen",
    "VaiTro" = EXCLUDED."VaiTro",
    "HoatDong" = TRUE;

COMMIT;

SELECT "Id", "TenDangNhap", "HoTen", "VaiTro", "HoatDong"
FROM public."NguoiDung"
WHERE "TenDangNhap" IN ('gv01', 'gv02', 'ktv01', 'ql01')
ORDER BY CASE "TenDangNhap"
    WHEN 'gv01' THEN 1 WHEN 'gv02' THEN 2 WHEN 'ktv01' THEN 3 WHEN 'ql01' THEN 4 END;
