# Đồng bộ database theo bản cục bộ

Nguồn chuẩn: database SQL Server `CafeManagementDB` trên máy chủ dự án, theo lựa chọn của chủ dự án trong lần đồng bộ này. Đã đối chiếu với `origin/main` tại commit `5fe16ed864bc9ab95f4545092613e0c8a26c8756` trước khi xuất.

## Phạm vi đã xuất

| Thành phần | Cục bộ / bản xuất mới | GitHub trước đồng bộ |
|---|---:|---:|
| Bảng | 16 | 16 |
| Cột | 136 | 136 |
| Vai trò | 5 | 5 |
| Danh mục | 3 | 3 |
| Sản phẩm | 13 | 13 |
| Biến thể / size | 33 | 25 |
| Nguyên liệu | 6 | 17 |
| Công thức | 6 | 31 |

`DatabaseProject.sql` chứa cấu trúc cục bộ và dữ liệu của Roles, Categories, Products, ProductVariants, Ingredients, ProductRecipes. ID, tên, mô tả, giá, trạng thái bán, tồn kho, ngày tạo/cập nhật và công thức được giữ đúng bản cục bộ. Đường dẫn ảnh của đủ 13 món trỏ tới tài nguyên trong repository.

Không xuất dữ liệu tài khoản thực, đơn hàng, thanh toán, khiếu nại hoặc lịch sử vận hành. Bộ 5 tài khoản demo đã có trên GitHub và một ca demo được giữ để nhóm chạy thử. Database cục bộ đang vận hành không bị tạo lại hoặc sửa giá trong lần đồng bộ này.

## Khởi tạo trên máy mới

1. Pull đầy đủ source và các file ảnh trong `CafeManagementSystem/wwwroot/images/products`.
2. Sao lưu database trên máy đích nếu cần giữ dữ liệu. `DatabaseProject.sql` là script KHỞI TẠO LẠI: nó xóa database `CafeManagementDB` cùng tên trước khi tạo mới.
3. Chạy script bằng SSMS hoặc `sqlcmd -S localhost -E -C -b -f 65001 -i DatabaseProject.sql`.
4. Cập nhật connection string của máy đích nếu instance SQL Server khác.

Với database đang có dữ liệu và chỉ muốn sửa ảnh, dùng `Database/UpdateProductImages.sql`; script đó chỉ sửa các URL ảnh sai đã biết, không tạo lại database.

## Khác biệt được giữ nguyên theo nguồn chuẩn

- ProductId của 10 món bổ sung là **31–40**, không phải 4–13 như script GitHub cũ. VariantId và các liên kết công thức cũng theo ID cục bộ.
- Giữ cả `M`/`L` và `Size M`/`Size L` đang tồn tại; không tự gộp hoặc xóa biến thể có thể được đơn hàng tham chiếu.
- Ví dụ giá cục bộ được giữ: Matcha Đá Xay `Size M` 49.000đ; Caramel Espresso Đá Xay `Size M` 52.000đ; Trà Đào Cam Sả `Size M` 42.000đ. Script GitHub cũ có các mức giá khác.
- Chỉ 6 nguyên liệu và 6 công thức đang có trên máy được xuất. Không đưa thêm 11 nguyên liệu hoặc các công thức từ seed GitHub vào bản cục bộ. Việc bổ sung định lượng cho các món còn thiếu là công việc nghiệp vụ riêng.
- Cục bộ có UNIQUE constraint Email không lọc NULL; GitHub trước đó dùng filtered unique index `WHERE Email IS NOT NULL`. Bản xuất giữ đúng cấu trúc cục bộ theo lựa chọn nguồn chuẩn. Vì vậy database tạo từ bản này vẫn chỉ cho tối đa một tài khoản có Email NULL. Đây là khác biệt đã biết, không phải lỗi xuất SQL; mapping EF hiện tại vẫn có filter và chưa được sửa.

## Kiểm chứng

Đã khởi tạo thành công bản xuất trên một database kiểm tra độc lập. Đối chiếu khớp 16 bảng/136 cột, 26 khóa ngoại, các primary key/unique index, 4 CHECK constraint, default/computed column và dữ liệu của 6 bảng danh mục (bao gồm Roles). Cả 13 đường dẫn ảnh đều có file tương ứng trong repository. Đọc lại database cục bộ sau kiểm thử xác nhận cấu trúc và danh mục không thay đổi. Bộ tài khoản demo và một ca demo được khởi tạo thành công. Database kiểm tra được dọn sau khi đối chiếu.
