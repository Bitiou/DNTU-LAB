-- DNTULAB POSTGRESQL - RESET TOAN BO SCHEMA PUBLIC
-- CANH BAO: XOA TAT CA BANG, DU LIEU, VIEW, FUNCTION... TRONG SCHEMA public.
-- Chay trong database DntuLab sau khi da backup neu can.

DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO public;

SELECT 'Da reset schema public. Tiep theo chay EF Core Migration (buoc 01).' AS ket_qua;
