USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'CafeManagementDB')
BEGIN
    ALTER DATABASE CafeManagementDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE CafeManagementDB;
END
GO

CREATE DATABASE CafeManagementDB;
GO

USE CafeManagementDB;
GO

-- =========================================================================
-- PHÂN HỆ 1: NGƯỜI DÙNG, PHÂN QUYỀN, LỊCH LÀM VIỆC & CHẤM CÔNG (ADMIN & STAFF)
-- Đáp ứng: Account & Profile Data, Customer Data, Loyalty Information,
--          Staff & Schedule Data, Staff & Attendance Information
-- =========================================================================

CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE, -- Admin, Cashier, Barista, DeliveryStaff, Customer
    Description NVARCHAR(255) NULL
);

CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    RoleId INT NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20) NOT NULL UNIQUE,
    Email NVARCHAR(100) NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Address NVARCHAR(255) NULL,
    LoyaltyPoints INT NOT NULL DEFAULT 0, -- Điểm tích lũy cho Customer
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);

CREATE TABLE WorkSchedules (
    ScheduleId INT IDENTITY(1,1) PRIMARY KEY,
    StaffId INT NOT NULL,
    CreatedByAdminId INT NOT NULL,
    WorkDate DATE NOT NULL,
    ShiftName NVARCHAR(50) NOT NULL, -- Ca Sáng (07:00-12:00), Ca Chiều (12:00-17:00), Ca Tối (17:00-22:00)
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    Note NVARCHAR(255) NULL,
    CONSTRAINT FK_WorkSchedules_Staff FOREIGN KEY (StaffId) REFERENCES Users(UserId),
    CONSTRAINT FK_WorkSchedules_Admin FOREIGN KEY (CreatedByAdminId) REFERENCES Users(UserId)
);

CREATE TABLE Attendances (
    AttendanceId INT IDENTITY(1,1) PRIMARY KEY,
    ScheduleId INT NOT NULL,
    StaffId INT NOT NULL,
    CheckInTime DATETIME2 NULL,
    CheckOutTime DATETIME2 NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT N'Present', -- Present, Late, Absent, Leave
    Note NVARCHAR(255) NULL,
    CONSTRAINT FK_Attendances_Schedule FOREIGN KEY (ScheduleId) REFERENCES WorkSchedules(ScheduleId),
    CONSTRAINT FK_Attendances_Staff FOREIGN KEY (StaffId) REFERENCES Users(UserId)
);

-- =========================================================================
-- PHÂN HỆ 2: THỰC ĐƠN, ĐỊNH LƯỢNG CÔNG THỨC & KHO NGUYÊN LIỆU (ADMIN & BARISTA)
-- Đáp ứng: Menu / Product Data, Order & Recipe Information,
--          Ingredient Information, Inventory & Restock Data, Wastage Information
-- =========================================================================

CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT NOT NULL,
    ProductName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL,
    ImageUrl NVARCHAR(500) NULL,
    IsAvailable BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
);

-- Bảng biến thể theo Size (S, M, L) và giá bán tương ứng
CREATE TABLE ProductVariants (
    VariantId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    SizeName NVARCHAR(20) NOT NULL, -- S, M, L hoặc Mặc định
    Price DECIMAL(18,2) NOT NULL CHECK (Price >= 0),
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_ProductVariants_Products FOREIGN KEY (ProductId) REFERENCES Products(ProductId) ON DELETE CASCADE
);

