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
    Email NVARCHAR(100) NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Address NVARCHAR(255) NULL,
    LoyaltyPoints INT NOT NULL DEFAULT 0, -- Điểm tích lũy cho Customer
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);
CREATE UNIQUE NONCLUSTERED INDEX UQ_Users_Email ON Users(Email) WHERE Email IS NOT NULL;
GO

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
    Unit NVARCHAR(30) NOT NULL, -- ml, gram, cái, gói, quả
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
    PreparationGuide NVARCHAR(500) NULL,
    CONSTRAINT FK_Recipes_Variants FOREIGN KEY (VariantId) REFERENCES ProductVariants(VariantId) ON DELETE CASCADE,
    CONSTRAINT FK_Recipes_Ingredients FOREIGN KEY (IngredientId) REFERENCES Ingredients(IngredientId),
    CONSTRAINT UQ_Variant_Ingredient UNIQUE (VariantId, IngredientId)
);

-- Bảng Lịch sử giao dịch kho: Nhập kho (Restock), Trừ tự động (OrderDeduction), Hao hụt (Wastage)
CREATE TABLE InventoryTransactions (
    InventoryTransId INT IDENTITY(1,1) PRIMARY KEY,
    IngredientId INT NOT NULL,
    PerformedByUserId INT NOT NULL,
    TransactionType NVARCHAR(30) NOT NULL, -- 'Restock', 'OrderDeduction', 'Wastage'
    QuantityChanged DECIMAL(18,2) NOT NULL,
    UnitCost DECIMAL(18,2) NULL,
    ReasonOrNote NVARCHAR(300) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_InvTrans_Ingredients FOREIGN KEY (IngredientId) REFERENCES Ingredients(IngredientId),
    CONSTRAINT FK_InvTrans_Users FOREIGN KEY (PerformedByUserId) REFERENCES Users(UserId)
);

-- =========================================================================
-- PHÂN HỆ 3: QUẢN LÝ CA THU NGÂN & BÀN GIAO TIỀN MẶT (CASHIER)
-- =========================================================================

CREATE TABLE CashierShifts (
    ShiftId INT IDENTITY(1,1) PRIMARY KEY,
    CashierId INT NOT NULL,
    PreviousShiftId INT NULL,
    StartTime DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    EndTime DATETIME2 NULL,
    OpeningCash DECIMAL(18,2) NOT NULL DEFAULT 0,
    IsOpeningCashConfirmed BIT NOT NULL DEFAULT 0,
    SystemCashSales DECIMAL(18,2) NOT NULL DEFAULT 0,
    ClosingActualCash DECIMAL(18,2) NULL,
    Discrepancy AS (ClosingActualCash - (OpeningCash + SystemCashSales)),
    HandoverNotes NVARCHAR(500) NULL,
    ShiftStatus NVARCHAR(30) NOT NULL DEFAULT N'Open', -- 'Open', 'Closed', 'HandedOver'
    CONSTRAINT FK_Shifts_Cashier FOREIGN KEY (CashierId) REFERENCES Users(UserId),
    CONSTRAINT FK_Shifts_PrevShift FOREIGN KEY (PreviousShiftId) REFERENCES CashierShifts(ShiftId)
);

-- =========================================================================
-- PHÂN HỆ 4: ĐƠN HÀNG & PHA CHẾ (CUSTOMER, CASHIER, BARISTA)
-- =========================================================================

CREATE TABLE Orders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    OrderCode NVARCHAR(30) NOT NULL UNIQUE,
    CustomerId INT NULL,
    CashierId INT NULL,
    CashierShiftId INT NULL,
    BaristaId INT NULL,
    OrderSource NVARCHAR(20) NOT NULL, -- 'Online' hoặc 'Counter'
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
    SugarLevel NVARCHAR(30) NULL DEFAULT N'100%',
    IceLevel NVARCHAR(30) NULL DEFAULT N'100%',
    ItemNote NVARCHAR(255) NULL,
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders(OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Variants FOREIGN KEY (VariantId) REFERENCES ProductVariants(VariantId)
);

