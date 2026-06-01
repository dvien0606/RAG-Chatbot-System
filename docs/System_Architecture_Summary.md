# Tóm Tắt Kiến Trúc Hệ Thống RAG Chatbot System

Tài liệu này tóm tắt toàn bộ kiến trúc mã nguồn, cấu trúc các tầng và luồng dữ liệu của hệ thống **RAG Chatbot System**.

---

## 1. Tổng Quan Kiến Trúc
Hệ thống được phát triển theo mô hình **Kiến trúc phân tầng (Layered Architecture)** kết hợp với một dịch vụ phụ trợ **Python (FastAPI)** để xử lý các mô hình học máy (Embedding, Reranking, Vector Search) và giao tiếp với cơ sở dữ liệu quan hệ **PostgreSQL** hỗ trợ phần mở rộng vector (`pgvector`).

```
                    ┌────────────────────────────────────────┐
                    │      RagChatbotSystem.Presentation     │ (ASP.NET Core MVC)
                    └───────────────────┬────────────────────┘
                                        │
                                        ▼
                    ┌────────────────────────────────────────┐
                    │       RagChatbotSystem.Business        │ (Business Logic & LLMs)
                    └───────────────────┬────────────────────┘
                                        │
                      ┌─────────────────┴─────────────────┐
                      ▼                                   ▼
┌────────────────────────────────────────┐    ┌────────────────────────────────────────┐
│      RagChatbotSystem.DataAccess       │    │       RAG-Retrieval-Indexing-API       │ (Python FastAPI)
└───────────────────┬────────────────────┘    └───────────────────┬────────────────────┘
                    │                                             │
                    │                                             │ (FAISS / BM25 / Rerank)
                    ▼                                             ▼
       ┌──────────────────────────┐                  ┌──────────────────────────┐
       │     PostgreSQL DB        │                  │      Local Storage       │
       │  (pgvector: embeddings)  │                  │  (Index Cache / Pickles) │
       └──────────────────────────┘                  └──────────────────────────┘
```

---

## 2. Chi Tiết các Dự Án & Thành Phần

### 2.1. RagChatbotSystem.Presentation (ASP.NET Core MVC)
Đóng vai trò là tầng trình diễn và điều phối yêu cầu từ người dùng:
*   **Controllers**:
    *   `HomeController.cs`: Quản lý không gian làm việc chính (Workspace), tải danh sách Dataset, Document, ChatSession và lịch sử tin nhắn phù hợp với phân quyền người dùng.
    *   `ChatSessionsController.cs`: Tạo mới phiên chat (`ChatSession`) hoặc gửi câu hỏi của người dùng (`SendMessage`) qua phương thức bảo mật.
    *   `AccountController.cs`: Xử lý xác thực người dùng sử dụng Cookie Authentication (`/Account/Login`, `/Account/Logout`).
    *   `DatasetsController.cs` & `DocumentsController.cs`: Quản lý danh mục tri thức và tệp tin đính kèm.
*   **Views**: Cung cấp giao diện trực quan hỗ trợ tải tài liệu lên, quản lý bộ dữ liệu và chat với AI.

### 2.2. RagChatbotSystem.Business (Business Logic Layer)
Xử lý các nghiệp vụ chính của hệ thống, chuẩn bị prompt và tương tác với các API bên ngoài:
*   `DocumentService.cs`:
    *   Hỗ trợ trích xuất văn bản từ nhiều định dạng: PDF (sử dụng thư viện `PdfPig`), Word `.docx` (sử dụng `OpenXml`), và file văn bản thô `.txt`.
    *   Chia nhỏ tài liệu (Text Chunking) với kích thước mảnh mặc định là `600` ký tự và độ trùng lặp là `120` ký tự để bảo toàn ngữ cảnh ở biên.
    *   Xác định trang chính xác của mảnh bằng thuật toán `ResolveDominantPage`.
    *   Gửi tài liệu sang Python API để lấy vector embedding và lưu các bản ghi `Chunk` cùng `VectorRecord` vào database PostgreSQL.
*   `ChatService.cs`:
    *   Tiếp nhận câu hỏi từ người dùng, gọi API Python để tìm kiếm tài liệu tương tự.
    *   Lọc kết quả tương thích theo đúng `DatasetId` của phòng chat.
    *   Tự động xây dựng prompt đưa ngữ cảnh và câu hỏi vào LLM để sinh ra phản hồi.
    *   Phân tích siêu dữ liệu (metadata) của tài liệu trả về để tạo các nguồn trích dẫn (`Citation`) chính xác đến từng trang.
