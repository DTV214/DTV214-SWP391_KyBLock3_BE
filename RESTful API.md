# Hướng Dẫn Chuẩn Thiết Kế Đường Dẫn RESTful API

---

## 1. Sử dụng Danh từ, Không dùng Động từ

Hành động đã được định nghĩa bởi các phương thức HTTP. URL chỉ nên đại diện cho **Tài nguyên (Resources)**.

* ❌ **Sai:** `GET /getAllUsers`, `POST /createOrder`, `DELETE /deleteProduct/123`
* ✅ **Đúng:** `GET /users`, `POST /orders`, `DELETE /products/123`

## 2. Ưu tiên số nhiều (Plural Nouns)

Sử dụng số nhiều giúp bộ API đồng nhất ngay cả khi bạn truy vấn một hay nhiều đối tượng.

* ❌ **Không nên:** `/user/123`
* ✅ **Nên:** `/users/123`

## 3. Bản đồ Phương thức HTTP (Standard Methods)

| Phương thức | Đường dẫn (URL) | Ý nghĩa |
| :--- | :--- | :--- |
| **GET** | `/products` | Lấy danh sách sản phẩm |
| **GET** | `/products/10` | Lấy thông tin chi tiết sản phẩm 10 |
| **POST** | `/products` | Tạo một sản phẩm mới |
| **PUT** | `/products/10` | Thay thế/Cập nhật toàn bộ sản phẩm 10 |
| **PATCH** | `/products/10` | Cập nhật một phần (vài trường) của sản phẩm 10 |
| **DELETE** | `/products/10` | Xóa sản phẩm 10 |

## 4. Cấu trúc Tài nguyên Phân cấp (Nested Resources)

Để thể hiện mối quan hệ giữa các tài nguyên, hãy sử dụng đường dẫn phân cấp. Tuy nhiên, **không nên lồng quá 2-3 cấp**.

* **Lấy danh sách bình luận của bài viết số 5:**
    `GET /posts/5/comments`
* **Lấy một bình luận cụ thể của bài viết số 5:**
    `GET /posts/5/comments/10`

## 5. Sử dụng Query Parameters để Lọc và Sắp xếp

Đừng thay đổi cấu trúc URL cho các bộ lọc dữ liệu. Hãy sử dụng tham số sau dấu `?`.

* **Lọc (Filtering):** `/products?category=laptop&brand=apple`
* **Sắp xếp (Sorting):** `/users?sort=age,desc`
* **Phân trang (Pagination):** `/orders?page=1&limit=20`

## 6. Các quy tắc định dạng (Naming Conventions)

* **Dùng chữ thường (Lowercase):** Tránh nhầm lẫn vì một số server phân biệt chữ hoa/thường.
  * ✅ `/api/customer-reports`
* **Dùng dấu gạch nối (`-`):** Thay vì gạch dưới (`_`) hoặc CamelCase để tăng khả năng đọc.
  * ✅ `/user-profiles` (Kebab-case)
  * ❌ `/user_profiles` hoặc `/userProfiles`

## 7. Mã phản hồi (HTTP Status Codes) phổ biến

* **200 OK:** Thành công (cho GET, PUT, PATCH).
* **201 Created:** Tạo mới thành công (cho POST).
* **204 No Content:** Xóa thành công (cho DELETE).
* **400 Bad Request:** Dữ liệu gửi lên không hợp lệ.
* **401 Unauthorized:** Chưa đăng nhập.
* **403 Forbidden:** Không có quyền truy cập.
* **404 Not Found:** Không tìm thấy tài nguyên.
* **500 Internal Server Error:** Lỗi hệ thống.
