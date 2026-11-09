-- Snapshot cấu trúc và danh mục từ SQL Server cục bộ; nguồn chuẩn do chủ dự án lựa chọn.
-- 16 bảng / 136 cột; giữ nguyên ID, giá/size, ảnh, nguyên liệu và công thức hiện có.
-- Chỉ dùng file này để KHỞI TẠO LẠI database: phần đầu sẽ xóa database cùng tên.
-- Tài khoản bên dưới là 5 tài khoản demo có sẵn trên GitHub, không xuất tài khoản hay đơn hàng thực.
-- Xem Database/README.md để biết phạm vi và các khác biệt được giữ nguyên.

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

-- CẤU TRÚC TỪ DATABASE CỤC BỘ

CREATE TABLE dbo.[Roles] (
    [RoleId] int IDENTITY(1,1) NOT NULL,
    [RoleName] nvarchar(50) NOT NULL,
    [Description] nvarchar(255) NULL,
    CONSTRAINT [PK__Roles__8AFACE1AA056D7A2] PRIMARY KEY CLUSTERED ([RoleId]),
    CONSTRAINT [UQ__Roles__8A2B616075C3D663] UNIQUE NONCLUSTERED ([RoleName])
);
GO

CREATE TABLE dbo.[Users] (
    [UserId] int IDENTITY(1,1) NOT NULL,
    [RoleId] int NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Email] nvarchar(100) NULL,
    [PasswordHash] nvarchar(255) NOT NULL,
    [Address] nvarchar(255) NULL,
    [LoyaltyPoints] int NOT NULL DEFAULT ((0)),
    [IsActive] bit NOT NULL DEFAULT ((1)),
    [CreatedAt] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK__Users__1788CC4C4119EC94] PRIMARY KEY CLUSTERED ([UserId]),
    CONSTRAINT [UQ__Users__A9D10534647CBCA7] UNIQUE NONCLUSTERED ([Email]),
    CONSTRAINT [UQ__Users__5C7E359E922831CF] UNIQUE NONCLUSTERED ([Phone])
);
GO

CREATE TABLE dbo.[WorkSchedules] (
    [ScheduleId] int IDENTITY(1,1) NOT NULL,
    [StaffId] int NOT NULL,
    [CreatedByAdminId] int NOT NULL,
    [WorkDate] date NOT NULL,
    [ShiftName] nvarchar(50) NOT NULL,
    [StartTime] time(7) NOT NULL,
    [EndTime] time(7) NOT NULL,
    [Note] nvarchar(255) NULL,
    CONSTRAINT [PK__WorkSche__9C8A5B499B76CD4E] PRIMARY KEY CLUSTERED ([ScheduleId])
);
GO

CREATE TABLE dbo.[Attendances] (
    [AttendanceId] int IDENTITY(1,1) NOT NULL,
    [ScheduleId] int NOT NULL,
    [StaffId] int NOT NULL,
    [CheckInTime] datetime2(7) NULL,
    [CheckOutTime] datetime2(7) NULL,
    [Status] nvarchar(30) NOT NULL DEFAULT (N'Present'),
    [Note] nvarchar(255) NULL,
    CONSTRAINT [PK__Attendan__8B69261CD14EEFC5] PRIMARY KEY CLUSTERED ([AttendanceId])
);
GO

CREATE TABLE dbo.[Categories] (
    [CategoryId] int IDENTITY(1,1) NOT NULL,
    [CategoryName] nvarchar(100) NOT NULL,
    [Description] nvarchar(255) NULL,
    [IsActive] bit NOT NULL DEFAULT ((1)),
    CONSTRAINT [PK__Categori__19093A0B67AB9FE3] PRIMARY KEY CLUSTERED ([CategoryId])
);
GO

CREATE TABLE dbo.[Products] (
    [ProductId] int IDENTITY(1,1) NOT NULL,
    [CategoryId] int NOT NULL,
    [ProductName] nvarchar(150) NOT NULL,
    [Description] nvarchar(500) NULL,
    [ImageUrl] nvarchar(500) NULL,
    [IsAvailable] bit NOT NULL DEFAULT ((1)),
    [CreatedAt] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK__Products__B40CC6CDC9D86FAD] PRIMARY KEY CLUSTERED ([ProductId])
);
GO