CREATE TABLE Ingredients (
    IngredientId INT IDENTITY(1,1) PRIMARY KEY,
    IngredientName NVARCHAR(150) NOT NULL,
    Unit NVARCHAR(30) NOT NULL, -- ml, gram, cái, gói
    StockQuantity DECIMAL(18,2) NOT NULL DEFAULT 0,
    MinStockLevel DECIMAL(18,2) NOT NULL DEFAULT 100, -- Ngưỡng cảnh báo sắp hết kho
    LastUpdated DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

-- Bảng Công thức pha chế (BOM): 1 ProductVariant cần bao nhiêu từng loại Ingredient
CREATE TABLE ProductRecipes (
    RecipeId INT IDENTITY(1,1) PRIMARY KEY,
    VariantId INT NOT NULL,
    IngredientId INT NOT NULL,
    QuantityRequired DECIMAL(18,2) NOT NULL CHECK (QuantityRequired > 0),
    PreparationGuide NVARCHAR(500) NULL, -- Hướng dẫn các bước pha cho Barista
    CONSTRAINT FK_Recipes_Variants FOREIGN KEY (VariantId) REFERENCES ProductVariants(VariantId) ON DELETE CASCADE,
    CONSTRAINT FK_Recipes_Ingredients FOREIGN KEY (IngredientId) REFERENCES Ingredients(IngredientId),
    CONSTRAINT UQ_Variant_Ingredient UNIQUE (VariantId, IngredientId)
);

-- Bảng Lịch sử giao dịch kho: Nhập kho (Restock), Trừ tự động theo đơn (OrderDeduction), Barista báo hủy/hao hụt (Wastage)
CREATE TABLE InventoryTransactions (
    InventoryTransId INT IDENTITY(1,1) PRIMARY KEY,
    IngredientId INT NOT NULL,
    PerformedByUserId INT NOT NULL, -- Admin (khi Restock) hoặc Barista (khi pha chế / báo Wastage)
    TransactionType NVARCHAR(30) NOT NULL, -- 'Restock', 'OrderDeduction', 'Wastage'
    QuantityChanged DECIMAL(18,2) NOT NULL, -- Số dương khi nhập kho, số âm khi trừ/hao hụt
    UnitCost DECIMAL(18,2) NULL, -- Giá vốn lúc nhập hàng (phục vụ báo cáo tài chính của Admin)
    ReasonOrNote NVARCHAR(300) NULL, -- VD: "Nhập hàng đầu tuần", "Đổ sữa khi đánh bọt", "Trừ kho tự động đơn #ORD01"
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_InvTrans_Ingredients FOREIGN KEY (IngredientId) REFERENCES Ingredients(IngredientId),
    CONSTRAINT FK_InvTrans_Users FOREIGN KEY (PerformedByUserId) REFERENCES Users(UserId)
);

-- =========================================================================
-- PHÂN HỆ 3: QUẢN LÝ CA THU NGÂN & BÀN GIAO TIỀN MẶT (CASHIER)
-- Đáp ứng: Previous Shift Information, Shift & Handover Data,
--          Cash Confirmation, Shift & Cash Information
-- =========================================================================

CREATE TABLE CashierShifts (
    ShiftId INT IDENTITY(1,1) PRIMARY KEY,
    CashierId INT NOT NULL,
    PreviousShiftId INT NULL, -- Liên kết tới ca ngay trước đó để lấy số tiền mặt để lại
    StartTime DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    EndTime DATETIME2 NULL,
    OpeningCash DECIMAL(18,2) NOT NULL DEFAULT 0, -- Tiền đầu ca nhận từ ca trước
    IsOpeningCashConfirmed BIT NOT NULL DEFAULT 0, -- Cashier bấm xác nhận tiền mặt đầu ca (Cash Confirmation)
    SystemCashSales DECIMAL(18,2) NOT NULL DEFAULT 0, -- Tiền mặt hệ thống tự cộng (Counter Cash + Shipper nộp COD)
    ClosingActualCash DECIMAL(18,2) NULL, -- Tiền mặt trong két thu ngân đếm thực tế cuối ca
    Discrepancy AS (ClosingActualCash - (OpeningCash + SystemCashSales)), -- Cột tự tính độ lệch tiền thừa/thiếu
    HandoverNotes NVARCHAR(500) NULL, -- Ghi chú bàn giao cho ca tiếp theo
    ShiftStatus NVARCHAR(30) NOT NULL DEFAULT N'Open', -- 'Open', 'Closed', 'HandedOver'
    CONSTRAINT FK_Shifts_Cashier FOREIGN KEY (CashierId) REFERENCES Users(UserId),
    CONSTRAINT FK_Shifts_PrevShift FOREIGN KEY (PreviousShiftId) REFERENCES CashierShifts(ShiftId)
);

-- =========================================================================
-- PHÂN HỆ 4: ĐƠN HÀNG & PHA CHẾ (CUSTOMER, CASHIER, BARISTA)
-- Đáp ứng: Order Data, Counter Order Data, Bill, Preparation Status
-- =========================================================================

CREATE TABLE Orders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    OrderCode NVARCHAR(30) NOT NULL UNIQUE,
    CustomerId INT NULL, -- Có thể NULL nếu khách vãng lai mua tại quầy không đọc SĐT
    CashierId INT NULL, -- Có giá trị nếu là Counter Order
    CashierShiftId INT NULL, -- Thuộc ca làm việc nào của Cashier
    BaristaId INT NULL, -- Barista nhận pha chế đơn này
    OrderSource NVARCHAR(20) NOT NULL, -- 'Online' (Khách tự đặt) hoặc 'Counter' (Thu ngân tạo tại POS)
    SubTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    ShippingFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    LoyaltyPointsUsed INT NOT NULL DEFAULT 0,
    LoyaltyPointsEarned INT NOT NULL DEFAULT 0,
    PreparationStatus NVARCHAR(30) NOT NULL DEFAULT N'Pending', -- 'Pending', 'Preparing', 'Ready', 'Completed', 'Cancelled'
    OrderNote NVARCHAR(300) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CompletedAt DATETIME2 NULL,
    CONSTRAINT FK_Orders_Customer FOREIGN KEY (CustomerId) REFERENCES Users(UserId),
    CONSTRAINT FK_Orders_Cashier FOREIGN KEY (CashierId) REFERENCES Users(UserId),
    CONSTRAINT FK_Orders_Shift FOREIGN KEY (CashierShiftId) REFERENCES CashierShifts(ShiftId),
    CONSTRAINT FK_Orders_Barista FOREIGN KEY (BaristaId) REFERENCES Users(UserId)
);

