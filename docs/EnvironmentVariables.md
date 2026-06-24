# Biến môi trường — RAG Chatbot System

Tham chiếu các biến môi trường dùng trong dự án. **Chỉ là tài liệu**, không thay đổi cấu hình hay mã nguồn.

---

## File cấu hình liên quan

| File | Vai trò |
|---|---|
| `.env` | Biến môi trường khi chạy Docker Compose (tạo từ `.env.example`) |
| `RagChatbotSystem.Presentation/appsettings.json` | Cấu hình web app khi chạy local |
| `RAG-Retrieval-Indexing-API/.env` | Cấu hình Python API |
| `docker-compose.yml` | Map biến `.env` vào container |

---

## Biến cơ sở dữ liệu

| Biến | Mặc định | Mô tả |
|---|---|---|
| `DB_PASSWORD` | `root` | Mật khẩu user `postgres` trong container PostgreSQL |

**Connection string** (trong Docker, web app nhận qua biến):

```text
Host=db;Port=5432;Database=RagChatbotSystemDb;Username=postgres;Password=<DB_PASSWORD>
```

Khi chạy local thủ công, đổi `Host=localhost`.

---

## Biến LLM API

Cần **ít nhất một** key để chatbot sinh câu trả lời đầy đủ.

| Biến | Dịch vụ | Ghi chú |
|---|---|---|
| `GROQ_API_KEY` | Groq | LLM mặc định — `llama-3.3-70b-versatile` |
| `GEMINI_API_KEY` | Google Gemini | Tích hợp qua `Gemini__ApiKey` |
| `OPENAI_API_KEY` | OpenAI | Tích hợp qua `OpenAi__ApiKey` |

Map trong `docker-compose.yml`:

```yaml
Gemini__ApiKey=${GEMINI_API_KEY:-}
Groq__ApiKey=${GROQ_API_KEY:-}
OpenAi__ApiKey=${OPENAI_API_KEY:-}
```

---

## Biến HuggingFace

| Biến | Bắt buộc | Mô tả |
|---|---|---|
| `HF_TOKEN` | Không | Token HuggingFace — dùng khi tải model embedding/reranker bị giới hạn |

Dùng bởi container `rag-api` (Python RAG API).

---

## Biến Google Drive

| Biến | Mô tả |
|---|---|
| `GOOGLE_DRIVE_FOLDER_ID` | ID thư mục Google Drive lưu file tài liệu |

File credentials: `google-credentials.json` (mount vào container web app, **không commit** lên Git).

---

## Biến tài khoản Admin (seed)

Dùng khi web app khởi động — tự tạo/cập nhật tài khoản Admin nếu chưa có.

| Biến | Mô tả |
|---|---|
| `ADMIN_EMAIL` | Email đăng nhập admin |
| `ADMIN_PASSWORD` | Mật khẩu admin |

Trong `appsettings.json` tương đương:

```json
"AdminSeed": {
  "Email": "...",
  "Password": "...",
  "Username": "admin",
  "FullName": "System Admin"
}
```

---

## Biến ASP.NET Core

| Biến | Giá trị (Docker) | Mô tả |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Môi trường chạy (Development bật Swagger) |
| `ConnectionStrings__DefaultConnection` | (xem trên) | Chuỗi kết nối PostgreSQL |
| `RagApi__BaseUrl` | `http://rag-api:8000` | URL Python RAG API |

Quy ước: dấu `__` trong biến môi trường tương ứng `:` trong JSON config.

---

## Mẫu file `.env`

```env
# Database
DB_PASSWORD=your_secure_password

# LLM (chọn ít nhất 1)
GROQ_API_KEY=
GEMINI_API_KEY=
OPENAI_API_KEY=

# HuggingFace
HF_TOKEN=

# Google Drive
GOOGLE_DRIVE_FOLDER_ID=

# Admin seed (tùy chọn)
ADMIN_EMAIL=admin@example.com
ADMIN_PASSWORD=YourSecurePassword123!
```

---

## Lưu ý bảo mật

- **Không commit** file `.env`, `appsettings.json` (có secret), `google-credentials.json`
- Dùng `.env.example` làm mẫu — chỉ chứa tên biến, không có giá trị thật
- Trên VPS: tạo `.env` thủ công sau lần deploy đầu (workflow CI/CD có bước copy từ `.env.example` nếu chưa có)

---

## Tài liệu liên quan

- [FAQ](FAQ.md)
- [Hướng dẫn kiểm thử](Testing.md)
- [README — Cấu hình môi trường](../README.md)
