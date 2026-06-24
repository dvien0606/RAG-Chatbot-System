# Thông Tin Dự Án — RAG Chatbot System

## 1. Thông tin chung

| Hạng mục | Nội dung |
|---|---|
| **Tên dự án** | RAG Chatbot System |
| **Môn học** | PRN222 — .NET Application Development |
| **Loại hệ thống** | Web application hỏi đáp thông minh dựa trên tài liệu |
| **Kỹ thuật cốt lõi** | RAG (Retrieval-Augmented Generation) |
| **Repository** | `RAG-Chatbot-System` |

### Mô tả ngắn

Hệ thống cho phép người dùng tải lên tài liệu (PDF, DOCX, TXT), tự động xử lý và lập chỉ mục, sau đó trò chuyện với chatbot AI để nhận câu trả lời dựa trên nội dung tài liệu — kèm trích dẫn nguồn minh bạch. Dự án kết hợp **ASP.NET Core (.NET 9)** cho phần web/nghiệp vụ và **Python FastAPI** cho các tác vụ học máy (embedding, hybrid search, reranking).

---

## 2. Mục tiêu dự án

- Xây dựng chatbot hỏi đáp theo ngữ cảnh tài liệu, hạn chế AI trả lời ngoài phạm vi tri thức đã upload.
- Hỗ trợ quản lý bộ dữ liệu (dataset) theo chủ đề/môn học với cơ chế phân quyền.
- Triển khai pipeline RAG hoàn chỉnh: upload → trích xuất → chunking → embedding → retrieval → sinh câu trả lời → citation.
- Áp dụng kiến trúc phân tầng, tách biệt trách nhiệm giữa Presentation, Business, Data Access và dịch vụ ML.

---

## 3. Đối tượng sử dụng & phân quyền

| Vai trò | Mô tả |
|---|---|
| **Admin** | Quản lý người dùng, phê duyệt tài khoản, phân quyền dataset, phân công giảng viên |
| **Teacher** | Quản lý dataset được phân công, upload/xóa tài liệu, sử dụng chatbot |
| **Student** | Truy cập dataset được cấp quyền, hỏi đáp qua chatbot |

Cơ chế bảo mật:
- Xác thực bằng **Cookie Authentication**
- Dataset **công khai** hoặc **riêng tư** (phân quyền qua bảng `DatasetPermissions`)
- Tài khoản mới cần **phê duyệt** (`is_approved`) trước khi sử dụng
- Hỗ trợ mật khẩu tạm thời và bắt buộc đổi mật khẩu lần đầu

---

## 4. Kiến trúc hệ thống

```
┌─────────────────────────────────────────────────────────┐
│           RagChatbotSystem.Presentation                 │
│              (ASP.NET Core MVC + Swagger)               │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│            RagChatbotSystem.Business                    │
│     (Document, Chat, LLM, Dataset, User Services)       │
└────────────┬───────────────────────────┬────────────────┘
             │                           │
┌────────────▼────────────┐   ┌──────────▼──────────────────┐
│ RagChatbotSystem.       │   │ RAG-Retrieval-Indexing-API  │
│ DataAccess              │   │ (Python FastAPI)            │
│ (EF Core + PostgreSQL)  │   │ FAISS + BM25 + Reranker     │
└────────────┬────────────┘   └──────────┬──────────────────┘
             │                           │
┌────────────▼────────────┐   ┌──────────▼──────────────────┐
│ PostgreSQL + pgvector   │   │ Local cache (FAISS/BM25)    │
└─────────────────────────┘   └─────────────────────────────┘
```

Sơ đồ chi tiết: [`docs/system_architecture_prn.jpg`](system_architecture_prn.jpg)

---

## 5. Cấu trúc solution

```
RAG-Chatbot-System/
├── RagChatbotSystem.sln
├── RagChatbotSystem.Presentation/     # Tầng giao diện (MVC, Controllers, Views)
├── RagChatbotSystem.Business/         # Tầng nghiệp vụ (Services, DTOs, Interfaces)
├── RagChatbotSystem.DataAccess/       # Tầng dữ liệu (Models, EF Core, Migrations)
├── RagChatbotSystem.Tests/            # Unit tests
├── RAG-Retrieval-Indexing-API/        # Python API (embedding & retrieval)
├── docs/                              # Tài liệu kỹ thuật
├── docker-compose.yml                 # Triển khai đa container
├── .env.example                       # Mẫu biến môi trường
└── .github/workflows/deploy.yml       # CI/CD deploy lên VPS
```

### Các project trong solution (.NET)

| Project | Vai trò |
|---|---|
| `RagChatbotSystem.Presentation` | Web MVC, routing, authentication, gọi business layer |
| `RagChatbotSystem.Business` | Logic nghiệp vụ, tích hợp LLM & Python RAG API |
| `RagChatbotSystem.DataAccess` | Entity Framework Core, repository, migrations |
| `RagChatbotSystem.Tests` | Kiểm thử tự động (chunking, PDF page resolve, v.v.) |

