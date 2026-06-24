# Thuật ngữ — RAG Chatbot System

Tài liệu giải thích các thuật ngữ kỹ thuật thường gặp trong dự án.

---

## RAG & AI

| Thuật ngữ | Giải thích |
|---|---|
| **RAG** | Retrieval-Augmented Generation — kỹ thuật cho AI trả lời dựa trên tài liệu được truy xuất, thay vì chỉ dùng kiến thức sẵn có của mô hình. |
| **LLM** | Large Language Model — mô hình ngôn ngữ lớn (Groq Llama, OpenAI, Gemini). |
| **Embedding** | Biểu diễn văn bản dưới dạng vector số thực để so sánh độ tương đồng ngữ nghĩa. |
| **Prompt** | Câu lệnh/đoạn văn bản gửi cho LLM, bao gồm ngữ cảnh tài liệu và câu hỏi người dùng. |
| **Citation** | Trích dẫn nguồn — thông tin file, trang và đoạn văn bản dùng để tạo câu trả lời. |

---

## Tìm kiếm & Retrieval

| Thuật ngữ | Giải thích |
|---|---|
| **Semantic Search** | Tìm kiếm theo ngữ nghĩa, dùng vector embedding (FAISS trong dự án). |
| **Lexical Search** | Tìm kiếm theo từ khóa, khớp chính xác thuật ngữ (BM25 trong dự án). |
| **Hybrid Search** | Kết hợp semantic + lexical để tăng độ chính xác. |
| **FAISS** | Thư viện Facebook AI Similarity Search — lưu và tìm vector nhanh. |
| **BM25** | Thuật toán xếp hạng tài liệu theo tần suất từ khóa (Okapi BM25). |
| **RRF** | Reciprocal Rank Fusion — hợp nhất kết quả từ nhiều chỉ mục tìm kiếm. |
| **Reranker** | Mô hình Cross-Encoder sắp xếp lại các đoạn ứng viên theo mức liên quan với câu hỏi. |
| **Top-K** | Số lượng kết quả tốt nhất được trả về sau bước retrieval. |

---

## Xử lý tài liệu

| Thuật ngữ | Giải thích |
|---|---|
| **Ingestion** | Quy trình nạp tài liệu: upload → trích xuất → chunking → embedding → lưu DB. |
| **Chunk** | Đoạn văn bản nhỏ cắt từ tài liệu gốc (mặc định 600 ký tự, overlap 120). |
| **Overlap** | Phần văn bản trùng lặp giữa hai chunk liên tiếp, giúp giữ ngữ cảnh ở biên. |
| **Indexing** | Tạo chỉ mục tìm kiếm (FAISS + BM25) từ các chunk đã embedding. |
| **pgvector** | Extension PostgreSQL lưu và truy vấn vector embedding. |

---

## Hệ thống & Nghiệp vụ

| Thuật ngữ | Giải thích |
|---|---|
| **Dataset** | Bộ dữ liệu/tài liệu được nhóm theo chủ đề hoặc môn học. |
| **ChatSession** | Phiên hội thoại giữa người dùng và chatbot trên một dataset. |
| **VectorRecord** | Bản ghi lưu vector embedding tương ứng với một chunk. |
| **Fallback** | Cơ chế dự phòng khi LLM lỗi — trả về ngữ cảnh tài liệu thay vì câu trả lời sinh tự do. |

---

## Tài liệu liên quan

- [Thông tin dự án](ThongTinDuAn.md)
- [Kiến trúc hệ thống](System_Architecture_Summary.md)
- [Giải thích ERD](ERD_RAG_Chatbot_Explanation.md)
