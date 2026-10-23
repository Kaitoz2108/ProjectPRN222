# ProjectPRN222
# ☕ Café Management System (CMS) - ASP.NET Core 8.0 MVC

Hệ thống Quản lý Chuỗi/Quán Cà phê toàn diện (**Café Management System - CMS**) được xây dựng trên nền tảng **ASP.NET Core 8.0 MVC** kết hợp **Entity Framework Core** và **SQL Server**. Dự án hiện thực hóa đầy đủ các luồng nghiệp vụ khép kín từ Đặt hàng đa kênh (Online & POS tại quầy), Quản lý Ca & Bàn giao tiền mặt, Pha chế theo định lượng công thức (BOM), Tự động trừ kho & Báo cáo hao hụt, Giao hàng & Đối soát COD, đến Xử lý Khiếu nại & Hoàn tiền.

---

## 📌 1. Tổng quan Kiến trúc & Các Tác nhân (External Entities)

Hệ thống được thiết kế bám sát **Context Diagram (DFD Level 0)** với **6 tác nhân** tương tác trực tiếp trên cùng một nền tảng Web MVC thống nhất:

1. **Customer (Khách hàng):**
   - Quản lý tài khoản và hồ sơ cá nhân (`Account & Profile Data`).
   - Xem thực đơn theo danh mục, kích cỡ và giá bán (`Menu / Product Information`).
   - Đặt đồ uống trực tuyến (`Order Data`), thanh toán (`Payment Information`), nhận hóa đơn điện tử (`Bill`).
   - Tích lũy và sử dụng điểm thưởng thành viên (`Loyalty Information` - quy đổi 1 điểm = 1.000đ).
   - Gửi đánh giá, khiếu nại kèm hình ảnh minh chứng (`Complaint / Feedback Data`) và nhận phản hồi xử lý/hoàn tiền (`Complaint Response`).
2. **Cashier (Nhân viên Thu ngân):**
   - Xem thông tin bàn giao từ ca làm việc trước (`Previous Shift Information`) và xác nhận tiền mặt đầu ca (`Cash Confirmation`).
   - Tạo đơn hàng trực tiếp tại quầy POS (`Counter Order Data`), tra cứu/tạo mới hồ sơ khách hàng tại quầy (`Customer Data` / `Customer Information`).
   - Theo dõi doanh thu tiền mặt trong ca (`Shift & Cash Information`) và kiểm đếm tiền mặt thực tế để chốt ca, tính độ lệch két tiền (`Shift & Handover Data`).
3. **Barista (Nhân viên Pha chế):**
   - Nhận đơn hàng thời gian thực trên màn hình KDS kèm hướng dẫn và định lượng công thức pha chế chi tiết (`Order & Recipe Information`).
   - Cập nhật trạng thái pha chế (`Preparation Status`: `Pending` $\rightarrow$ `Preparing` $\rightarrow$ `Ready`) và kích hoạt cơ chế **tự động trừ kho nguyên liệu** theo công thức.
   - Theo dõi tồn kho tại quầy (`Ingredient Information`) và gửi phiếu báo cáo nguyên liệu bị đổ/hỏng trong ca (`Ingredient & Wastage Information`).
4. **Delivery Staff (Nhân viên Giao hàng):**
   - Nhận phân công giao đơn hàng Online kèm thông tin người nhận (`Delivery Assignment & Order Information`).
   - Cập nhật tiến trình giao hàng (`Delivery Status`: `Assigned` $\rightarrow$ `PickedUp` $\rightarrow$ `Delivering` $\rightarrow$ `Delivered` / `Failed`).
   - Đối soát và nộp tiền thu hộ (`COD`) về ca thu ngân hoặc ghi nhận sự cố giao hàng (`Delivery Problem & COD Information`).
5. **Admin (Quản trị viên / Chủ quán):**
   - Quản lý danh mục, sản phẩm, kích cỡ và thiết lập định lượng công thức pha chế (`Menu / Product Data`).
   - Kiểm soát tồn kho (`Inventory Information`) và lập phiếu nhập thêm nguyên liệu mới (`Inventory & Restock Data`).
   - Quản lý hồ sơ nhân viên, xếp lịch phân ca (`Staff & Schedule Data`) và theo dõi chấm công (`Staff & Attendance Information`).
   - Tiếp nhận, phản hồi khiếu nại (`Complaint Information`, `Complaint Response`), phê duyệt hoàn tiền và truy xuất báo cáo doanh thu, độ lệch ca, hao hụt kho (`Complaint & Business Reports`).