---

## 6. Công nghệ sử dụng

### Backend (.NET)

| Công nghệ | Phiên bản / Ghi chú |
|---|---|
| .NET | 9.0 |
| ASP.NET Core MVC | Web framework |
| Entity Framework Core | 9.0.10 |
| PostgreSQL + Npgsql | 9.0.4, hỗ trợ pgvector |
| PdfPig | Trích xuất PDF |
| OpenXml | Trích xuất DOCX |
| Google Drive API | Lưu trữ file tài liệu |
| Swashbuckle | Swagger/OpenAPI (môi trường Development) |

### Dịch vụ ML (Python)

| Công nghệ | Mục đích |
|---|---|
| FastAPI | REST API cho indexing & retrieval |
| sentence-transformers | Embedding (`all-MiniLM-L6-v2`) |
| FAISS | Tìm kiếm ngữ nghĩa (semantic search) |
| rank-bm25 | Tìm kiếm từ khóa (lexical search) |
| Cross-Encoder | Reranking (`ms-marco-MiniLM-L-6-v2`) |

### LLM (tích hợp)

- **Groq** — Llama 3.3 70B (mặc định)
- **OpenAI**
- **Google Gemini**
- Cơ chế **fallback** khi API key thiếu hoặc lỗi kết nối

### Hạ tầng & DevOps

- Docker & Docker Compose
- PostgreSQL (pgvector)
- Nginx (reverse proxy)
- GitHub Actions (auto deploy VPS)
- Certbot (SSL)

---

## 7. Tính năng chính

### Quản lý tri thức
- Tạo/quản lý **Dataset** theo chủ đề
- Upload tài liệu đa định dạng: `.pdf`, `.docx`, `.txt`
- Quy trình upload và indexing tách 2 bước để tối ưu UX
- Xem trước (preview) tài liệu

### Xử lý tài liệu (Ingestion)
- Trích xuất văn bản theo định dạng file
- **Smart Chunking**: 600 ký tự/chunk, overlap 120 ký tự
- Xác định số trang PDF bằng thuật toán `ResolveDominantPage`
- Tạo embedding và lưu vector vào PostgreSQL

### Hỏi đáp (RAG)
- **Hybrid Search**: BM25 + FAISS + RRF (Reciprocal Rank Fusion)
- **Cross-Encoder Reranker** để chọn ngữ cảnh tốt nhất
- Sinh câu trả lời qua LLM với prompt giới hạn trong tài liệu
- **Citations**: trích dẫn file, trang, đoạn nội dung nguồn

### Quản trị hệ thống
- Đăng ký/đăng nhập, phê duyệt tài khoản
- Quản lý người dùng và phân quyền dataset
- Phân công giảng viên quản lý môn học/dataset
- Gửi email (SMTP) cho mật khẩu tạm thời

---

## 8. Controllers chính

| Controller | Chức năng |
|---|---|
| `HomeController` | Workspace chính, hiển thị dataset/chat session |
| `AccountController` | Đăng nhập, đăng xuất, đổi mật khẩu |
| `DatasetsController` | CRUD dataset |
| `DocumentsController` | Upload, xử lý, xóa tài liệu |
| `ChatSessionsController` | Tạo phiên chat, gửi tin nhắn |
| `AdminController` | Quản trị user, phân quyền, phân công giảng viên |
| `TestRagController` | Endpoint kiểm thử RAG (development) |

---

## 9. Services chính (Business Layer)

| Service | Trách nhiệm |
|---|---|
| `DocumentService` | Trích xuất, chunking, gọi Python API index, lưu DB |
| `ChatService` | Retrieval, xây dựng prompt, gọi LLM, tạo citation |
| `ChatSessionService` | Quản lý phiên hội thoại |
| `DatasetService` | Quản lý dataset và phân quyền |
| `UserService` | Quản lý tài khoản người dùng |
| `GroqService` / `OpenAiService` / `LlmService` | Tích hợp LLM |
| `RagApiClient` | HTTP client gọi Python API |
| `GoogleDriveStorageService` | Lưu trữ file trên Google Drive |

---

## 10. Cơ sở dữ liệu

Hệ thống sử dụng **PostgreSQL** với extension **pgvector**.

### Các bảng chính