CREATE TABLE dbo.[ProductVariants] (
    [VariantId] int IDENTITY(1,1) NOT NULL,
    [ProductId] int NOT NULL,
    [SizeName] nvarchar(20) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT ((1)),
    CONSTRAINT [PK__ProductV__0EA2338478FA710C] PRIMARY KEY CLUSTERED ([VariantId]),
    CHECK ([Price]>=(0))
);
GO

CREATE TABLE dbo.[Ingredients] (
    [IngredientId] int IDENTITY(1,1) NOT NULL,
    [IngredientName] nvarchar(150) NOT NULL,
    [Unit] nvarchar(30) NOT NULL,
    [StockQuantity] decimal(18,2) NOT NULL DEFAULT ((0)),
    [MinStockLevel] decimal(18,2) NOT NULL DEFAULT ((100)),
    [LastUpdated] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK__Ingredie__BEAEB25A94288EF4] PRIMARY KEY CLUSTERED ([IngredientId])
);
GO

CREATE TABLE dbo.[ProductRecipes] (
    [RecipeId] int IDENTITY(1,1) NOT NULL,
    [VariantId] int NOT NULL,
    [IngredientId] int NOT NULL,
    [QuantityRequired] decimal(18,2) NOT NULL,
    [PreparationGuide] nvarchar(500) NULL,
    CONSTRAINT [PK__ProductR__FDD988B02440CC57] PRIMARY KEY CLUSTERED ([RecipeId]),
    CONSTRAINT [UQ_Variant_Ingredient] UNIQUE NONCLUSTERED ([VariantId], [IngredientId]),
    CHECK ([QuantityRequired]>(0))
);
GO

CREATE TABLE dbo.[InventoryTransactions] (
    [InventoryTransId] int IDENTITY(1,1) NOT NULL,
    [IngredientId] int NOT NULL,
    [PerformedByUserId] int NOT NULL,
    [TransactionType] nvarchar(30) NOT NULL,
    [QuantityChanged] decimal(18,2) NOT NULL,
    [UnitCost] decimal(18,2) NULL,
    [ReasonOrNote] nvarchar(300) NULL,
    [CreatedAt] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK__Inventor__1464F9627515D26E] PRIMARY KEY CLUSTERED ([InventoryTransId])
);
GO

CREATE TABLE dbo.[CashierShifts] (
    [ShiftId] int IDENTITY(1,1) NOT NULL,
    [CashierId] int NOT NULL,
    [PreviousShiftId] int NULL,
    [StartTime] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
    [EndTime] datetime2(7) NULL,
    [OpeningCash] decimal(18,2) NOT NULL DEFAULT ((0)),
    [IsOpeningCashConfirmed] bit NOT NULL DEFAULT ((0)),
    [SystemCashSales] decimal(18,2) NOT NULL DEFAULT ((0)),
    [ClosingActualCash] decimal(18,2) NULL,
    [Discrepancy] AS ([ClosingActualCash]-([OpeningCash]+[SystemCashSales])),
    [HandoverNotes] nvarchar(500) NULL,
    [ShiftStatus] nvarchar(30) NOT NULL DEFAULT (N'Open'),
    CONSTRAINT [PK__CashierS__C0A838813EB969F8] PRIMARY KEY CLUSTERED ([ShiftId])
);
GO

CREATE TABLE dbo.[Orders] (
    [OrderId] int IDENTITY(1,1) NOT NULL,
    [OrderCode] nvarchar(30) NOT NULL,
    [CustomerId] int NULL,
    [CashierId] int NULL,
    [CashierShiftId] int NULL,
    [BaristaId] int NULL,
    [OrderSource] nvarchar(20) NOT NULL,
    [SubTotal] decimal(18,2) NOT NULL DEFAULT ((0)),
    [DiscountAmount] decimal(18,2) NOT NULL DEFAULT ((0)),
    [ShippingFee] decimal(18,2) NOT NULL DEFAULT ((0)),
    [TotalAmount] decimal(18,2) NOT NULL DEFAULT ((0)),
    [LoyaltyPointsUsed] int NOT NULL DEFAULT ((0)),
    [LoyaltyPointsEarned] int NOT NULL DEFAULT ((0)),
    [PreparationStatus] nvarchar(30) NOT NULL DEFAULT (N'Pending'),
    [OrderNote] nvarchar(300) NULL,
    [CreatedAt] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
    [CompletedAt] datetime2(7) NULL,
    CONSTRAINT [PK__Orders__C3905BCF92D72DC4] PRIMARY KEY CLUSTERED ([OrderId]),
    CONSTRAINT [UQ__Orders__999B52292FAEC2E4] UNIQUE NONCLUSTERED ([OrderCode])
);
GO