*   `LlmService.cs` / `GroqService.cs` / `OpenAiService.cs`:
    *   Triển khai dịch vụ gọi các mô hình ngôn ngữ lớn (Gemini, Groq Llama, OpenAI).
    *   Có cơ chế tự động dự phòng (fallback): Khi API Key chưa cấu hình hoặc bị lỗi kết nối, dịch vụ sẽ tự động cắt phần văn bản ngữ cảnh liên quan và trả về trực tiếp để đảm bảo hệ thống không bị ngắt quãng.
*   `RagApiClient.cs`:
    *   Cung cấp các hàm gửi HTTP request dạng `PostAsJsonAsync` / `DeleteAsync` kết nối tới API Python (`/index`, `/retrieve`, `/documents/{document_id}`).

### 2.3. RagChatbotSystem.DataAccess (Data Access Layer)
Quản lý giao tiếp dữ liệu với cơ sở dữ liệu:
*   **AppDbContext.cs**: Định nghĩa các tập thực thể chính và cấu hình kiểu dữ liệu `pgvector` sử dụng thư viện `Pgvector` của C#.
*   **Repositories / Unit of Work**:
    *   Áp dụng mô hình Generic Repository (`GenericRepository.cs`) và Unit of Work (`UnitOfWork.cs`) để tối ưu hóa việc quản lý các phiên giao dịch (transactions), đảm bảo tính toàn vẹn của dữ liệu khi lưu đồng thời cả tài liệu, các đoạn nhỏ và vector tương ứng.

### 2.4. RAG-Retrieval-Indexing-API (Python FastAPI)
Tầng xử lý các thuật toán Học máy và tìm kiếm thông tin cục bộ:
*   `main.py`: Cung cấp các API RESTful gọn nhẹ:
    *   `POST /index`: Nhận danh sách chunks, chuyển đổi thành vector embedding qua mô hình HuggingFace, lưu trữ vào chỉ mục FAISS + BM25 và trả về danh sách vector số thực.
    *   `POST /retrieve`: Tiếp nhận câu hỏi và thực hiện truy xuất lai, trả về các đoạn tương đồng cùng điểm số liên quan.
    *   `DELETE /documents/{document_id}`: Xóa toàn bộ chỉ mục vector của tài liệu khỏi cả FAISS và BM25.
*   `service.py`:
    *   **Mô hình Embedding**: Tải mô hình HuggingFace Embeddings để chuyển hóa văn bản thành vector.
    *   **Tìm kiếm lai (Hybrid Search)**:
        *   *Tìm kiếm ngữ nghĩa (Semantic)*: Sử dụng thư viện **FAISS** để tính toán Cosine Similarity hoặc L2 Distance của các Vector.
        *   *Tìm kiếm từ khóa (Lexical)*: Sử dụng thuật toán **BM25Okapi** tìm kiếm tần suất từ khóa xuất hiện.
        *   *Gộp kết quả*: Kết hợp kết quả của FAISS và BM25 bằng công thức cộng điểm thứ hạng nghịch đảo **RRF (Reciprocal Rank Fusion)** có trọng số.
    *   **Xếp hạng lại (Reranking)**: Sử dụng mô hình **Cross-Encoder** (`sentence-transformers`) làm Singleton để sắp xếp lại các mảnh tài liệu tốt nhất, nâng cao độ chính xác trước khi trả kết quả về cho C#.

---

## 3. Thiết Kế Cơ Sở Dữ Liệu (ERD)

Hệ thống quản lý thông tin thông qua 8 bảng chính:

| Tên Bảng | Chức Năng | Các Trường Chính |
| :--- | :--- | :--- |
| **Users** | Lưu trữ thông tin tài khoản người dùng | `UserId`, `FullName`, `Email`, `Role`, `CreatedAt` |
| **Datasets** | Nhóm tài liệu theo chủ đề học tập/làm việc | `DatasetId`, `Name`, `Description`, `CreatedBy` |
| **Documents** | Lưu vết tài liệu gốc tải lên hệ thống | `DocumentId`, `DatasetId`, `FileName`, `FilePath`, `Status` |
| **Chunks** | Đoạn văn bản ngắn được chia nhỏ từ tài liệu | `ChunkId`, `DocumentId`, `ChunkIndex`, `Content`, `PageNumber` |
| **VectorRecords** | Vector biểu diễn ngữ nghĩa của các Chunk | `VectorId`, `ChunkId`, `Embedding` (vector), `EmbeddingModel` |
| **ChatSessions** | Các phòng chat/phiên hội thoại của người dùng | `SessionId`, `UserId`, `DatasetId`, `Title`, `UpdatedAt` |
| **ChatMessages** | Nội dung các tin nhắn (User hoặc AI) | `MessageId`, `SessionId`, `Role`, `Content`, `CreatedAt` |
| **Citations** | Nguồn trích dẫn làm bằng chứng cho câu trả lời | `CitationId`, `MessageId`, `DocumentId`, `ChunkId`, `PageNumber`, `QuoteText` |

