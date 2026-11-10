
-- DNTULAB PostgreSQL - Seed phong va thiet bi demo
-- Chay sau EF Core Migration.
-- An toan chay lai: khong xoa du lieu hien co.

BEGIN;

-- =============================================
-- 1. TAO 13 PHONG LAB
-- =============================================

INSERT INTO public."Phong"
    ("MaPhong", "LoaiPhong", "Cum", "SucChua")
VALUES
    ('F507', 'PC', 'F', 50),
    ('F508', 'PC', 'F', 50),
    ('F509', 'PC', 'F', 50),
    ('G201', 'BYOD', 'G', 60),
    ('G202', 'BYOD', 'G', 60),
    ('G203', 'BYOD', 'G', 60),
    ('G204', 'BYOD', 'G', 60),
    ('G205', 'BYOD', 'G', 60),
    ('G206', 'BYOD', 'G', 60),
    ('G207', 'BYOD', 'G', 60),
    ('G208', 'BYOD', 'G', 60),
    ('G209', 'BYOD', 'G', 60),
    ('AIBOT', 'Lab AI', 'AIBOT', 80)
ON CONFLICT ("MaPhong") DO UPDATE SET
    "LoaiPhong" = EXCLUDED."LoaiPhong",
    "Cum" = EXCLUDED."Cum",
    "SucChua" = EXCLUDED."SucChua";


-- =============================================
-- 2. NHA F: 150 MAY TINH
-- =============================================

INSERT INTO public."ThietBi"
    ("MaThietBi", "MaPhong", "Loai", "Ten", "TrangThai")
SELECT
    p."MaPhong" || '-PC' || lpad(n.i::text, 2, '0'),
    p."MaPhong",
    'PC',
    'May tinh ' || lpad(n.i::text, 2, '0'),
    'Sẵn sàng'
FROM public."Phong" p
CROSS JOIN generate_series(1, 50) AS n(i)
WHERE p."Cum" = 'F'
ON CONFLICT ("MaThietBi") DO NOTHING;


-- =============================================
-- 3. NHA G: 27 THIET BI
-- Moi phong: TV, WIFI, TU SAC
-- =============================================

INSERT INTO public."ThietBi"
    ("MaThietBi", "MaPhong", "Loai", "Ten", "TrangThai")
SELECT
    p."MaPhong" || v."Hau",
    p."MaPhong",
    v."Loai",
    v."Ten",
    'Sẵn sàng'
FROM public."Phong" p
CROSS JOIN (
    VALUES
        ('-TV01', 'TV', 'Man hinh Tivi trinh chieu'),
        ('-WIFI', 'WIFI', 'He thong mang khong day'),
        ('-TUSAC01', 'TUSAC', 'Tu sac laptop')
) AS v("Hau", "Loai", "Ten")
WHERE p."Cum" = 'G'
ON CONFLICT ("MaThietBi") DO NOTHING;


-- =============================================
-- 4. AIBOT: 6 BO KIT ROBOT / STEM
-- =============================================

INSERT INTO public."ThietBi"
    ("MaThietBi", "MaPhong", "Loai", "Ten", "TrangThai")
SELECT
    'AI-KIT' || lpad(n.i::text, 2, '0'),
    'AIBOT',
    'KIT',
    'Bo Kit Robot va mo hinh STEM ' || lpad(n.i::text, 2, '0'),
    'Sẵn sàng'
FROM generate_series(1, 6) AS n(i)
ON CONFLICT ("MaThietBi") DO NOTHING;


COMMIT;


-- =============================================
-- 5. KIEM TRA KET QUA
-- =============================================

SELECT
    (SELECT COUNT(*) FROM public."Phong") AS "TongPhong",
    (SELECT COUNT(*) FROM public."ThietBi") AS "TongThietBi";

SELECT
    "Cum",
    COUNT(*) AS "SoPhong"
FROM public."Phong"
GROUP BY "Cum"
ORDER BY "Cum";

SELECT
    "TrangThai",
    COUNT(*) AS "SoThietBi"
FROM public."ThietBi"
GROUP BY "TrangThai"
ORDER BY "TrangThai";
