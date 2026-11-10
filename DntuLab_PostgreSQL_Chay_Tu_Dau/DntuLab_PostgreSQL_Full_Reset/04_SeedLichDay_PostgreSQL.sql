-- DNTULAB PostgreSQL - lịch mẫu GV01/GV02
-- Giữ đúng logic file SQL Server gốc: 20/10/2026 đến 30/11/2026.
-- 42 ngày x 4 ca/ngày = 168 lịch; GV01 84 ca, GV02 84 ca.
-- Có thể chạy lại: bỏ qua lịch cùng ngày/ca/giảng viên đã có.
DO $$
DECLARE
    v_gv01 integer;
    v_gv02 integer;
    r record;
BEGIN
    SELECT "Id" INTO v_gv01 FROM public."NguoiDung" WHERE lower("TenDangNhap") = 'gv01';
    SELECT "Id" INTO v_gv02 FROM public."NguoiDung" WHERE lower("TenDangNhap") = 'gv02';

    IF v_gv01 IS NULL OR v_gv02 IS NULL THEN
        RAISE EXCEPTION 'Không tìm thấy gv01 và/hoặc gv02. Hãy chạy 03_SeedAccounts_PostgreSQL.sql trước.';
    END IF;

    FOR r IN
        WITH days AS (
            SELECT d::date AS ngay,
                   (d::date - DATE '2026-10-20')::integer AS idx
            FROM generate_series(DATE '2026-10-20', DATE '2026-11-30', INTERVAL '1 day') AS d
        ), sessions AS (
            SELECT ngay, 'Ca 1'::varchar(10) AS ca, v_gv01 AS gv_id,
                   TIME '07:00' AS bat_dau, TIME '09:00' AS ket_thuc,
                   (ARRAY['Lập trình Web API','An toàn thông tin','Phát triển ứng dụng di động','Học máy ứng dụng','Trí tuệ nhân tạo và Robotics'])[(idx % 5) + 1] AS mon_hoc,
                   (ARRAY['F507','F509','F508','G203','AIBOT'])[(idx % 5) + 1] AS ma_phong
            FROM days
            UNION ALL
            SELECT ngay, 'Ca 2', v_gv02, TIME '09:30', TIME '11:30',
                   (ARRAY['Cơ sở dữ liệu','Trí tuệ nhân tạo và Robotics','Mạng máy tính','Cơ sở dữ liệu nâng cao','Phân tích thiết kế hệ thống'])[(idx % 5) + 1],
                   (ARRAY['G201','AIBOT','G202','F508','G204'])[(idx % 5) + 1]
            FROM days
            UNION ALL
            SELECT ngay, 'Ca 3', v_gv01, TIME '13:00', TIME '15:00',
                   (ARRAY['Lập trình Web API','An toàn thông tin','Phát triển ứng dụng di động','Học máy ứng dụng','Trí tuệ nhân tạo và Robotics'])[((idx + 1) % 5) + 1],
                   (ARRAY['F507','F509','F508','G203','AIBOT'])[((idx + 1) % 5) + 1]
            FROM days
            UNION ALL
            SELECT ngay, 'Ca 4', v_gv02, TIME '15:30', TIME '17:30',
                   (ARRAY['Cơ sở dữ liệu','Trí tuệ nhân tạo và Robotics','Mạng máy tính','Cơ sở dữ liệu nâng cao','Phân tích thiết kế hệ thống'])[((idx + 1) % 5) + 1],
                   (ARRAY['G201','AIBOT','G202','F508','G204'])[((idx + 1) % 5) + 1]
            FROM days
        )
        SELECT * FROM sessions ORDER BY ngay, bat_dau
    LOOP
        -- Bỏ qua bản ghi đã seed trước đó.
        IF EXISTS (
            SELECT 1 FROM public."LichDay"
            WHERE "Ngay" = r.ngay AND "Ca" = r.ca AND "GiangVienId" = r.gv_id
        ) THEN
            CONTINUE;
        END IF;

        -- Không âm thầm tạo lịch trùng phòng hoặc trùng giờ giảng viên.
        IF EXISTS (
            SELECT 1 FROM public."LichDay" l
            WHERE l."Ngay" = r.ngay
              AND l."MaPhong" = r.ma_phong
              AND l."BatDau" < r.ket_thuc
              AND l."KetThuc" > r.bat_dau
        ) THEN
            RAISE EXCEPTION 'Xung đột phòng % ngày % ca %. Kiểm tra lịch hiện có trước khi seed.', r.ma_phong, r.ngay, r.ca;
        END IF;

        IF EXISTS (
            SELECT 1 FROM public."LichDay" l
            WHERE l."Ngay" = r.ngay
              AND l."GiangVienId" = r.gv_id
              AND l."BatDau" < r.ket_thuc
              AND l."KetThuc" > r.bat_dau
        ) THEN
            RAISE EXCEPTION 'Giảng viên ID % bị trùng giờ ngày % ca %.', r.gv_id, r.ngay, r.ca;
        END IF;

        INSERT INTO public."LichDay"
            ("Ngay", "Ca", "BatDau", "KetThuc", "MonHoc", "MaPhong", "GiangVienId", "TrangThai", "SoBuoi")
        VALUES
            (r.ngay, r.ca, r.bat_dau, r.ket_thuc, r.mon_hoc, r.ma_phong, r.gv_id, 'Chưa check-in', 1);
    END LOOP;
END $$;

SELECT nd."TenDangNhap", count(*) AS "TongCa",
       count(DISTINCT ld."Ngay") AS "SoNgayCoLich",
       count(*) * 2 AS "TongTiet"
FROM public."LichDay" ld
JOIN public."NguoiDung" nd ON nd."Id" = ld."GiangVienId"
WHERE lower(nd."TenDangNhap") IN ('gv01', 'gv02')
  AND ld."Ngay" BETWEEN DATE '2026-10-20' AND DATE '2026-11-30'
GROUP BY nd."TenDangNhap"
ORDER BY nd."TenDangNhap";