| Bảng | Mô tả |
|---|---|
| `Users` | Tài khoản người dùng |
| `Datasets` | Nhóm tài liệu theo chủ đề |
| `Documents` | Metadata tài liệu upload |
| `Chunks` | Đoạn văn bản sau khi chia nhỏ |
| `VectorRecords` | Vector embedding của từng chunk |
| `ChatSessions` | Phiên hội thoại |
| `ChatMessages` | Tin nhắn user/assistant |
| `Citations` | Nguồn trích dẫn cho câu trả lời |
| `DatasetPermissions` | Phân quyền truy cập dataset riêng tư |
| `TeacherSubjectAssignments` | Phân công giảng viên quản lý dataset |

Chi tiết ERD: [`docs/ERD_RAG_Chatbot_Explanation.md`](ERD_RAG_Chatbot_Explanation.md)

---

## 11. Luồng nghiệp vụ

### 11.1. Nạp tài liệu (Data Ingestion)

```
User upload file
    → DocumentService lưu file & trích xuất text
    → Chia thành chunks (600 chars, overlap 120)
    → Gọi POST /index (Python API) → nhận embeddings
    → Lưu Chunks + VectorRecords vào PostgreSQL
    → Cập nhật status: Uploaded → Processing → Completed
```

### 11.2. Hỏi đáp (Query & Answer)

```
User gửi câu hỏi trong ChatSession
    → Lưu tin nhắn (role: user)
    → Gọi POST /retrieve (Python API)
        → Hybrid Search (FAISS + BM25 + RRF)
        → Cross-Encoder Reranker
    → Lọc kết quả theo DatasetId
    → Xây dựng prompt + gọi LLM
    → Lưu tin nhắn (role: assistant) + Citations
    → Trả kết quả kèm nguồn trích dẫn
```

---

## 12. Python RAG API

| Endpoint | Method | Mô tả |
|---|---|---|
| `/index` | POST | Nhận chunks, tạo embedding, cập nhật FAISS + BM25 |
| `/retrieve` | POST | Hybrid search + rerank, trả về top-k chunks |
| `/documents/{document_id}` | DELETE | Xóa chỉ mục của tài liệu |
| `/docs` | GET | Swagger UI (FastAPI) |

Cache lưu tại: `cache/faiss_index` và `cache/bm25.pkl`

---

## 13. Cấu hình & triển khai

### Biến môi trường (`.env`)

| Biến | Mô tả |
|---|---|
| `DB_PASSWORD` | Mật khẩu PostgreSQL |
| `GROQ_API_KEY` | API key Groq LLM |
| `GEMINI_API_KEY` | API key Google Gemini |
| `OPENAI_API_KEY` | API key OpenAI |
| `HF_TOKEN` | HuggingFace token (tải model embedding) |
| `GOOGLE_DRIVE_FOLDER_ID` | Thư mục Google Drive lưu file |
| `ADMIN_EMAIL` / `ADMIN_PASSWORD` | Seed tài khoản admin khi khởi động |

### Cổng dịch vụ (Docker Compose)

| Dịch vụ | Cổng |
|---|---|
| Web App (C#) | `5259` |
| Python RAG API | `8000` |
| PostgreSQL | `5432` |

### Khởi chạy nhanh

```bash
docker-compose up --build -d
```

### Chạy tests

```bash
dotnet test RagChatbotSystem.sln
```

Hướng dẫn chi tiết: [`README.md`](../README.md)

---

## 14. CI/CD

Workflow GitHub Actions (`deploy.yml`):
- Trigger khi push lên nhánh `main`
- Copy mã nguồn lên VPS qua SCP
- Chạy `docker compose up -d --build`
- Tự động migrate database khi web app khởi động
- Cấu hình Nginx + SSL (lần đầu)

---

## 15. Tài liệu liên quan trong repo

| File | Nội dung |
|---|---|
| [`README.md`](../README.md) | Hướng dẫn cài đặt, cấu hình, chạy dự án |
| [`docs/System_Architecture_Summary.md`](System_Architecture_Summary.md) | Tóm tắt kiến trúc và luồng dữ liệu |
| [`docs/ERD_RAG_Chatbot_Explanation.md`](ERD_RAG_Chatbot_Explanation.md) | Giải thích chi tiết ERD |
| [`RAG-Retrieval-Indexing-API/README.md`](../RAG-Retrieval-Indexing-API/README.md) | Tài liệu Python API |

---

## 16. Thông số kỹ thuật RAG

| Tham số | Giá trị |
|---|---|
| Chunk size | 600 ký tự |
| Chunk overlap | 120 ký tự |
| Embedding model | `sentence-transformers/all-MiniLM-L6-v2` |
| Reranker model | `cross-encoder/ms-marco-MiniLM-L-6-v2` |
| Hybrid weight | ~70% semantic, ~30% lexical |
| LLM mặc định | Groq — `llama-3.3-70b-versatile` |
| Định dạng file hỗ trợ | PDF, DOCX, TXT |

---

*Tài liệu được tổng hợp từ mã nguồn và tài liệu kỹ thuật hiện có trong repository.*
