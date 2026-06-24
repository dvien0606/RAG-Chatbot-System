# Hướng dẫn kiểm thử — RAG Chatbot System

Tài liệu mô tả cách chạy test và kiểm tra nhanh các thành phần chính. **Chỉ là tài liệu**, không ảnh hưởng build hay runtime.

---

## 1. Unit test (.NET)

Dự án có project `RagChatbotSystem.Tests` kiểm thử các logic xử lý tài liệu.

### Chạy toàn bộ test

```bash
dotnet test RagChatbotSystem.sln
```

### Chạy với log chi tiết

```bash
dotnet test RagChatbotSystem.sln --logger "console;verbosity=detailed"
```

### Phạm vi kiểm thử hiện tại

| Nhóm | Nội dung |
|---|---|
| Document processing | Trích xuất, chunking văn bản |
| PDF page resolve | Thuật toán `ResolveDominantPage` |

---

## 2. Kiểm tra Python RAG API

### Khởi động API

```bash
cd RAG-Retrieval-Indexing-API
uv run uvicorn main:app --host 127.0.0.1 --port 8000 --reload
```

### Swagger UI

Truy cập: `http://127.0.0.1:8000/docs`

### Script test tự động

```bash
cd RAG-Retrieval-Indexing-API
uv run python test_api.py
```

Script sẽ index dữ liệu mẫu và chạy một truy vấn retrieval để xác nhận hybrid search hoạt động.

---

## 3. Kiểm tra thủ công qua Web App

### Checklist cơ bản

- [ ] Đăng nhập thành công
- [ ] Tạo dataset mới
- [ ] Upload file PDF/DOCX/TXT
- [ ] Tài liệu chuyển trạng thái `Completed`
- [ ] Tạo phiên chat trên dataset
- [ ] Gửi câu hỏi liên quan nội dung tài liệu
- [ ] Nhận câu trả lời kèm citation (file, trang)

### Câu hỏi mẫu để test RAG

Sau khi upload tài liệu, thử hỏi:
- Nội dung cụ thể có trong file (ví dụ: "Chính sách nghỉ phép là gì?")
- Câu hỏi **không** có trong tài liệu — kỳ vọng: bot trả lời không tìm thấy thông tin

---

## 4. Kiểm tra Docker Compose

```bash
docker-compose up --build -d
docker-compose ps
```

| Container | Port | Kiểm tra |
|---|---|---|
| `rag-web-app` | 5259 | Mở `http://localhost:5259` |
| `rag-retrieval-api` | 8000 | Mở `http://localhost:8000/docs` |
| `rag-postgres-db` | 5432 | `docker exec rag-postgres-db pg_isready -U postgres` |

### Xem log khi lỗi

```bash
docker-compose logs web-app
docker-compose logs rag-api
docker-compose logs db
```

---

## 5. Kiểm tra kết nối giữa các tầng

```
Web App (C#)  ──HTTP──►  Python RAG API  ──►  FAISS + BM25 cache
      │
      └──EF Core──►  PostgreSQL (pgvector)
```

| Kiểm tra | Cách |
|---|---|
| C# → Python API | Upload tài liệu, xem log `rag-api` có nhận `/index` |
| C# → PostgreSQL | Đăng nhập, tạo dataset — không lỗi migration |
| Retrieval | Gửi câu hỏi chat, xem log `/retrieve` trên Python API |

---

## 6. Biến môi trường cần có khi test đầy đủ

| Biến | Bắt buộc | Mục đích |
|---|---|---|
| `DB_PASSWORD` | Có (Docker) | Kết nối PostgreSQL |
| `GROQ_API_KEY` hoặc tương đương | Khuyên có | Test sinh câu trả lời LLM |
| `HF_TOKEN` | Tùy chọn | Tải model embedding nếu bị giới hạn |

---

## 7. Xử lý lỗi thường gặp khi test

| Triệu chứng | Hướng xử lý |
|---|---|
| Upload xong mãi `Processing` | Kiểm tra Python API đang chạy |
| Chat không trả lời | Kiểm tra API key LLM |
| `Connection refused` port 8000 | Khởi động lại `rag-api` |
| Migration lỗi | Xóa volume DB và chạy lại `docker-compose up` |

---

## Tài liệu liên quan

- [FAQ](FAQ.md)
- [Thuật ngữ](Glossary.md)
- [README — Chạy automated tests](../README.md)
