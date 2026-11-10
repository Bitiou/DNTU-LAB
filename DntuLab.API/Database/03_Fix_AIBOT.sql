-- DNTULAB - FIX AIBOT + 6 KIT (PostgreSQL)
-- Chay tren database DntuLab sau khi da tao schema. Co the chay lai an toan.

BEGIN;

ALTER TABLE IF EXISTS public."Phong" DROP CONSTRAINT IF EXISTS "CK_Phong_Cum";
ALTER TABLE IF EXISTS public."Phong"
    ADD CONSTRAINT "CK_Phong_Cum" CHECK ("Cum" IN ('F', 'G', 'AIBOT'));

ALTER TABLE IF EXISTS public."Phong" DROP CONSTRAINT IF EXISTS "CK_Phong_Loai";
ALTER TABLE IF EXISTS public."Phong"
    ADD CONSTRAINT "CK_Phong_Loai" CHECK ("LoaiPhong" IN ('PC', 'BYOD', 'Lab AI'));

INSERT INTO public."Phong" ("MaPhong", "LoaiPhong", "Cum", "SucChua")
VALUES ('AIBOT', 'Lab AI', 'AIBOT', 80)
ON CONFLICT ("MaPhong") DO UPDATE SET
    "LoaiPhong" = EXCLUDED."LoaiPhong",
    "Cum" = EXCLUDED."Cum",
    "SucChua" = CASE WHEN public."Phong"."SucChua" <= 0 THEN 80 ELSE public."Phong"."SucChua" END;

INSERT INTO public."ThietBi" ("MaThietBi", "MaPhong", "Loai", "Ten", "TrangThai")
VALUES
 ('AI-KIT01', 'AIBOT', 'KIT', 'Bộ Kit Robot & mô hình STEM 01', 'Sẵn sàng'),
 ('AI-KIT02', 'AIBOT', 'KIT', 'Bộ Kit Robot & mô hình STEM 02', 'Sẵn sàng'),
 ('AI-KIT03', 'AIBOT', 'KIT', 'Bộ Kit Robot & mô hình STEM 03', 'Sẵn sàng'),
 ('AI-KIT04', 'AIBOT', 'KIT', 'Bộ Kit Robot & mô hình STEM 04', 'Sẵn sàng'),
 ('AI-KIT05', 'AIBOT', 'KIT', 'Bộ Kit Robot & mô hình STEM 05', 'Sẵn sàng'),
 ('AI-KIT06', 'AIBOT', 'KIT', 'Bộ Kit Robot & mô hình STEM 06', 'Sẵn sàng')
ON CONFLICT ("MaThietBi") DO UPDATE SET
    "MaPhong" = EXCLUDED."MaPhong",
    "Loai" = EXCLUDED."Loai",
    "Ten" = EXCLUDED."Ten";

COMMIT;

SELECT "MaPhong", "LoaiPhong", "Cum", "SucChua"
FROM public."Phong" WHERE "MaPhong" = 'AIBOT';

SELECT "MaThietBi", "MaPhong", "Loai", "Ten", "TrangThai"
FROM public."ThietBi" WHERE "MaPhong" = 'AIBOT' ORDER BY "MaThietBi";