CREATE TABLE dbo.[OrderItems] (
    [OrderItemId] int IDENTITY(1,1) NOT NULL,
    [OrderId] int NOT NULL,
    [VariantId] int NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [SugarLevel] nvarchar(30) NULL DEFAULT (N'100%'),
    [IceLevel] nvarchar(30) NULL DEFAULT (N'100%'),
    [ItemNote] nvarchar(255) NULL,
    CONSTRAINT [PK__OrderIte__57ED068116535615] PRIMARY KEY CLUSTERED ([OrderItemId]),
    CHECK ([Quantity]>(0))
);
GO

CREATE TABLE dbo.[Deliveries] (
    [DeliveryId] int IDENTITY(1,1) NOT NULL,
    [OrderId] int NOT NULL,
    [DeliveryStaffId] int NOT NULL,
    [RecipientName] nvarchar(100) NOT NULL,
    [RecipientPhone] nvarchar(20) NOT NULL,
    [ShippingAddress] nvarchar(300) NOT NULL,
    [DeliveryStatus] nvarchar(30) NOT NULL DEFAULT (N'Assigned'),
    [CodAmount] decimal(18,2) NOT NULL DEFAULT ((0)),
    [IsCodCollected] bit NOT NULL DEFAULT ((0)),
    [HandedCodToShiftId] int NULL,
    [ProblemNote] nvarchar(500) NULL,
    [AssignedAt] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
    [DeliveredAt] datetime2(7) NULL,
    CONSTRAINT [PK__Deliveri__626D8FCED14E47DA] PRIMARY KEY CLUSTERED ([DeliveryId]),
    CONSTRAINT [UQ__Deliveri__C3905BCE6B682C11] UNIQUE NONCLUSTERED ([OrderId])
);
GO

CREATE TABLE dbo.[PaymentTransactions] (
    [PaymentId] int IDENTITY(1,1) NOT NULL,
    [OrderId] int NOT NULL,
    [PaymentMethod] nvarchar(30) NOT NULL,
    [TransactionType] nvarchar(20) NOT NULL DEFAULT (N'Payment'),
    [Amount] decimal(18,2) NOT NULL,
    [GatewayTransactionRef] nvarchar(100) NULL,
    [GatewayResponseCode] nvarchar(50) NULL,
    [PaymentStatus] nvarchar(30) NOT NULL DEFAULT (N'Pending'),
    [CreatedAt] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK__PaymentT__9B556A381DF3C2C6] PRIMARY KEY CLUSTERED ([PaymentId])
);
GO

CREATE TABLE dbo.[Complaints] (
    [ComplaintId] int IDENTITY(1,1) NOT NULL,
    [OrderId] int NOT NULL,
    [CustomerId] int NOT NULL,
    [ComplaintType] nvarchar(50) NOT NULL,
    [Rating] int NULL,
    [Content] nvarchar(1000) NOT NULL,
    [EvidenceImageUrl] nvarchar(500) NULL,
    [ComplaintStatus] nvarchar(30) NOT NULL DEFAULT (N'Pending'),
    [AdminResponse] nvarchar(1000) NULL,
    [ResolvedByAdminId] int NULL,
    [IsRefundApproved] bit NOT NULL DEFAULT ((0)),
    [RefundAmount] decimal(18,2) NULL,
    [CreatedAt] datetime2(7) NOT NULL DEFAULT (sysdatetime()),
    [ResolvedAt] datetime2(7) NULL,
    CONSTRAINT [PK__Complain__740D898FDB2FA40E] PRIMARY KEY CLUSTERED ([ComplaintId]),
    CHECK ([Rating]>=(1) AND [Rating]<=(5))
);
GO

