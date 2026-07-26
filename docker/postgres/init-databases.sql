-- Postgres container'ı yalnızca POSTGRES_DB ile tek veritabanı oluşturur.
-- NewsService ve NotificationService kendi veritabanlarını bekliyor; burada açıyoruz.
-- Bu script SADECE postgres_data volume'ü boşken (ilk kurulumda) çalışır.

CREATE DATABASE "NewsServiceDb" OWNER ahmet;
CREATE DATABASE "NotificationServiceDb" OWNER ahmet;