CREATE TABLE OrderItems (
    OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    VariantId INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(18,2) NOT NULL,
    SugarLevel NVARCHAR(30) NULL DEFAULT N'100%', -- 0%, 30%, 50%, 70%, 100%
    IceLevel NVARCHAR(30) NULL DEFAULT N'100%',   -- Không đá, Ít đá, Bình thường
    ItemNote NVARCHAR(255) NULL,
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders(OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Variants FOREIGN KEY (VariantId) REFERENCES ProductVariants(VariantId)
);

-- =========================================================================
-- PHÂN HỆ 5: GIAO HÀNG & ĐỐI SOÁT COD (DELIVERY STAFF)
-- Đáp ứng: Delivery Assignment & Order Information, Delivery Status,
--          Delivery Problem & COD Information
-- =========================================================================

CREATE TABLE Deliveries (
    DeliveryId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL UNIQUE,
    DeliveryStaffId INT NOT NULL,
    RecipientName NVARCHAR(100) NOT NULL,
    RecipientPhone NVARCHAR(20) NOT NULL,
    ShippingAddress NVARCHAR(300) NOT NULL,
    DeliveryStatus NVARCHAR(30) NOT NULL DEFAULT N'Assigned', -- 'Assigned', 'PickedUp', 'Delivering', 'Delivered', 'Failed'
    CodAmount DECIMAL(18,2) NOT NULL DEFAULT 0, -- Số tiền mặt Shipper cần thu hộ (0 nếu đã trả qua Payment Gateway)
    IsCodCollected BIT NOT NULL DEFAULT 0, -- Shipper đã thu tiền của khách chưa
    HandedCodToShiftId INT NULL, -- Ca thu ngân mà Shipper đã nộp lại tiền COD về quán
    ProblemNote NVARCHAR(500) NULL, -- Ghi chú sự cố giao hàng (Khách không nghe máy, đổ nước, sai địa chỉ...)
    AssignedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    DeliveredAt DATETIME2 NULL,
    CONSTRAINT FK_Deliveries_Orders FOREIGN KEY (OrderId) REFERENCES Orders(OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_Deliveries_Staff FOREIGN KEY (DeliveryStaffId) REFERENCES Users(UserId),
    CONSTRAINT FK_Deliveries_Shift FOREIGN KEY (HandedCodToShiftId) REFERENCES CashierShifts(ShiftId)
);

-- =========================================================================
-- PHÂN HỆ 6: CỔNG THANH TOÁN, HOÀN TIỀN & KHIẾU NẠI (PAYMENT GATEWAY & COMPLAINTS)
-- Đáp ứng: Payment Information, Payment & Refund Request / Result,
--          Transaction Information, Complaint / Feedback Data, Complaint Response
-- =========================================================================

CREATE TABLE PaymentTransactions (
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    PaymentMethod NVARCHAR(30) NOT NULL, -- 'Cash', 'VNPay', 'MoMo', 'PayOS', 'COD'
    TransactionType NVARCHAR(20) NOT NULL DEFAULT N'Payment', -- 'Payment' (Thanh toán) hoặc 'Refund' (Hoàn tiền)
    Amount DECIMAL(18,2) NOT NULL,
    GatewayTransactionRef NVARCHAR(100) NULL, -- Mã giao dịch trả về từ cổng thanh toán
    GatewayResponseCode NVARCHAR(50) NULL,
    PaymentStatus NVARCHAR(30) NOT NULL DEFAULT N'Pending', -- 'Pending', 'Success', 'Failed', 'Refunded'
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId) REFERENCES Orders(OrderId) ON DELETE CASCADE
);

CREATE TABLE Complaints (
    ComplaintId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    CustomerId INT NOT NULL,
    ComplaintType NVARCHAR(50) NOT NULL, -- 'Feedback', 'WrongItem', 'QualityIssue', 'LateDelivery', 'RefundRequest'
    Rating INT NULL CHECK (Rating BETWEEN 1 AND 5),
    Content NVARCHAR(1000) NOT NULL,
    EvidenceImageUrl NVARCHAR(500) NULL,
    ComplaintStatus NVARCHAR(30) NOT NULL DEFAULT N'Pending', -- 'Pending', 'InProgress', 'Resolved', 'Rejected'
    AdminResponse NVARCHAR(1000) NULL,
    ResolvedByAdminId INT NULL,
    IsRefundApproved BIT NOT NULL DEFAULT 0, -- Nếu = 1, kích hoạt luồng Refund sang bảng PaymentTransactions
    RefundAmount DECIMAL(18,2) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ResolvedAt DATETIME2 NULL,
    CONSTRAINT FK_Complaints_Orders FOREIGN KEY (OrderId) REFERENCES Orders(OrderId),
    CONSTRAINT FK_Complaints_Customer FOREIGN KEY (CustomerId) REFERENCES Users(UserId),
    CONSTRAINT FK_Complaints_Admin FOREIGN KEY (ResolvedByAdminId) REFERENCES Users(UserId)
);
GO

-- =========================================================================
-- DỮ LIỆU MẪU (SEED DATA) ĐỂ TEST NGAY CÁC ROLE VÀ LUỒNG NGHIỆP VỤ
-- =========================================================================

-- 1. Thêm 5 Roles
INSERT INTO Roles (RoleName, Description) VALUES
(N'Admin', N'Quản trị viên hệ thống, quản lý kho, nhân sự, menu và báo cáo'),
(N'Cashier', N'Thu ngân tại quầy POS, quản lý ca và bàn giao tiền mặt'),
(N'Barista', N'Nhân viên pha chế, nhận đơn kèm công thức và báo cáo hao hụt'),
(N'DeliveryStaff', N'Nhân viên giao hàng, cập nhật trạng thái giao và thu tiền COD'),
(N'Customer', N'Khách hàng đặt nước, tích điểm và gửi phản hồi/khiếu nại');

-- 2. Thêm 5 Users đại diện (Mật khẩu mẫu: '123456')
INSERT INTO Users (RoleId, FullName, Phone, Email, PasswordHash, Address, LoyaltyPoints) VALUES
(1, N'Nguyễn Quản Trị (Admin)', '0901000001', 'admin@cms.vn', '123456', N'Quận 1, TP.HCM', 0),
(2, N'Trần Thu Ngân (Cashier)', '0901000002', 'cashier@cms.vn', '123456', N'Quận 3, TP.HCM', 0),
(3, N'Lê Pha Chế (Barista)', '0901000003', 'barista@cms.vn', '123456', N'Bình Thạnh, TP.HCM', 0),
(4, N'Phạm Giao Hàng (Shipper)', '0901000004', 'delivery@cms.vn', '123456', N'Phú Nhuận, TP.HCM', 0),
(5, N'Đặng Minh Hiếu (Customer)', '0988888888', 'customer@cms.vn', '123456', N'Khu CNC Hòa Lạc', 150);

-- 3. Thêm Danh mục & Sản phẩm
INSERT INTO Categories (CategoryName, Description) VALUES
(N'Cà phê Việt Nam', N'Các dòng cà phê phin và pha máy truyền thống'),
(N'Trà trái cây', N'Trà hoa quả tươi thanh mát'),
(N'Đá xay & Matcha', N'Thức uống đá xay kèm kem béo');

INSERT INTO Products (CategoryId, ProductName, Description, ImageUrl) VALUES
(1, N'Bạc Xỉu Đá', N'Sự kết hợp hoàn hảo giữa sữa đặc, sữa tươi và cà phê Espresso đậm đà', '/images/products/bac-xiu.jpg'),
(1, N'Cà Phê Sữa Đá', N'Cà phê Robusta nguyên chất pha phin truyền thống', '/images/products/cf-sua.jpg'),
(2, N'Trà Đào Cam Sả', N'Trà đen ủ lạnh kết hợp đào miếng giòn, cam vàng và hương sả', '/images/products/tra-dao.jpg');

-- 4. Thêm Size và Giá (ProductVariants)
INSERT INTO ProductVariants (ProductId, SizeName, Price) VALUES
(1, N'S', 29000), (1, N'M', 35000), (1, N'L', 39000),
(2, N'M', 29000), (2, N'L', 35000),
(3, N'M', 45000), (3, N'L', 55000);

-- 5. Thêm Nguyên liệu trong kho (Ingredients)
INSERT INTO Ingredients (IngredientName, Unit, StockQuantity, MinStockLevel) VALUES
(N'Hạt Cà phê Espresso', N'gram', 5000, 500),
(N'Sữa đặc Ngôi Sao', N'ml', 10000, 1000),
(N'Sữa tươi thanh trùng', N'ml', 15000, 2000),
(N'Cốt Trà Đen', N'ml', 8000, 1000),
(N'Syrup Đào', N'ml', 3000, 500),
(N'Đào miếng ngâm', N'miếng', 120, 20);

-- 6. Thêm Công thức pha chế (ProductRecipes) cho Bạc Xỉu Size M (VariantId = 2) và Trà Đào Size M (VariantId = 6)
INSERT INTO ProductRecipes (VariantId, IngredientId, QuantityRequired, PreparationGuide) VALUES
(2, 1, 18, N'Chiết xuất 18g cà phê lấy 30ml cốt Espresso'),
(2, 2, 40, N'Đong 40ml sữa đặc vào đáy ly'),
(2, 3, 100, N'Đánh bọt 100ml sữa tươi đổ lên trên đá, sau đó rót Espresso lên mặt'),
(6, 4, 150, N'Lắc đều 150ml cốt trà đen với đá'),
(6, 5, 30, N'Pha 30ml Syrup Đào cùng 10ml nước đường'),
(6, 6, 3, N'Trang trí 3 miếng đào ngâm và 1 lát cam sả lên mặt ly');

-- 7. Mở sẵn 1 Ca làm việc mẫu cho Cashier (UserId = 2)
INSERT INTO CashierShifts (CashierId, OpeningCash, IsOpeningCashConfirmed, SystemCashSales, ShiftStatus, HandoverNotes)
VALUES (2, 1000000, 1, 0, N'Open', N'Đã kiểm đủ 1.000.000đ tiền lẻ đầu ca sáng');
GO