---

## 4. Các Luồng Dữ Liệu Nghiệp Vụ

### 4.1. Luồng Nạp Tài Liệu (Data Ingestion Flow)
1.  **Người dùng** tải file lên thông qua giao diện Web.
2.  `DocumentService` lưu trữ file gốc và bắt đầu tiến trình xử lý văn bản:
    *   Trích xuất nội dung văn bản dựa trên định dạng tệp (PDF/Word/Text).
    *   Cắt văn bản thành các `TextChunk`.
3.  `DocumentService` gọi API Python thông qua `/index` gửi kèm các chunks văn bản.
4.  **Dịch vụ Python** thực hiện:
    *   Tính toán vector embedding cho từng mảnh.
    *   Tích hợp vector vào cơ sở dữ liệu vector FAISS cục bộ.
    *   Cập nhật cơ sở dữ liệu từ khóa BM25 cục bộ.
    *   Trả kết quả là mảng vector về cho C#.
5.  `DocumentService` lưu toàn bộ thông tin `Chunk` và `VectorRecord` (chứa các mảng vector số thực) vào PostgreSQL của dự án để quản lý.
6.  Trạng thái của tài liệu chuyển từ `Uploaded` -> `Processing` -> `Completed` (hoặc `Failed` nếu lỗi).

### 4.2. Luồng Truy Vấn và Trả Lời (Query & Answer / RAG Flow)
1.  **Người dùng** gửi câu hỏi trong một phòng chat cụ thể.
2.  `ChatService` lưu câu hỏi vào bảng `ChatMessages` với vai trò là `User`.
3.  `ChatService` gọi API Python thông qua `/retrieve` kèm theo câu hỏi.
4.  **Dịch vụ Python** thực hiện tìm kiếm lai (Hybrid Search):
    *   Chuyển đổi câu hỏi thành vector bằng mô hình embedding.
    *   Tìm kiếm ngữ nghĩa trên FAISS thu về danh sách mảnh phù hợp ngữ nghĩa.
    *   Tìm kiếm từ khóa bằng BM25 thu về danh sách mảnh chứa từ khóa khớp nhất.
    *   Hợp nhất và tính điểm hai tập kết quả bằng thuật toán **RFF (Reciprocal Rank Fusion)**.
    *   Đưa các ứng viên tốt nhất qua mô hình **Cross-Encoder (Reranker)** để tính điểm tương quan trực tiếp giữa câu hỏi và ngữ cảnh, sắp xếp lại và trả về top kết quả tốt nhất.
5.  `ChatService` lọc kết quả theo `DatasetId` được cấp quyền truy cập, nối các nội dung này thành một chuỗi **Ngữ cảnh (Context)**.
6.  `ChatService` xây dựng prompt hoàn chỉnh gửi đến cổng **LLM (Groq Llama 3.3 / OpenAI / Gemini)**:
    *   *Yêu cầu*: Trả lời chính xác câu hỏi dựa trên Ngữ cảnh tiếng Việt được cung cấp. Nếu không tìm thấy thông tin trong ngữ cảnh, bắt buộc trả lời là *"Tôi không tìm thấy thông tin này trong tài liệu của bạn."*.
7.  Nhận câu trả lời từ LLM, `ChatService` lưu nội dung vào bảng `ChatMessages` dưới vai trò `Assistant`.
8.  Trích xuất thông tin tệp tin, trang từ siêu dữ liệu của các mảnh ngữ cảnh đã dùng để tạo bản ghi nguồn dẫn chứng `Citation` lưu vào database.
9.  Trả kết quả hoàn chỉnh hiển thị lên giao diện kèm theo danh sách nguồn tài liệu được đánh dấu vị trí trang cụ thể để người dùng kiểm chứng.
