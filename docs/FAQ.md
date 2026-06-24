# FAQ — RAG Chatbot System

Câu hỏi thường gặp khi cài đặt, chạy và sử dụng hệ thống.

---

## Cài đặt & Khởi chạy

### Hệ thống cần những gì để chạy?

- .NET 9 SDK
- Python 3.10+ (nếu chạy RAG API thủ công)
- Docker Desktop (khuyên dùng)
- Ít nhất một API key LLM: Groq, Gemini hoặc OpenAI

### Cách nhanh nhất để chạy toàn bộ hệ thống?

```bash
docker-compose up --build -d
```

Sau khi chạy:
- Web App: `http://localhost:5259`
- Python API: `http://localhost:8000`
- PostgreSQL: `localhost:5432`

### Làm sao tạo bảng database?

```bash
dotnet ef database update --project RagChatbotSystem.DataAccess --startup-project RagChatbotSystem.Presentation
```

Khi chạy bằng Docker, web app sẽ **tự migrate** khi khởi động.

---

## Tài liệu & RAG

### Hệ thống hỗ trợ những định dạng file nào?

- `.pdf` — trích xuất bằng PdfPig
- `.docx` — trích xuất bằng OpenXml
- `.txt` — đọc trực tiếp

### Tại sao tài liệu bị kẹt ở trạng thái Processing?

Thường do:
- Python RAG API chưa chạy hoặc không kết nối được (`RagApi:BaseUrl`)
- Lỗi khi trích xuất nội dung file
- Timeout khi embedding (file quá lớn)

Kiểm tra log container `rag-retrieval-api` và `rag-web-app`.

### Chunk size và overlap là bao nhiêu?

- **Chunk size:** 600 ký tự
- **Overlap:** 120 ký tự

### Làm sao xóa chỉ mục và index lại từ đầu?

Xóa thư mục cache của Python API:
- `cache/faiss_index`
- `cache/bm25.pkl`

Sau đó khởi động lại `rag-api` và upload/index lại tài liệu.

---

## Chatbot & LLM

### Chatbot trả lời "Tôi không tìm thấy thông tin..." — tại sao?

Hệ thống được thiết kế **chỉ trả lời dựa trên ngữ cảnh tài liệu**. Nếu retrieval không tìm thấy đoạn liên quan trong dataset hiện tại, LLM sẽ trả lời không có thông tin thay vì bịa.

### Cần cấu hình API key nào cho LLM?

Cấu hình ít nhất một trong các biến:
- `GROQ_API_KEY`
- `GEMINI_API_KEY`
- `OPENAI_API_KEY`

Trong file `.env` hoặc `appsettings.json`.

### LLM lỗi thì hệ thống có dừng hẳn không?

Không. Hệ thống có cơ chế **fallback**: trả về nội dung ngữ cảnh tài liệu liên quan khi LLM không khả dụng.

---

## Tài khoản & Phân quyền

### Các vai trò trong hệ thống?

| Vai trò | Quyền chính |
|---|---|
| Admin | Quản lý user, phê duyệt, phân quyền dataset |
| Teacher | Quản lý dataset được phân công |
| Student | Hỏi đáp trên dataset được cấp quyền |

### Tài khoản mới đăng ký không đăng nhập được?

Kiểm tra trường `is_approved` — tài khoản cần được **Admin phê duyệt** trước khi sử dụng.

### Dataset public và private khác nhau thế nào?

- **Public:** Mọi user đã đăng nhập có thể truy cập
- **Private:** Chỉ user có bản ghi trong `DatasetPermissions` mới truy cập được

---

## Triển khai & Git

### Push lên GitHub bị lỗi 403?

Thường do không có quyền ghi vào repo hoặc chưa xác thực đúng (cần PAT thay mật khẩu khi dùng HTTPS).

### Deploy lên VPS hoạt động thế nào?

Push lên nhánh `main` sẽ kích hoạt GitHub Actions (`.github/workflows/deploy.yml`):
1. Copy mã nguồn lên VPS
2. Chạy `docker compose up -d --build`
3. Tự migrate database khi web app khởi động

---

## Kiểm thử

### Chạy unit test?

```bash
dotnet test RagChatbotSystem.sln
```

### Test Python RAG API?

```bash
cd RAG-Retrieval-Indexing-API
uv run python test_api.py
```

---

## Tài liệu liên quan

- [README](../README.md)
- [Thông tin dự án](ThongTinDuAn.md)
- [Thuật ngữ](Glossary.md)
- [Kiến trúc hệ thống](System_Architecture_Summary.md)