ALTER TABLE dbo.[Attendances] WITH CHECK ADD CONSTRAINT [FK_Attendances_Schedule] FOREIGN KEY ([ScheduleId]) REFERENCES dbo.[WorkSchedules] ([ScheduleId]);
ALTER TABLE dbo.[Attendances] WITH CHECK ADD CONSTRAINT [FK_Attendances_Staff] FOREIGN KEY ([StaffId]) REFERENCES dbo.[Users] ([UserId]);
ALTER TABLE dbo.[Complaints] WITH CHECK ADD CONSTRAINT [FK_Complaints_Admin] FOREIGN KEY ([ResolvedByAdminId]) REFERENCES dbo.[Users] ([UserId]);
ALTER TABLE dbo.[Complaints] WITH CHECK ADD CONSTRAINT [FK_Complaints_Customer] FOREIGN KEY ([CustomerId]) REFERENCES dbo.[Users] ([UserId]);
ALTER TABLE dbo.[Complaints] WITH CHECK ADD CONSTRAINT [FK_Complaints_Orders] FOREIGN KEY ([OrderId]) REFERENCES dbo.[Orders] ([OrderId]);
ALTER TABLE dbo.[Deliveries] WITH CHECK ADD CONSTRAINT [FK_Deliveries_Orders] FOREIGN KEY ([OrderId]) REFERENCES dbo.[Orders] ([OrderId]) ON DELETE CASCADE;
ALTER TABLE dbo.[Deliveries] WITH CHECK ADD CONSTRAINT [FK_Deliveries_Shift] FOREIGN KEY ([HandedCodToShiftId]) REFERENCES dbo.[CashierShifts] ([ShiftId]);
ALTER TABLE dbo.[Deliveries] WITH CHECK ADD CONSTRAINT [FK_Deliveries_Staff] FOREIGN KEY ([DeliveryStaffId]) REFERENCES dbo.[Users] ([UserId]);
ALTER TABLE dbo.[InventoryTransactions] WITH CHECK ADD CONSTRAINT [FK_InvTrans_Ingredients] FOREIGN KEY ([IngredientId]) REFERENCES dbo.[Ingredients] ([IngredientId]);
ALTER TABLE dbo.[InventoryTransactions] WITH CHECK ADD CONSTRAINT [FK_InvTrans_Users] FOREIGN KEY ([PerformedByUserId]) REFERENCES dbo.[Users] ([UserId]);
ALTER TABLE dbo.[OrderItems] WITH CHECK ADD CONSTRAINT [FK_OrderItems_Orders] FOREIGN KEY ([OrderId]) REFERENCES dbo.[Orders] ([OrderId]) ON DELETE CASCADE;
ALTER TABLE dbo.[OrderItems] WITH CHECK ADD CONSTRAINT [FK_OrderItems_Variants] FOREIGN KEY ([VariantId]) REFERENCES dbo.[ProductVariants] ([VariantId]);
ALTER TABLE dbo.[Orders] WITH CHECK ADD CONSTRAINT [FK_Orders_Barista] FOREIGN KEY ([BaristaId]) REFERENCES dbo.[Users] ([UserId]);
ALTER TABLE dbo.[Orders] WITH CHECK ADD CONSTRAINT [FK_Orders_Cashier] FOREIGN KEY ([CashierId]) REFERENCES dbo.[Users] ([UserId]);
ALTER TABLE dbo.[Orders] WITH CHECK ADD CONSTRAINT [FK_Orders_Customer] FOREIGN KEY ([CustomerId]) REFERENCES dbo.[Users] ([UserId]);
ALTER TABLE dbo.[Orders] WITH CHECK ADD CONSTRAINT [FK_Orders_Shift] FOREIGN KEY ([CashierShiftId]) REFERENCES dbo.[CashierShifts] ([ShiftId]);
ALTER TABLE dbo.[PaymentTransactions] WITH CHECK ADD CONSTRAINT [FK_Payments_Orders] FOREIGN KEY ([OrderId]) REFERENCES dbo.[Orders] ([OrderId]) ON DELETE CASCADE;
ALTER TABLE dbo.[Products] WITH CHECK ADD CONSTRAINT [FK_Products_Categories] FOREIGN KEY ([CategoryId]) REFERENCES dbo.[Categories] ([CategoryId]);
ALTER TABLE dbo.[ProductVariants] WITH CHECK ADD CONSTRAINT [FK_ProductVariants_Products] FOREIGN KEY ([ProductId]) REFERENCES dbo.[Products] ([ProductId]) ON DELETE CASCADE;
ALTER TABLE dbo.[ProductRecipes] WITH CHECK ADD CONSTRAINT [FK_Recipes_Ingredients] FOREIGN KEY ([IngredientId]) REFERENCES dbo.[Ingredients] ([IngredientId]);
ALTER TABLE dbo.[ProductRecipes] WITH CHECK ADD CONSTRAINT [FK_Recipes_Variants] FOREIGN KEY ([VariantId]) REFERENCES dbo.[ProductVariants] ([VariantId]) ON DELETE CASCADE;
ALTER TABLE dbo.[CashierShifts] WITH CHECK ADD CONSTRAINT [FK_Shifts_Cashier] FOREIGN KEY ([CashierId]) REFERENCES dbo.[Users] ([UserId]);
ALTER TABLE dbo.[CashierShifts] WITH CHECK ADD CONSTRAINT [FK_Shifts_PrevShift] FOREIGN KEY ([PreviousShiftId]) REFERENCES dbo.[CashierShifts] ([ShiftId]);
ALTER TABLE dbo.[Users] WITH CHECK ADD CONSTRAINT [FK_Users_Roles] FOREIGN KEY ([RoleId]) REFERENCES dbo.[Roles] ([RoleId]);
ALTER TABLE dbo.[WorkSchedules] WITH CHECK ADD CONSTRAINT [FK_WorkSchedules_Admin] FOREIGN KEY ([CreatedByAdminId]) REFERENCES dbo.[Users] ([UserId]);
ALTER TABLE dbo.[WorkSchedules] WITH CHECK ADD CONSTRAINT [FK_WorkSchedules_Staff] FOREIGN KEY ([StaffId]) REFERENCES dbo.[Users] ([UserId]);
GO

