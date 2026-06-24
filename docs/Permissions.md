# Phân quyền & Vai trò — RAG Chatbot System

Tài liệu mô tả cơ chế phân quyền trong hệ thống. **Chỉ là tài liệu**, không ảnh hưởng code hay runtime.

---

## Vai trò người dùng (`Users.role`)

| Vai trò | Mô tả ngắn |
|---|---|
| **Admin** | Quản trị toàn hệ thống |
| **Teacher** | Quản lý dataset/môn học được phân công |
| **Student** | Sử dụng chatbot trên dataset được cấp quyền |

---

## Quyền theo vai trò

### Admin

- Phê duyệt tài khoản mới (`is_approved`)
- Tạo/sửa/xóa người dùng
- Phân quyền truy cập dataset riêng tư (`DatasetPermissions`)
- Phân công giảng viên quản lý dataset (`TeacherSubjectAssignments`)
- Truy cập mọi dataset và tài liệu
- Quản lý toàn bộ hệ thống qua `AdminController`

### Teacher

- Quản lý dataset được **phân công** (thêm/sửa/xóa tài liệu)
- Upload và xử lý tài liệu trong dataset của mình
- Tạo phiên chat và hỏi đáp trên dataset được quản lý
- Không quản lý user hệ thống

### Student

- Truy cập dataset **public** hoặc dataset được **cấp quyền riêng**
- Tạo phiên chat và hỏi đáp
- Không upload/xóa tài liệu (trừ khi được cấp quyền đặc biệt qua logic nghiệp vụ)

---

## Chế độ Dataset

### Dataset công khai (`is_public = true`)

- Mọi user đã đăng nhập và được phê duyệt có thể xem tài liệu và chat
- Không cần bản ghi trong `DatasetPermissions`

### Dataset riêng tư (`is_public = false`)

- Chỉ các user có bản ghi trong bảng **`DatasetPermissions`** mới truy cập được
- Admin cấp quyền qua giao diện quản trị

```
Datasets (private) ──1:N──► DatasetPermissions ──N:1──► Users
```

---

## Phân công giảng viên

Bảng **`TeacherSubjectAssignments`** liên kết Teacher với Dataset:

| Trường | Ý nghĩa |
|---|---|
| `teacher_id` | Giảng viên được phân công |
| `dataset_id` | Dataset (môn học/chủ đề) cần quản lý |
| `assigned_by` | Admin thực hiện phân công |
| `assigned_at` | Thời điểm phân công |

Quan hệ: mỗi dataset có **tối đa một** giảng viên quản lý chính (1-1).

---

## Xác thực tài khoản

| Cơ chế | Mô tả |
|---|---|
| Cookie Authentication | Đăng nhập qua `/Account/Login`, session 7 ngày |
| `is_approved` | Tài khoản mới cần Admin phê duyệt |
| `must_change_password` | Bắt buộc đổi mật khẩu lần đầu |
| `temporary_password_expires_at` | Hết hạn mật khẩu tạm |

Trang từ chối truy cập: `/Account/AccessDenied`

---

## Luồng kiểm tra quyền (tóm tắt)

```
User đăng nhập
    → Kiểm tra is_approved
    → Với mỗi Dataset:
        → is_public? → Cho phép
        → Có DatasetPermission? → Cho phép
        → Là Admin? → Cho phép
        → Là Teacher được phân công? → Cho phép (quản lý)
        → Ngược lại → Từ chối
```

Khi chat: `ChatSession` gắn `dataset_id` — retrieval chỉ lấy chunk thuộc dataset đó.

---

## Bảng liên quan

| Bảng | Vai trò phân quyền |
|---|---|
| `Users` | Vai trò, trạng thái tài khoản |
| `Datasets` | `is_public`, `is_approved`, `created_by` |
| `DatasetPermissions` | Quyền truy cập dataset riêng tư |
| `TeacherSubjectAssignments` | Phân công giảng viên |
| `ChatSessions` | Giới hạn phạm vi chat theo dataset |
| `Documents` | `uploaded_by` — theo dõi người upload |

---

## Tài liệu liên quan

- [ERD — DatasetPermissions, TeacherSubjectAssignments](ERD_RAG_Chatbot_Explanation.md)
- [Luồng hỏi đáp](QueryAnswerFlow.md)
- [FAQ](FAQ.md)
- [Thông tin dự án](ThongTinDuAn.md)