6. **Payment Gateway (Cổng thanh toán nội bộ):**
   - Tiếp nhận yêu cầu thanh toán trực tuyến và yêu cầu hoàn tiền từ hệ thống (`Payment & Refund Request`).
   - Xử lý giao dịch, sinh mã tham chiếu (`Transaction Information`) và trả kết quả (`Payment & Refund Result`).

---

## 🛠️ 2. Công nghệ sử dụng (Tech Stack)

- **Framework:** ASP.NET Core 8.0 MVC (Server-Side Rendering với Razor Views, Session, Cookie Authentication).
- **ORM & Database:** Entity Framework Core 8.0 (Database-First / Scaffold) & Microsoft SQL Server (`CafeManagementDB`).
- **Frontend UI:** HTML5, CSS3, Bootstrap 5, Bootstrap Icons.
- **IDE hỗ trợ:** Visual Studio 2022/2026, VS Code, Antigravity IDE.

---

## 🗄️ 3. Cấu trúc Cơ sở dữ liệu (`CafeManagementDB`)

Cơ sở dữ liệu gồm **13 bảng** được chia thành 6 phân hệ liên kết chặt chẽ:
- **Phân quyền & Nhân sự:** `Roles`, `Users`, `WorkSchedules`, `Attendances`.
- **Thực đơn & Công thức (BOM):** `Categories`, `Products`, `ProductVariants`, `Ingredients`, `ProductRecipes`.
- **Giao dịch Kho & Hao hụt:** `InventoryTransactions` (Lưu 3 loại biến động: `Restock`, `OrderDeduction`, `Wastage`).
- **Quản lý Ca & Két tiền:** `CashierShifts` (Tích hợp Computed Column `Discrepancy` tự động tính chênh lệch tiền mặt cuối ca).
- **Đơn hàng & Giao vận:** `Orders`, `OrderItems`, `Deliveries`.
- **Thanh toán & Khiếu nại:** `PaymentTransactions` (`Payment` & `Refund`), `Complaints`.

---

## 📂 4. Cấu trúc Thư mục Dự án (MVC Architecture)

```text
CafeManagementSystem.sln
└── CafeManagementSystem/
    ├── Controllers/
    │   ├── AccountController.cs                    # Đăng nhập 5 Role, Đăng ký, Hồ sơ cá nhân
    │   ├── CustomerController.cs                   # Menu, Giỏ hàng Session, Đặt đơn, Hóa đơn, Khiếu nại
    │   ├── PaymentGatewayController.cs             # Cổng thanh toán QR giả lập & Xử lý giao dịch
    │   ├── CashierController.cs                    # Mở/Chốt ca, Xác nhận tiền mặt, Bán hàng POS
    │   ├── BaristaController.cs                    # Màn hình KDS, Xem công thức, Trừ kho, Báo hao hụt
    │   ├── DeliveryController.cs                   # Nhận đơn giao, Cập nhật trạng thái, Đối soát COD
    │   ├── AdminController.cs                      # Khai báo Partial Class gốc cho Admin
    │   ├── AdminController.CatalogInventory.cs     # Admin: Quản lý Menu, Công thức & Nhập kho
    │   └── AdminController.OperationsReport.cs     # Admin: Nhân sự, Chấm công, Khiếu nại/Hoàn tiền & Báo cáo
    ├── Models/
    │   ├── Entities/                               # CafeManagementDbContext & 13 Entity Classes
    │   └── ViewModels/                             # Các ViewModel đóng gói dữ liệu hiển thị
    ├── Views/
    │   ├── Account/                                # Login, Register, Profile
    │   ├── Customer/                               # Menu, Cart, Bill, MyOrders, Complaints
    │   ├── PaymentGateway/                         # Pay
    │   ├── Cashier/                                # Shift, POS
    │   ├── Barista/                                # KDS, Wastage
    │   ├── Delivery/                               # Index
    │   ├── Admin/                                  # Dashboard, Products, Inventory, StaffSchedule, Complaints
    │   └── Shared/                                 # _Layout.cshtml (Navbar động theo 5 Role)
    ├── wwwroot/                                    # Static files & Ảnh minh chứng khiếu nại
    ├── appsettings.json                            # Chuỗi kết nối SQL Server
    └── Program.cs                                  # Cấu hình DI, Session & Cookie Auth