-- DANH MỤC CỤC BỘ: giữ nguyên dữ liệu, không chuẩn hóa hoặc gộp size.

SET IDENTITY_INSERT dbo.[Roles] ON;
INSERT INTO dbo.[Roles] ([RoleId], [RoleName], [Description]) VALUES
(1, N'Admin', N'Quản trị viên hệ thống, quản lý kho, nhân sự, menu và báo cáo'),
(2, N'Cashier', N'Thu ngân tại quầy POS, quản lý ca và bàn giao tiền mặt'),
(3, N'Barista', N'Nhân viên pha chế, nhận đơn kèm công thức và báo cáo hao hụt'),
(4, N'DeliveryStaff', N'Nhân viên giao hàng, cập nhật trạng thái giao và thu tiền COD'),
(5, N'Customer', N'Khách hàng đặt nước, tích điểm và gửi phản hồi/khiếu nại');
SET IDENTITY_INSERT dbo.[Roles] OFF;
GO

-- Giữ bộ tài khoản DEMO có sẵn của repository (mật khẩu demo: 123456).
INSERT INTO Users (RoleId, FullName, Phone, Email, PasswordHash, Address, LoyaltyPoints) VALUES
(1, N'Nguyễn Quản Trị (Admin)', '0901000001', 'admin@cms.vn', '123456', N'Quận 1, TP.HCM', 0),
(2, N'Trần Thu Ngân (Cashier)', '0901000002', 'cashier@cms.vn', '123456', N'Quận 3, TP.HCM', 0),
(3, N'Lê Pha Chế (Barista)', '0901000003', 'barista@cms.vn', '123456', N'Bình Thạnh, TP.HCM', 0),
(4, N'Phạm Giao Hàng (Shipper)', '0901000004', 'delivery@cms.vn', '123456', N'Phú Nhuận, TP.HCM', 0),
(5, N'Đặng Minh Hiếu (Customer)', '0988888888', 'customer@cms.vn', '123456', N'Khu CNC Hòa Lạc, Hà Nội', 150);
GO

SET IDENTITY_INSERT dbo.[Categories] ON;
INSERT INTO dbo.[Categories] ([CategoryId], [CategoryName], [Description], [IsActive]) VALUES
(1, N'Cà phê Việt Nam', N'Các dòng cà phê phin và pha máy truyền thống', 1),
(2, N'Trà trái cây', N'Trà hoa quả tươi thanh mát', 1),
(3, N'Đá xay & Matcha', N'Thức uống đá xay kèm kem béo', 1);
SET IDENTITY_INSERT dbo.[Categories] OFF;
GO