-- =========================================================================
-- PHÂN HỆ 5: GIAO HÀNG & ĐỐI SOÁT COD (DELIVERY STAFF)
-- =========================================================================

CREATE TABLE Deliveries (
    DeliveryId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL UNIQUE,
    DeliveryStaffId INT NOT NULL,
    RecipientName NVARCHAR(100) NOT NULL,
    RecipientPhone NVARCHAR(20) NOT NULL,
    ShippingAddress NVARCHAR(300) NOT NULL,
    DeliveryStatus NVARCHAR(30) NOT NULL DEFAULT N'Assigned', -- 'Assigned', 'PickedUp', 'Delivering', 'Delivered', 'Failed'
    CodAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    IsCodCollected BIT NOT NULL DEFAULT 0,
    HandedCodToShiftId INT NULL,
    ProblemNote NVARCHAR(500) NULL,
    AssignedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    DeliveredAt DATETIME2 NULL,
    CONSTRAINT FK_Deliveries_Orders FOREIGN KEY (OrderId) REFERENCES Orders(OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_Deliveries_Staff FOREIGN KEY (DeliveryStaffId) REFERENCES Users(UserId),
    CONSTRAINT FK_Deliveries_Shift FOREIGN KEY (HandedCodToShiftId) REFERENCES CashierShifts(ShiftId)
);

-- =========================================================================
-- PHÂN HỆ 6: CỔNG THANH TOÁN, HOÀN TIỀN & KHIẾU NẠI (PAYMENT & COMPLAINTS)
-- =========================================================================

CREATE TABLE PaymentTransactions (
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    PaymentMethod NVARCHAR(30) NOT NULL, -- 'Cash', 'VNPay', 'MoMo', 'PayOS', 'COD'
    TransactionType NVARCHAR(20) NOT NULL DEFAULT N'Payment', -- 'Payment' hoặc 'Refund'
    Amount DECIMAL(18,2) NOT NULL,
    GatewayTransactionRef NVARCHAR(100) NULL,
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
    IsRefundApproved BIT NOT NULL DEFAULT 0,
    RefundAmount DECIMAL(18,2) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ResolvedAt DATETIME2 NULL,
    CONSTRAINT FK_Complaints_Orders FOREIGN KEY (OrderId) REFERENCES Orders(OrderId),
    CONSTRAINT FK_Complaints_Customer FOREIGN KEY (CustomerId) REFERENCES Users(UserId),
    CONSTRAINT FK_Complaints_Admin FOREIGN KEY (ResolvedByAdminId) REFERENCES Users(UserId)
);
GO

-- =========================================================================
-- DỮ LIỆU MẪU (SEED DATA)
-- =========================================================================

-- 1. Roles
INSERT INTO Roles (RoleName, Description) VALUES
(N'Admin', N'Quản trị viên hệ thống, quản lý kho, nhân sự, menu và báo cáo'),
(N'Cashier', N'Thu ngân tại quầy POS, quản lý ca và bàn giao tiền mặt'),
(N'Barista', N'Nhân viên pha chế, nhận đơn kèm công thức và báo cáo hao hụt'),
(N'DeliveryStaff', N'Nhân viên giao hàng, cập nhật trạng thái giao và thu tiền COD'),
(N'Customer', N'Khách hàng đặt nước, tích điểm và gửi phản hồi/khiếu nại');

-- 2. Users (Mật khẩu mặc định: '123456')
INSERT INTO Users (RoleId, FullName, Phone, Email, PasswordHash, Address, LoyaltyPoints) VALUES
(1, N'Nguyễn Quản Trị (Admin)', '0901000001', 'admin@cms.vn', '123456', N'Quận 1, TP.HCM', 0),
(2, N'Trần Thu Ngân (Cashier)', '0901000002', 'cashier@cms.vn', '123456', N'Quận 3, TP.HCM', 0),
(3, N'Lê Pha Chế (Barista)', '0901000003', 'barista@cms.vn', '123456', N'Bình Thạnh, TP.HCM', 0),
(4, N'Phạm Giao Hàng (Shipper)', '0901000004', 'delivery@cms.vn', '123456', N'Phú Nhuận, TP.HCM', 0),
(5, N'Đặng Minh Hiếu (Customer)', '0988888888', 'customer@cms.vn', '123456', N'Khu CNC Hòa Lạc, Hà Nội', 150);

-- 3. Categories
INSERT INTO Categories (CategoryName, Description) VALUES
(N'Cà phê Việt Nam', N'Các dòng cà phê phin và pha máy truyền thống'),
(N'Trà trái cây', N'Trà hoa quả tươi thanh mát nhiệt đới'),
(N'Đá xay & Matcha', N'Thức uống đá xay kèm kem béo và trà xanh Nhật Bản');

-- 4. Products (13 sản phẩm với ảnh nội bộ đã đối chiếu đúng từng món)
SET IDENTITY_INSERT Products ON;
INSERT INTO Products (ProductId, CategoryId, ProductName, Description, ImageUrl) VALUES
(1, 1, N'Bạc Xỉu Đá', N'Sự kết hợp hoàn hảo giữa sữa đặc, sữa tươi và cà phê Espresso đậm đà', '/images/products/bac-xiu.jpg'),
(2, 1, N'Cà Phê Sữa Đá', N'Cà phê Robusta nguyên chất pha phin truyền thống thơm nồng', '/images/products/cf-sua.jpg'),
(3, 2, N'Trà Đào Cam Sả', N'Trà đen ủ lạnh kết hợp đào miếng giòn, cam vàng và hương sả tự nhiên', '/images/products/tra-dao.jpg'),
(4, 1, N'Cà Phê Đen Đá', N'Hương vị đậm đà nguyên bản từ hạt cà phê Robusta rang mộc thơm nức', '/images/products/cf-den-da.jpg'),
(5, 1, N'Cà Phê Muối', N'Cà phê phin truyền thống hòa quyện cùng lớp kem béo mặn đặc trưng xứ Huế', '/images/products/cf-muoi.png'),
(6, 1, N'Cà Phê Trứng', N'Lớp kem trứng đánh bông mịn ngậy kết hợp cà phê phin nóng hổi truyền thống Hà Nội', '/images/products/cf-trung.webp'),
(7, 1, N'Latte Hạnh Nhân', N'Cà phê Espresso kết hợp sữa hạnh nhân béo ngậy và lớp bọt sữa mềm mịn nghệ thuật', '/images/products/latte-hanh-nhan.jpg'),
(8, 2, N'Trà Vải Hạt Sen', N'Vị trà thanh mát kết hợp trái vải mọng nước và hạt sen bùi béo nấu đường phèn', '/images/products/tra-vai-hat-sen.jpg'),
(9, 2, N'Trà Ô Long Tuyết Lê', N'Trà ô long thượng hạng ủ lạnh cùng nước cốt lê ngọt thanh giải nhiệt', '/images/products/tra-o-long-tuyet-le.jpg'),
(10, 2, N'Trà Dưa Lưới Nha Đam', N'Vị trà xanh nhẹ nhàng quyện nước ép dưa lưới tươi và thạch nha đam thanh mát giòn tan', '/images/products/tra-dua-luoi-nha-dam.png'),
(11, 3, N'Matcha Đá Xay', N'Bột trà xanh Uji cao cấp xay nhuyễn cùng đá và sữa tươi, phủ bông kem béo ngậy', '/images/products/matcha-da-xay.png'),
(12, 3, N'Caramel Espresso Đá Xay', N'Sự hòa quyện mát lạnh giữa espresso hảo hạng, sốt caramel béo ngọt và whipping cream', '/images/products/caramel-espresso-da-xay.jpg'),
(13, 3, N'Matcha Latte Nóng', N'Trà xanh nguyên chất hòa quyện lớp sữa nóng mềm mịn với hậu vị ngọt dịu', '/images/products/matcha-latte-nong.jpg');
SET IDENTITY_INSERT Products OFF;

-- 5. ProductVariants (Size và Giá cho cả 13 sản phẩm)
SET IDENTITY_INSERT ProductVariants ON;
INSERT INTO ProductVariants (VariantId, ProductId, SizeName, Price) VALUES
-- 1. Bạc Xỉu Đá
(1, 1, N'S', 29000), (2, 1, N'M', 35000), (3, 1, N'L', 39000),
-- 2. Cà Phê Sữa Đá
(4, 2, N'M', 29000), (5, 2, N'L', 35000),
-- 3. Trà Đào Cam Sả
(6, 3, N'M', 45000), (7, 3, N'L', 55000),
-- 4. Cà Phê Đen Đá
(8, 4, N'M', 25000), (9, 4, N'L', 29000),
-- 5. Cà Phê Muối
(10, 5, N'M', 35000), (11, 5, N'L', 42000),
-- 6. Cà Phê Trứng
(12, 6, N'M', 40000),
-- 7. Latte Hạnh Nhân
(13, 7, N'M', 45000), (14, 7, N'L', 52000),
-- 8. Trà Vải Hạt Sen
(15, 8, N'M', 45000), (16, 8, N'L', 55000),
-- 9. Trà Ô Long Tuyết Lê
(17, 9, N'M', 45000), (18, 9, N'L', 55000),
-- 10. Trà Dưa Lưới Nha Đam
(19, 10, N'M', 42000), (20, 10, N'L', 49000),
-- 11. Matcha Đá Xay
(21, 11, N'M', 52000), (22, 11, N'L', 59000),
-- 12. Caramel Espresso Đá Xay
(23, 12, N'M', 55000), (24, 12, N'L', 62000),
-- 13. Matcha Latte Nóng
(25, 13, N'M', 45000);
SET IDENTITY_INSERT ProductVariants OFF;

-- 6. Ingredients (Nguyên liệu kho)
SET IDENTITY_INSERT Ingredients ON;
INSERT INTO Ingredients (IngredientId, IngredientName, Unit, StockQuantity, MinStockLevel) VALUES
(1, N'Hạt Cà phê Espresso', N'gram', 8000, 500),
(2, N'Sữa đặc Ngôi Sao', N'ml', 12000, 1000),
(3, N'Sữa tươi thanh trùng', N'ml', 20000, 2000),
(4, N'Cốt Trà Đen', N'ml', 10000, 1000),
(5, N'Syrup Đào', N'ml', 4000, 500),
(6, N'Đào miếng ngâm', N'miếng', 150, 30),
(7, N'Hạt Cà phê Robusta phin', N'gram', 10000, 800),
(8, N'Kem béo mặn (Muối biển)', N'ml', 5000, 400),
(9, N'Trứng gà tươi', N'quả', 100, 20),
(10, N'Sữa hạt hạnh nhân', N'ml', 8000, 800),
(11, N'Trái vải ngâm & Hạt sen', N'gram', 5000, 500),
(12, N'Cốt Trà Ô Long', N'ml', 10000, 1000),
(13, N'Nước cốt lê tuyết', N'ml', 4000, 400),
(14, N'Nước dưa lưới & Nha đam', N'ml', 6000, 600),
(15, N'Bột Matcha Uji', N'gram', 3000, 300),
(16, N'Sốt Caramel', N'ml', 3500, 400),
(17, N'Kem whipping đánh bông', N'ml', 5000, 500);
SET IDENTITY_INSERT Ingredients OFF;

-- 7. ProductRecipes (BOM - Công thức pha chế)
INSERT INTO ProductRecipes (VariantId, IngredientId, QuantityRequired, PreparationGuide) VALUES
-- Bạc Xỉu S / M / L
(1, 1, 14, N'Chiết xuất 14g cà phê lấy 25ml cốt Espresso'),
(1, 2, 30, N'Đong 30ml sữa đặc vào đáy ly'),
(1, 3, 80, N'Đánh bọt 80ml sữa tươi rót lên đá, đổ Espresso lên tầng trên'),
(2, 1, 18, N'Chiết xuất 18g cà phê lấy 30ml cốt Espresso'),
(2, 2, 40, N'Đong 40ml sữa đặc vào đáy ly'),
(2, 3, 100, N'Đánh bọt 100ml sữa tươi rót lên đá, đổ Espresso lên tầng trên'),
(3, 1, 22, N'Chiết xuất 22g cà phê lấy 40ml cốt Espresso'),
(3, 2, 50, N'Đong 50ml sữa đặc vào đáy ly'),
(3, 3, 130, N'Đánh bọt 130ml sữa tươi rót lên đá, đổ Espresso lên tầng trên'),
-- Cà Phê Sữa Đá M / L
(4, 7, 20, N'Pha phin 20g Robusta lấy 40ml cốt'),
(4, 2, 35, N'Khuấy đều 35ml sữa đặc với cà phê nóng rồi cho đá'),
(5, 7, 25, N'Pha phin 25g Robusta lấy 50ml cốt'),
(5, 2, 45, N'Khuấy đều 45ml sữa đặc với cà phê nóng rồi cho đá'),
-- Trà Đào Cam Sả M / L
(6, 4, 150, N'Lắc 150ml cốt trà đen cùng đá'),
(6, 5, 30, N'Thêm 30ml syrup đào và nước cốt cam'),
(6, 6, 3, N'Cho 3 miếng đào giòn lên mặt ly'),
(7, 4, 200, N'Lắc 200ml cốt trà đen cùng đá'),
(7, 5, 40, N'Thêm 40ml syrup đào và nước cốt cam'),
(7, 6, 4, N'Cho 4 miếng đào giòn lên mặt ly'),
-- Cà Phê Đen Đá M
(8, 7, 25, N'Pha phin lấy 50ml cốt đậm đặc, thêm 10g đường lắc tạo bọt'),
-- Cà Phê Muối M
(10, 7, 20, N'Rót 40ml cốt cà phê phin vào đáy ly'),
(10, 8, 45, N'Rót nhẹ 45ml lớp kem béo mặn phủ kín bề mặt ly đá'),
-- Cà Phê Trứng M
(12, 7, 20, N'Rót 40ml cốt cà phê nguyên chất nóng ấm'),
(12, 9, 1, N'Dùng 1 lòng đỏ trứng đánh bông mịn cùng sữa đặc phủ kín ly'),
-- Matcha Đá Xay M
(21, 15, 10, N'Xay 10g bột Matcha cùng đá viên và 60ml sữa tươi'),
(21, 17, 30, N'Xịt 30ml bông kem whipping lên mặt ly'),
-- Caramel Espresso Đá Xay M
(23, 1, 18, N'Chiết xuất 30ml espresso xay cùng đá và 25ml sốt caramel'),
(23, 16, 15, N'Rưới sốt caramel trang trí thành ly'),
(23, 17, 30, N'Xịt 30ml bông kem whipping lên đỉnh'),
-- Matcha Latte Nóng M
(25, 15, 8, N'Đánh tan 8g bột Matcha với 40ml nước ấm 80 độ'),
(25, 3, 150, N'Đánh nóng 150ml sữa tươi ở 65 độ C và tạo hình Latte Art');

-- 8. Ca làm việc mẫu (Cashier UserId = 2)
INSERT INTO CashierShifts (CashierId, OpeningCash, IsOpeningCashConfirmed, SystemCashSales, ShiftStatus, HandoverNotes)
VALUES (2, 1000000, 1, 0, N'Open', N'Đã kiểm đủ 1.000.000đ tiền lẻ đầu ca sáng');
GO
