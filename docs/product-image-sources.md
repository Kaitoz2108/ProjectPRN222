# Ảnh sản phẩm đã đối chiếu

Ngày: 09/11/2026. Đối chiếu trực tiếp 13 bản ghi Products trong CafeManagementDB.
Giữ nguyên ba ảnh đúng của Bạc Xỉu Đá, Cà Phê Sữa Đá và Trà Đào Cam Sả; thay 10 đường dẫn sai.
Ảnh tải về được giữ nguyên nội dung; phần mở rộng theo đúng định dạng JPEG/PNG/WebP thực tế.
Các ảnh là ảnh minh họa từ những nguồn dưới đây, không phải ảnh chụp xác nhận sản phẩm thực tế của quán.

| Sản phẩm | ID tại database đã kiểm tra | File trong wwwroot/images/products | Nguồn |
|---|---:|---|---|
| Cà Phê Đen Đá | 31 | cf-den-da.jpg | [productshop.myharavan.com](https://productshop.myharavan.com/products/ca-phe-den) |
| Cà Phê Muối | 32 | cf-muoi.png | [maisoncaphe.com](https://maisoncaphe.com/vietnamese-coffee) |
| Cà Phê Trứng | 33 | cf-trung.webp | [www.legalnomads.com](https://www.legalnomads.com/vietnamese-egg-coffee-recipe/) |
| Latte Hạnh Nhân | 34 | latte-hanh-nhan.jpg | [www.keepingthepeas.com](https://www.keepingthepeas.com/almond-milk-latte/) |
| Trà Vải Hạt Sen | 35 | tra-vai-hat-sen.jpg | [lotusdelivery.vn](https://lotusdelivery.vn/products/lychee-lotus-seeds-tea-large) |
| Trà Ô Long Tuyết Lê | 36 | tra-o-long-tuyet-le.jpg | [teaphor.com](https://teaphor.com/en/pear-oolong-tea/) |
| Trà Dưa Lưới Nha Đam | 37 | tra-dua-luoi-nha-dam.png | Ảnh minh họa tạo bằng imagegen: trà dưa lưới, đá và nha đam; không dùng ảnh salad/poster công thức. |
| Matcha Đá Xay | 38 | matcha-da-xay.png | [monin.us](https://monin.us/products/matcha-frappe) |
| Caramel Espresso Đá Xay | 39 | caramel-espresso-da-xay.jpg | [www.tiger-corporation.com](https://www.tiger-corporation.com/en/usa/feature/recipe/coffee-machine/caramel-frappuccino/) |
| Matcha Latte Nóng | 40 | matcha-latte-nong.jpg | [images.unsplash.com](https://images.unsplash.com/photo-1536256263959-770b48d82b0a) — ảnh matcha trước đây bị gán nhầm cho Latte Hạnh Nhân. |

## Áp dụng cho database khác

Triển khai các file ảnh trước, sau đó chạy `Database/UpdateProductImages.sql`.
Script khớp theo tên món và URL sai cũ, không phụ thuộc ProductId vì ID thực tế (31–40) khác ID seed (4–13).
Script chỉ cập nhật Products.ImageUrl, chạy trong transaction và có thể chạy lại; ảnh tùy chỉnh khác được giữ nguyên.
DatabaseProject.sql đã dùng đường dẫn nội bộ đúng cho đủ 13 sản phẩm khi khởi tạo mới.
Không chạy lại DatabaseProject.sql trên database đang dùng chỉ để sửa ảnh vì script đó tạo lại database.

## Ảnh minh họa được tạo

Công cụ: built-in imagegen. File: `tra-dua-luoi-nha-dam.png`.
Prompt: ảnh sản phẩm vuông, một ly trà xanh dưa lưới có đá và các khối nha đam trong suốt ở đáy; trang trí lát dưa lưới, cạnh ly có dưa lưới và nha đam; nền kem, ánh sáng tự nhiên; không sữa, kem, hạt chia, trân châu, nhãn hiệu hoặc chữ.

Prompt gốc đã sử dụng:

```text
Use case: product-mockup. Create one square photorealistic illustrative menu image of the Vietnamese café beverage "Trà Dưa Lưới Nha Đam" (iced cantaloupe / honeydew melon green tea with aloe vera cubes). Intended use: replace an incorrect salad photograph in a café product catalog. Show exactly this beverage, NOT a salad, NOT a milkshake, NOT bubble tea, NOT winter melon tea.
One clear tall elegant unbranded glass, full glass visible and centered with breathing space for responsive product-card cropping. Light translucent pale melon-green jasmine green tea over clearly visible ice cubes. A generous small layer of translucent colorless aloe vera gel cubes visible at the bottom, no seeds, no tapioca pearls, no coconut jelly strips. Garnish with one modest fresh cantaloupe wedge on the rim; show a few small orange cantaloupe cubes near the glass to identify the fruit, and one small cut aloe vera leaf segment beside it to identify the aloe topping. No other fruit, no apple, no lime, no whipped cream, no milk foam.
Background: warm neutral cream seamless café tabletop and softly blurred beige background, natural diffused daylight, refined product photography, realistic refreshing condensation and natural glass reflections, minimal composition harmonious with a cream and forest green Vietnamese café website. Drink must look real and appetizing, fresh and translucent rather than opaque or artificially neon green.
No branding, no text, no captions, no recipe card, no labels, no logos, no watermark. Square 1024 by 1024 composition, high detail. This image is an illustrative product photograph, not evidence of a real café serving.
```
