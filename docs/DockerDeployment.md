# Triển khai Docker — RAG Chatbot System

Hướng dẫn chạy toàn bộ hệ thống bằng Docker Compose. **Chỉ là tài liệu**, không ảnh hưởng build hay runtime.

---

## Kiến trúc container

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   rag-web-app   │────►│  rag-retrieval  │     │ rag-postgres-db │
│   (C# .NET 9)   │     │  -api (Python)  │     │   (pgvector)    │
│   port: 5259    │     │   port: 8000    │     │   port: 5432    │
└────────┬────────┘     └────────┬────────┘     └────────┬────────┘
         │                       │                       │
         └───────────────────────┴───────────────────────┘
                    docker-compose network
```

| Service | Container | Image / Build |
|---|---|---|
| `web-app` | `rag-web-app` | Build từ `RagChatbotSystem.Presentation/Dockerfile` |
| `rag-api` | `rag-retrieval-api` | Build từ `RAG-Retrieval-Indexing-API/Dockerfile` |
| `db` | `rag-postgres-db` | `ankane/pgvector:latest` |

---

## Yêu cầu

- Docker Desktop (hoặc Docker Engine + Compose)
- File `.env` ở thư mục gốc (copy từ `.env.example`)
- File `google-credentials.json` (nếu dùng Google Drive) — tạo file rỗng `{}` nếu chưa có

---

## Khởi chạy

### Lần đầu (build + chạy nền)

```bash
docker-compose up --build -d
```

### Xem trạng thái

```bash
docker-compose ps
```

### Xem log

```bash
docker-compose logs -f web-app
docker-compose logs -f rag-api
docker-compose logs -f db
```

### Dừng hệ thống

```bash
docker-compose down
```

### Dừng và xóa volume (reset DB + cache)

```bash
docker-compose down -v
```

---

## Volume & dữ liệu lưu trữ

| Volume | Nội dung |
|---|---|
| `pgdata` | Dữ liệu PostgreSQL |
| `rag-cache` | Chỉ mục FAISS + BM25 của Python API |

Xóa `rag-cache` khi cần index lại từ đầu (kết hợp re-upload tài liệu).

---

## Cổng truy cập

| Dịch vụ | URL |
|---|---|
| Web App | http://localhost:5259 |
| Python API Swagger | http://localhost:8000/docs |
| PostgreSQL | `localhost:5432` |

---

## Biến môi trường quan trọng

Chi tiết đầy đủ: [EnvironmentVariables.md](EnvironmentVariables.md)

```env
DB_PASSWORD=your_password
GROQ_API_KEY=...
GEMINI_API_KEY=...
OPENAI_API_KEY=...
HF_TOKEN=...
GOOGLE_DRIVE_FOLDER_ID=...
ADMIN_EMAIL=...
ADMIN_PASSWORD=...
```

---

## Thứ tự khởi động

1. **db** — chờ `pg_isready` (healthcheck)
2. **rag-api** — tải model embedding (lần đầu có thể chậm)
3. **web-app** — migrate DB tự động + seed admin

`web-app` phụ thuộc `db` (healthy) và `rag-api` (started).

---

## Deploy lên VPS (tóm tắt)

GitHub Actions (push `main`) tự động:

1. SCP mã nguồn lên `/var/www/rag-chatbot-system`
2. Chạy `docker compose up -d --build`
3. Cấu hình Nginx + SSL (lần đầu)

Workflow: `.github/workflows/deploy.yml`

---

## Xử lý sự cố

| Vấn đề | Cách xử lý |
|---|---|
| Port 5259/8000/5432 bị chiếm | Đổi port trong `docker-compose.yml` hoặc tắt process đang dùng |
| Web app không kết nối DB | Kiểm tra `DB_PASSWORD` khớp giữa `.env` và connection string |
| RAG API OOM | Tăng RAM Docker; model embedding cần ~1–2 GB |
| Migration lỗi | `docker-compose logs web-app`; thử `down -v` và chạy lại |
| Google Drive lỗi | Kiểm tra `google-credentials.json` mount đúng |

---

## Tài liệu liên quan

- [Biến môi trường](EnvironmentVariables.md)
- [Hướng dẫn kiểm thử](Testing.md)
- [FAQ](FAQ.md)
- [README](../README.md)