SET IDENTITY_INSERT dbo.[Products] ON;
INSERT INTO dbo.[Products] ([ProductId], [CategoryId], [ProductName], [Description], [ImageUrl], [IsAvailable], [CreatedAt]) VALUES
(1, 1, N'Bạc Xỉu Đá', N'Sự kết hợp hoàn hảo giữa sữa đặc, sữa tươi và cà phê Espresso đậm đà', N'/images/products/bac-xiu.jpg', 1, N'2026-10-22T11:52:28.9423403'),
(2, 1, N'Cà Phê Sữa Đá', N'Cà phê Robusta nguyên chất pha phin truyền thống', N'/images/products/cf-sua.jpg', 1, N'2026-10-22T11:52:28.9423403'),
(3, 2, N'Trà Đào Cam Sả', N'Trà đen ủ lạnh kết hợp đào miếng giòn, cam vàng và hương sả', N'/images/products/tra-dao.jpg', 1, N'2026-10-22T11:52:28.9423403'),
(31, 1, N'Cà Phê Đen Đá', N'Hương vị đậm đà nguyên bản từ hạt cà phê Robusta rang mộc thơm lừng', N'/images/products/cf-den-da.jpg', 1, N'2026-11-03T00:06:45.08'),
(32, 1, N'Cà Phê Muối', N'Cà phê phin truyền thống hòa quyện cùng lớp kem béo mặn đặc trưng xứ Huế', N'/images/products/cf-muoi.png', 1, N'2026-11-03T00:06:45.08'),
(33, 1, N'Cà Phê Trứng', N'Lớp kem trứng đánh bông mịn ngậy kết hợp cà phê phin đậm đặc nóng hổi', N'/images/products/cf-trung.webp', 1, N'2026-11-03T00:06:45.08'),
(34, 1, N'Latte Hạnh Nhân', N'Cà phê Espresso kết hợp sữa hạnh nhân béo ngậy và bọt sữa mịn', N'/images/products/latte-hanh-nhan.jpg', 1, N'2026-11-03T00:06:45.08'),
(35, 2, N'Trà Vải Hạt Sen', N'Vị trà thanh mát kết hợp trái vải mọng nước và hạt sen bùi béo tự nhiên', N'/images/products/tra-vai-hat-sen.jpg', 1, N'2026-11-03T00:06:45.08'),
(36, 2, N'Trà Ô Long Tuyết Lê', N'Trà ô long thượng hạng ủ lạnh cùng nước cốt lê ngọt thanh và thạch giòn', N'/images/products/tra-o-long-tuyet-le.jpg', 1, N'2026-11-03T00:06:45.08'),
(37, 2, N'Trà Dưa Lưới Nha Đam', N'Vị trà xanh nhẹ nhàng quyện nước ép dưa lưới tươi và topping nha đam', N'/images/products/tra-dua-luoi-nha-dam.png', 1, N'2026-11-03T00:06:45.08'),
(38, 3, N'Matcha Đá Xay', N'Bột trà xanh Uji cao cấp xay nhuyễn cùng đá và sữa tươi, phủ whipping cream', N'/images/products/matcha-da-xay.png', 1, N'2026-11-03T00:06:45.08'),
(39, 3, N'Caramel Espresso Đá Xay', N'Sự hòa quyện mát lạnh giữa espresso, sốt caramel hảo hạng và sữa đá tuyết', N'/images/products/caramel-espresso-da-xay.jpg', 1, N'2026-11-03T00:06:45.08'),
(40, 3, N'Matcha Latte Nóng', N'Trà xanh nguyên chất hòa quyện lớp sữa nóng mềm mượt và lớp bọt sữa vẽ nghệ thuật', N'/images/products/matcha-latte-nong.jpg', 1, N'2026-11-03T00:06:45.08');
SET IDENTITY_INSERT dbo.[Products] OFF;
GO

