-- Correct only the known incorrect product image URLs. No schema or sales data changes.
-- Run AFTER deploying the image files under CafeManagementSystem/wwwroot/images/products.
-- Safe to rerun: existing corrected URLs and custom images are preserved.
USE CafeManagementDB;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Images TABLE (
        ProductName nvarchar(150) NOT NULL PRIMARY KEY,
        PreviousImageUrl nvarchar(500) NOT NULL,
        CorrectImageUrl nvarchar(500) NOT NULL
    );

    INSERT INTO @Images (ProductName, PreviousImageUrl, CorrectImageUrl) VALUES
    (N'Cà Phê Đen Đá', N'https://images.unsplash.com/photo-1517256064527-09c73fc73e38?w=800&auto=format&fit=crop&q=80', N'/images/products/cf-den-da.jpg'),
    (N'Cà Phê Muối', N'https://images.unsplash.com/photo-1461023058943-07fcbe16d735?w=800&auto=format&fit=crop&q=80', N'/images/products/cf-muoi.png'),
    (N'Cà Phê Trứng', N'https://images.unsplash.com/photo-1534778101976-62847782c213?w=800&auto=format&fit=crop&q=80', N'/images/products/cf-trung.webp'),
    (N'Latte Hạnh Nhân', N'https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80', N'/images/products/latte-hanh-nhan.jpg'),
    (N'Trà Vải Hạt Sen', N'https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80', N'/images/products/tra-vai-hat-sen.jpg'),
    (N'Trà Ô Long Tuyết Lê', N'https://images.unsplash.com/photo-1499638673689-79a0b5115d87?w=800&auto=format&fit=crop&q=80', N'/images/products/tra-o-long-tuyet-le.jpg'),
    (N'Trà Dưa Lưới Nha Đam', N'https://images.unsplash.com/photo-1540420773420-3366772f4999?w=800&auto=format&fit=crop&q=80', N'/images/products/tra-dua-luoi-nha-dam.png'),
    (N'Matcha Đá Xay', N'https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop&q=80', N'/images/products/matcha-da-xay.png'),
    (N'Caramel Espresso Đá Xay', N'https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80', N'/images/products/caramel-espresso-da-xay.jpg'),
    (N'Matcha Latte Nóng', N'https://images.unsplash.com/photo-1515823662972-da6a2e4d3002?w=800&auto=format&fit=crop&q=80', N'/images/products/matcha-latte-nong.jpg');

    IF EXISTS (
        SELECT p.ProductName
        FROM dbo.Products AS p
        INNER JOIN @Images AS i ON i.ProductName = p.ProductName
        GROUP BY p.ProductName
        HAVING COUNT(*) > 1
    )
        THROW 51000, 'Product names are ambiguous. No image URLs were changed.', 1;

    UPDATE p
    SET p.ImageUrl = i.CorrectImageUrl
    OUTPUT inserted.ProductId, inserted.ProductName,
           deleted.ImageUrl AS PreviousImageUrl, inserted.ImageUrl AS CorrectImageUrl
    FROM dbo.Products AS p WITH (UPDLOCK)
    INNER JOIN @Images AS i ON i.ProductName = p.ProductName
    WHERE p.ImageUrl = i.PreviousImageUrl;

    DECLARE @UpdatedProducts int = @@ROWCOUNT;
    COMMIT TRANSACTION;
    SELECT @UpdatedProducts AS UpdatedProducts;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
