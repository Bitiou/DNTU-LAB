-- DNTULAB PostgreSQL - Ticket sự cố và yêu cầu đổi phòng mẫu
-- Chạy sau 02_SeedData_PostgreSQL.sql, 03_SeedAccounts_PostgreSQL.sql,
-- và 04_SeedLichDay_PostgreSQL.sql.
-- An toàn chạy lại: không tạo Ticket đang mở trùng thiết bị và chỉ seed
-- nhóm yêu cầu DEMO-POSTGRESQL một lần. Không xóa dữ liệu hiện có.

BEGIN;

DO $$
DECLARE
    v_gv01 integer;
    v_gv02 integer;
    v_ktv integer;
    v_ql integer;
BEGIN
    SELECT "Id" INTO v_gv01 FROM public."NguoiDung" WHERE lower("TenDangNhap") = 'gv01';
    SELECT "Id" INTO v_gv02 FROM public."NguoiDung" WHERE lower("TenDangNhap") = 'gv02';
    SELECT "Id" INTO v_ktv FROM public."NguoiDung" WHERE lower("TenDangNhap") = 'ktv01';
    SELECT "Id" INTO v_ql FROM public."NguoiDung" WHERE lower("TenDangNhap") = 'ql01';

    IF v_gv01 IS NULL OR v_gv02 IS NULL OR v_ktv IS NULL OR v_ql IS NULL THEN
        RAISE EXCEPTION 'Thiếu tài khoản demo. Hãy chạy 03_SeedAccounts_PostgreSQL.sql trước.';
    END IF;

    -- Thiết bị đỏ: tạo Ticket ở cột Chờ xử lý.
    UPDATE public."ThietBi"
       SET "TrangThai" = 'Hỏng'
     WHERE "MaThietBi" IN ('F507-PC01', 'F507-PC02', 'F508-PC01', 'G201-TV01');

    INSERT INTO public."TicketSuCo"
        ("MaThietBi", "LoaiLoi", "GhiChu", "NguoiBaoId", "ThoiGianTao", "TrangThai")
    SELECT x.ma_thiet_bi, x.loai_loi, x.ghi_chu, x.nguoi_bao_id,
           now() - x.tu_luc, 'Chờ xử lý'
    FROM (VALUES
        ('F507-PC01', 'Không khởi động', 'DEMO-POSTGRESQL: Máy không lên nguồn, cần kiểm tra bộ nguồn.', v_gv01, interval '3 hours'),
        ('F507-PC02', 'Lỗi màn hình', 'DEMO-POSTGRESQL: Màn hình không hiển thị tín hiệu.', v_gv02, interval '2 hours'),
        ('F508-PC01', 'Lỗi phần cứng', 'DEMO-POSTGRESQL: Máy thường xuyên tự khởi động lại.', v_gv01, interval '90 minutes'),
        ('G201-TV01', 'Lỗi trình chiếu', 'DEMO-POSTGRESQL: TV không nhận tín hiệu HDMI.', v_gv02, interval '45 minutes')
    ) AS x(ma_thiet_bi, loai_loi, ghi_chu, nguoi_bao_id, tu_luc)
    WHERE EXISTS (SELECT 1 FROM public."ThietBi" d WHERE d."MaThietBi" = x.ma_thiet_bi)
      AND NOT EXISTS (
          SELECT 1 FROM public."TicketSuCo" t
          WHERE t."MaThietBi" = x.ma_thiet_bi AND t."TrangThai" <> 'Đã khắc phục'
      );

    -- Thiết bị vàng: tạo Ticket ở cột Đang sửa chữa và gán KTV đang xử lý.
    UPDATE public."ThietBi"
       SET "TrangThai" = 'Đang bảo trì'
     WHERE "MaThietBi" IN ('F509-PC01', 'G202-WIFI', 'AI-KIT01');

    INSERT INTO public."TicketSuCo"
        ("MaThietBi", "LoaiLoi", "GhiChu", "NguoiBaoId", "ThoiGianTao",
         "TrangThai", "KtvXuLyId", "ThoiGianCapNhat")
    SELECT x.ma_thiet_bi, x.loai_loi, x.ghi_chu, x.nguoi_bao_id,
           now() - x.tu_luc, 'Đang sửa chữa', v_ktv, now() - interval '20 minutes'
    FROM (VALUES
        ('F509-PC01', 'Bảo trì máy tính', 'DEMO-POSTGRESQL: Đã tiếp nhận, đang kiểm tra ổ cứng và RAM.', v_gv01, interval '5 hours'),
        ('G202-WIFI', 'Mạng Wi-Fi chập chờn', 'DEMO-POSTGRESQL: KTV đang kiểm tra access point và kết nối mạng.', v_gv02, interval '4 hours'),
        ('AI-KIT01', 'Lỗi bộ Kit Robot', 'DEMO-POSTGRESQL: Đang kiểm tra bộ điều khiển và cảm biến.', v_gv01, interval '1 day')
    ) AS x(ma_thiet_bi, loai_loi, ghi_chu, nguoi_bao_id, tu_luc)
    WHERE EXISTS (SELECT 1 FROM public."ThietBi" d WHERE d."MaThietBi" = x.ma_thiet_bi)
      AND NOT EXISTS (
          SELECT 1 FROM public."TicketSuCo" t
          WHERE t."MaThietBi" = x.ma_thiet_bi AND t."TrangThai" <> 'Đã khắc phục'
      );

    -- Chỉ tạo nhóm yêu cầu mẫu một lần. Chọn lịch tương lai trong tháng 11/2026,
    -- chỉ đổi giữa các phòng cùng cụm F hoặc G và phòng đích không bị trùng giờ.
    IF NOT EXISTS (
        SELECT 1 FROM public."YeuCauDoiPhong"
        WHERE "LyDo" LIKE 'DEMO-POSTGRESQL:%'
    ) THEN
        WITH candidates AS (
            SELECT l."Id" AS lich_day_id,
                   l."GiangVienId" AS giang_vien_id,
                   l."MaPhong" AS phong_cu,
                   target."MaPhong" AS phong_moi,
                   l."Ngay", l."BatDau", l."KetThuc", l."Ca",
                   row_number() OVER (ORDER BY l."Ngay", l."BatDau", l."Id") AS rn
            FROM public."LichDay" l
            JOIN public."Phong" current_room ON current_room."MaPhong" = l."MaPhong"
            CROSS JOIN LATERAL (
                SELECT p2."MaPhong"
                FROM public."Phong" p2
                WHERE p2."Cum" = current_room."Cum"
                  AND p2."MaPhong" <> l."MaPhong"
                  AND p2."Cum" IN ('F', 'G')
                  AND NOT EXISTS (
                      SELECT 1 FROM public."LichDay" other_lesson
                      WHERE other_lesson."Ngay" = l."Ngay"
                        AND other_lesson."MaPhong" = p2."MaPhong"
                        AND other_lesson."Id" <> l."Id"
                        AND other_lesson."BatDau" < l."KetThuc"
                        AND other_lesson."KetThuc" > l."BatDau"
                  )
                ORDER BY p2."MaPhong"
                LIMIT 1
            ) target
            WHERE l."Ngay" BETWEEN DATE '2026-11-11' AND DATE '2026-11-30'
              AND current_room."Cum" IN ('F', 'G')
              AND NOT EXISTS (
                  SELECT 1 FROM public."YeuCauDoiPhong" y
                  WHERE y."LichDayId" = l."Id"
              )
            ORDER BY l."Ngay", l."BatDau", l."Id"
            LIMIT 4
        )
        INSERT INTO public."YeuCauDoiPhong"
            ("LichDayId", "GiangVienId", "PhongCu", "PhongMoi", "LyDo",
             "Loai", "TrangThai", "ThoiGianTao", "NguoiDuyetId", "ThoiGianDuyet", "GhiChuDuyet")
        SELECT c.lich_day_id,
               c.giang_vien_id,
               c.phong_cu,
               c.phong_moi,
               CASE WHEN c.rn = 4
                    THEN 'DEMO-POSTGRESQL: Đề nghị đổi phòng do cần bố trí lại thiết bị giảng dạy.'
                    ELSE 'DEMO-POSTGRESQL: Đề nghị đổi phòng để thuận tiện cho buổi thực hành.'
               END,
               'Có kế hoạch',
               CASE WHEN c.rn = 4 THEN 'Từ chối' ELSE 'Chờ duyệt' END,
               now() - interval '10 minutes' * c.rn,
               CASE WHEN c.rn = 4 THEN v_ql ELSE NULL END,
               CASE WHEN c.rn = 4 THEN now() - interval '5 minutes' ELSE NULL END,
               CASE WHEN c.rn = 4 THEN 'DEMO: Phòng đề xuất chưa phù hợp với kế hoạch sử dụng.' ELSE NULL END
        FROM candidates c;
    END IF;
END $$;

COMMIT;

-- Thống kê nhanh sau khi seed.
SELECT d."TrangThai" AS "TrangThaiThietBi", count(*) AS "SoThietBi"
FROM public."ThietBi" d
WHERE d."MaThietBi" IN ('F507-PC01','F507-PC02','F508-PC01','G201-TV01','F509-PC01','G202-WIFI','AI-KIT01')
GROUP BY d."TrangThai"
ORDER BY d."TrangThai";

SELECT t."TrangThai" AS "TrangThaiTicket", count(*) AS "SoTicket"
FROM public."TicketSuCo" t
WHERE t."GhiChu" LIKE 'DEMO-POSTGRESQL:%'
GROUP BY t."TrangThai"
ORDER BY t."TrangThai";

SELECT y."TrangThai" AS "TrangThaiYeuCau", count(*) AS "SoYeuCau"
FROM public."YeuCauDoiPhong" y
WHERE y."LyDo" LIKE 'DEMO-POSTGRESQL:%'
GROUP BY y."TrangThai"
ORDER BY y."TrangThai";