SET IDENTITY_INSERT dbo.[ProductVariants] ON;
INSERT INTO dbo.[ProductVariants] ([VariantId], [ProductId], [SizeName], [Price], [IsActive]) VALUES
(1, 1, N'S', 29000.0, 1),
(2, 1, N'M', 35000.0, 1),
(3, 1, N'L', 39000.0, 1),
(4, 2, N'M', 29000.0, 1),
(5, 2, N'L', 35000.0, 1),
(6, 3, N'M', 45000.0, 1),
(7, 3, N'L', 55000.0, 1),
(15, 1, N'Size M', 32000.0, 1),
(16, 1, N'Size L', 39000.0, 1),
(17, 2, N'Size M', 29000.0, 1),
(18, 2, N'Size L', 35000.0, 1),
(19, 3, N'Size M', 42000.0, 1),
(20, 3, N'Size L', 49000.0, 1),
(21, 31, N'Size M', 25000.0, 1),
(22, 31, N'Size L', 32000.0, 1),
(23, 32, N'Size M', 35000.0, 1),
(24, 32, N'Size L', 42000.0, 1),
(25, 33, N'Size M', 40000.0, 1),
(26, 33, N'Size L', 48000.0, 1),
(27, 34, N'Size M', 45000.0, 1),
(28, 34, N'Size L', 52000.0, 1),
(29, 35, N'Size M', 45000.0, 1),
(30, 35, N'Size L', 52000.0, 1),
(31, 36, N'Size M', 45000.0, 1),
(32, 36, N'Size L', 52000.0, 1),
(33, 37, N'Size M', 42000.0, 1),
(34, 37, N'Size L', 49000.0, 1),
(35, 38, N'Size M', 49000.0, 1),
(36, 38, N'Size L', 59000.0, 1),
(37, 39, N'Size M', 52000.0, 1),
(38, 39, N'Size L', 62000.0, 1),
(39, 40, N'Size M', 45000.0, 1),
(40, 40, N'Size L', 52000.0, 1);
SET IDENTITY_INSERT dbo.[ProductVariants] OFF;
GO

SET IDENTITY_INSERT dbo.[Ingredients] ON;
INSERT INTO dbo.[Ingredients] ([IngredientId], [IngredientName], [Unit], [StockQuantity], [MinStockLevel], [LastUpdated]) VALUES
(1, N'Hạt Cà phê Espresso', N'gram', 5000.0, 500.0, N'2026-10-22T11:52:28.9439619'),
(2, N'Sữa đặc Ngôi Sao', N'ml', 10000.0, 1000.0, N'2026-10-22T11:52:28.9439619'),
(3, N'Sữa tươi thanh trùng', N'ml', 15000.0, 2000.0, N'2026-10-22T11:52:28.9439619'),
(4, N'Cốt Trà Đen', N'ml', 9000.0, 1000.0, N'2026-11-09T23:33:35.4646739'),
(5, N'Syrup Đào', N'ml', 3000.0, 500.0, N'2026-10-22T11:52:28.9439619'),
(6, N'Đào miếng ngâm', N'miếng', 120.0, 20.0, N'2026-10-22T11:52:28.9439619');
SET IDENTITY_INSERT dbo.[Ingredients] OFF;
GO

SET IDENTITY_INSERT dbo.[ProductRecipes] ON;
INSERT INTO dbo.[ProductRecipes] ([RecipeId], [VariantId], [IngredientId], [QuantityRequired], [PreparationGuide]) VALUES
(1, 2, 1, 18.0, N'Chiết xuất 18g cà phê lấy 30ml cốt Espresso'),
(2, 2, 2, 40.0, N'Đong 40ml sữa đặc vào đáy ly'),
(3, 2, 3, 100.0, N'Đánh bọt 100ml sữa tươi đổ lên trên đá, sau đó rót Espresso lên mặt'),
(4, 6, 4, 150.0, N'Lắc đều 150ml cốt trà đen với đá'),
(5, 6, 5, 30.0, N'Pha 30ml Syrup Đào cùng 10ml nước đường'),
(6, 6, 6, 3.0, N'Trang trí 3 miếng đào ngâm và 1 lát cam sả lên mặt ly');
SET IDENTITY_INSERT dbo.[ProductRecipes] OFF;
GO

-- Ca demo phục vụ chạy thử; không sao chép két tiền, lịch sử bán hàng hoặc giao dịch thực.
INSERT INTO dbo.CashierShifts (CashierId, OpeningCash, IsOpeningCashConfirmed, SystemCashSales, ShiftStatus, HandoverNotes)
VALUES (2, 1000000, 1, 0, N'Open', N'Ca demo sau khi khởi tạo database');
GO
